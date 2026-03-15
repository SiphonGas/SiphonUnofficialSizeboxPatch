local Enabled = false
local TemplateHeliModels = {
	{name="Attack Heli",scale=0.1}
} --This is a list of lists that dictate what the model name is (not case-sensitive) and its spawn size

local NoisePromptOffDiff = 2 --At what difficulty will the noise level become hidden?
local TargettedPromptOffDiff = 1 --At what difficulty do you become unable to see what hostile is targetting you.

--This script includes a "Noise" system. The more you move while playing as a player the higher your noise is. The higher the noise the more likely you are to be chosen over other targets (Same for AI micros)

--==DO NOT EDIT BELOW THIS LINE!==--

stompAnimModule = require "stomp_anim"

local Settings = {
	CannotBeEdited = {
		GTSWalkAnimation = "Walk", --Cannot be configured!
		GTSRunAnimation = {"Run","Running","Fast Run"}, --Cannot be configured!
		MicroWalkAnimation = "Walk", --Cannot be configured!
		MicroRunAnimation = {"Run","Running","Fast Run"}, --Cannot be configured!
		IsConfigured = false,
		ConfigureIndex = 0
	},
	Limits={
		FriendlyChance = {min=0,max=100},
		FriendlyFollowMulti = {min=1,max=10},
		Difficulty = {min=0,max=100},
		HungryChance = {min=0,max=100},
		SpawnRadius = {min=0,max=5000},
		SpawnAlt = {min=0,max=5000},
		MaxGTS = {min=1,max=5},
		MaxMicro = {min=0,max=50},
		MaxScout = {min=0,max=75},
		ScoutSpeed = {min=0,max=6},
		ScoutCruiseAlt = {min=0,max=5000},
		ScoutFOV = {min=1,max=180}
	},
	SettingDescriptions={
		CanBeHungry = "Can the GTS eat the target!",
		CanBeFriendly = "Can the GTS be friendly? (Allows revival of dead players & NPC)",
		GTSInvestigate = "Can GTS enter the investigation state?",
		FriendlyChance = "Chance for the GTS to be friendly. (There will ALWAYS be a hostile GTS)",
		FriendlyFollowMulti = "How many GTS should be able to fit in between you and the GTS (Not ontop)",
		Difficulty = "Difficulty the script is on. Higher Values = More danger",
		HungryChance = "Chance for a GTS to be \"Hungry\". Hungry GTS will eat what they stomp preventing revive",
		SpawnRadius = "Radius around camera looking position to spawn entities",
		SpawnAlt = "What altitude to spawn entities at?",
		MaxGTS = "How many GTS to spawn",
		MaxMicro = "How many Micros to spawn",
		MaxScout = "How many scouts to spawn",
		ScoutSpeed = "How fast scouts are. (Default: 1)",
		ScoutCruiseAlt = "Scout cruise altitude during Wandering behavior",
		ScoutFOV = "How wide the scouts FOV is. Increase if scouts are too blind.",
		DescriptionPrompt={}
	},
	CanBeHungry = true,
	CanBeFriendly = true,
	GTSInvestigate = true,
	FriendlyChance = 10,
	FriendlyFollowMulti = 1,
	Difficulty = 0,
	HungryChance = 100,
	SpawnRadius = 100,
	SpawnAlt = 100,
	MaxGTS = 3,
	MaxMicro = 4,
	MaxScout = 1,
	ScoutSpeed = 1,
	ScoutCruiseAlt = 400,
	ScoutFOV = 45
}

local Data = {
	GTS = {},
	SCOUT = {},
	MICRO = {},
	HeliModels = {},
	SpawnPosition = Vector3.New(0,0,0)
}
function math.clamp(n,x,m)
	if n < x then return x elseif n > m then return m else return n end
end
function GetStartAlt()
	local model = Entity.GetGtsModelList()[math.random(#Entity.GetGtsModelList())]
	local gts = Entity.SpawnGiantess(model,math.random(45,50))
	Settings.SpawnAlt = math.floor(gts.transform.position.y)
	Settings.ScoutCruiseAlt = math.floor(gts.transform.position.y)*2
	Data.SpawnPosition = gts.transform.position
	gts.Delete()
end
function GetCameraLookAt()
	local model = Entity.GetGtsModelList()[math.random(#Entity.GetGtsModelList())]
	local gts = Entity.SpawnGiantess(model,math.random(45,50))
	local pos = nil
	local rot = nil
	if gts ~= nil and gts.position ~= nil then pos=gts.position end
	if gts ~= nil and gts.transform ~= nil then rot=gts.transform.rotation end
	gts.Delete()
	return pos,rot
end
function GetWalkAnimation()
	local model = Entity.GetGtsModelList()[math.random(#Entity.GetGtsModelList())]
	local gts = Entity.SpawnGiantess(model,math.random(45,50))
	for _,v in pairs(gts.animation.GetAnimationList()) do
		if string.find(v,"Walk Feminine 70AP") then Settings.CannotBeEdited.GTSWalkAnimation="Walk Feminine 70AP"
		elseif string.find(v,"Female Walk") then Settings.CannotBeEdited.GTSWalkAnimation="Female Walk" end
	end
	gts.Delete()
end
function GetHeliModel()
	for _,v in pairs(Entity.GetObjectList()) do
		for _,v2 in pairs(TemplateHeliModels) do
			if string.find(v,v2.name) then
				Data.HeliModels[#Data.HeliModels+1]={name=v,scale=v2.scale}
			end
		end
	end
end
function Start()
	if not Enabled then return end
	GetWalkAnimation()
	GetStartAlt()
	GetHeliModel()
	local Event1 = Event.Register(self, EventCode.OnLocalPlayerChanged, PlayerChanged)
	local Event1 = Event.Register(self, EventCode.KeyDown, KeyPressedDown)
	local Event1 = Event.Register(self, EventCode.KeyUp, KeyPressedUp)
end
function PlaceRandomRadius(entity,radius,alt)
	local x = math.random(0,radius)
	local z = math.random(0,radius)
	local s,r = pcall(function() return math.random(entity.scale,radius) end)
	local s2,r2 = pcall(function() return math.random(entity.scale,radius) end)
	if s then x=r end
	if s2 then z=r2 end
	if math.random(0,2) <= 1 then x=x*-1 end
	if math.random(0,2) <= 1 then z=z*-1 end
	entity.transform.position = Vector3.New(Data.SpawnPosition.x+x,alt,Data.SpawnPosition.z+z)
	entity.transform.rotation = Quaternion.Euler(0,math.random(-180,180),0)
end
function table.firstElement(tablee)
	for _,v in pairs(tablee) do
		if v~=nil then return v end
	end
	return nil
end
function table.lastElement(tablee)
	return tablee[#tablee]
end
function table.combine( ... )
	local mt = {}
	for _,v in pairs(...) do
		mt[#mt+1]=v
	end
	return mt
end
function MoveAway(entity,pos,distance,turnOnly)
	local ky = ternary(turnOnly~=nil,turnOnly,true)
	local rot = entity.transform.rotation
	entity.transform.LookAt(pos)
	entity.transform.rotation = Quaternion.Euler(rot.eulerAngles.x,entity.transform.rotation.eulerAngles.y,rot.eulerAngles.z)
	entity.transform.Translate(-Vector3.forward * distance)
	entity.transform.rotation=rot
end
function SafeLocation(entity)
	for i=1,50 do
		local pos = entity.position + Vector3.New(math.random(-100,100),0,math.random(-100,100))
		local d = math.huge
		local ct = table.combine(Data.GTS,Data.MICRO,Data.SCOUT)
		for _,v in pairs(ct) do
			if v~=nil and v.entity~=nil and v.entity.id~=entity.id then
				if Vector3.Distance(Vector3.New(pos.x,v.entity.position.y,pos.z),v.entity.position) < d then
					d = Vector3.Distance(Vector3.New(pos.x,v.entity.position.y,pos.z),v.entity.position)
				end
			end
		end
		if m~=nil and d > (m.entity.scale+entity.scale)*ternary(entity.isGiantess(),4,2) then
			entity.position = pos
			break;
		end
	end
	return entity
end
function SpawnEntities()
	if #Data.GTS < Settings.MaxGTS then
		local model = Entity.GetGtsModelList()[math.random(#Entity.GetGtsModelList())]
		local gts = Entity.SpawnGiantess(model,math.random(45,50))
		local data = {
			entity = gts,
			model = model,
			state = "SpawnCooldown",
			lastState = "None",
			stateCooldown = (5*60),
			target = nil,
			isAIEnabled = false,
			lostPosition = nil,
			canBeKnockedOut = true,
			canKnockOut = true,
			ourpos = gts.position,
			stucktimer = -1,
			blacklistedStates={},
			friendly = #Data.GTS > 0 and math.random(0,100) <= Settings.FriendlyChance and Settings.CanBeFriendly,
			hungry = math.random(0,100) <= Settings.HungryChance and Settings.CanBeHungry
		}
		if data.friendly then print(gts.name.." is friendly!"); data.hungry=false else print(gts.name.." is hostile!") end
		PlaceRandomRadius(gts,Settings.SpawnRadius,Settings.SpawnAlt)
		SafeLocation(gts)
		Data.GTS[#Data.GTS+1]=data
	end
	if GetAIMicroCount() < Settings.MaxMicro then
		local data = {
			entity = nil,
			model = nil,
			cannotBeTargetted = false,
			hidden = false,
			isAI = true,
			isAIEnabled = false,
			state="SpawnCooldown",
			lastState = "None",
			ourpos = nil,
			stucktimer = -1,
			blacklistedStates={},
			stateCooldown = (2*60),
			fleeTimer = -1,
			noise = 0
		}
		if math.random(0,1) == 0 then
			local model = Entity.GetMaleMicroList()[math.random(#Entity.GetMaleMicroList())]
			local micro = Entity.SpawnMaleMicro(model,1)
			data.entity = micro
			data.model = model
			data.ourpos = micro.position
		else
			local model = Entity.GetFemaleMicroList()[math.random(#Entity.GetFemaleMicroList())]
			local micro = Entity.SpawnFemaleMicro(model,1)
			data.entity = micro
			data.model = model
			data.ourpos = micro.position
		end
		PlaceRandomRadius(data.entity,Settings.SpawnRadius,Settings.SpawnAlt)
		SafeLocation(data.entity)
		Data.MICRO[#Data.MICRO+1]=data
	end
	if #Data.HeliModels > 0 and #Data.SCOUT < Settings.MaxScout then
		local model = Data.HeliModels[math.random(#Data.HeliModels)]
		local pos,rot = GetCameraLookAt()
		local obj = Entity.SpawnObject(model.name,pos,rot,model.scale)
		if #Data.GTS > 0 then
			local gts = table.firstElement(Data.GTS)
			if gts~=nil then
				local pos = gts.entity.transform.position
				local rx = math.random(50,100)
				local rz = math.random(50,100)
				if math.random(0,1) == 0 then rx=rx*-1 end
				if math.random(0,1) == 0 then rz=rz*-1 end
				obj.transform.position = Vector3.New(pos.x+rx,Settings.SpawnAlt,pos.z+rz)
			end
		else
			PlaceRandomRadius(obj,Settings.SpawnRadius,Settings.SpawnAlt)
		end
		SafeLocation(obj)
		local data = {
			entity = obj,
			model = model,
			state="SpawnCooldown",
			lastState = "None",
			target=nil,
			stateCooldown = (60),
			fleeTimer = -1,
			lostPosition = nil,
			wanderPosition = nil,
			blacklistedStates={},
			forwardMotion = 0,
			tiltMotion = 0,
			positionCheckTimer = -1,
			hasArrived = false,
			canChangeTargets = true,
		}
		Data.SCOUT[#Data.SCOUT+1] = data
	end
end
function GetLimit(index)
	return Settings.Limits[index]
end
function VariableToString(variable)
	if type(variable) == "nil" then return "nil" end
	if type(variable) == "boolean" then if variable then return "true" else return "false" end end
	if type(variable) == "number" then return tostring(variable) end
	if type(variable) == "function" then return "function" end
	if type(variable) == "table" then return "table" end
	return "Unknown"
end
function DecreaseConfig(index)
	local ind = ConvertToCFGIndex(index)
	local var = ElementAtNumIndex(index)
	if ind ~= nil and var ~= nil then
		if type(var) == "boolean" then if var then var=false else var=true end end
		if type(var) == "number" then 
			local restrict = GetLimit(ind)
			local min = ternary(restrict~=nil,restrict.min,0)
			local max = ternary(restrict~=nil,restrict.max,math.huge)
			var=math.clamp(var-1,min,max)
		end
		Settings[ind]=var
	end
end
function IncreaseConfig(index)
	local ind = ConvertToCFGIndex(index)
	local var = ElementAtNumIndex(index)
	if ind ~= nil and var ~= nil then
		if type(var) == "boolean" then if var then var=false else var=true end end
		if type(var) == "number" then
			local restrict = GetLimit(ind)
			if restrict~=nil then
				local min = ternary(restrict~=nil,restrict.min,0)
				local max = ternary(restrict~=nil,restrict.max,math.huge)
				var=math.clamp(var+1,min,max)
			else var=var+1 end
		end
		Settings[ind]=var
	end
end
local skipConfigs = 3
function GetConfigLength()
	local s1 = 0
	local c = 0
	for _,_ in pairs(Settings) do
		if s1 < skipConfigs then s1=s1+1 else
			c=c+1
		end
	end
	return c
end
function ConvertToCFGIndex(num_index)
	local s1 = 0
	local c = 0
	for index,_ in pairs(Settings) do
		if s1<skipConfigs then s1=s1+1 else
			if c==num_index then return index end
			c=c+1
		end
	end
	return nil
end
function ElementAtNumIndex(num_index)
	local s1 = 0
	local c = 0
	for index,v in pairs(Settings) do
		if s1<skipConfigs then s1=s1+1 else
			if c==num_index then return v end
			c=c+1
		end
	end
	return nil
end
function DisplayDesciption(pure_index)
	local desc = Settings.SettingDescriptions[pure_index]
	if #Settings.SettingDescriptions.DescriptionPrompt >= 3 then
		local toast = table.lastElement(Settings.SettingDescriptions.DescriptionPrompt)
		if toast~=nil and desc~=nil then
			toast.toast.print(desc)
			toast.text=desc
			toast.timer=(5.5*60)
			Settings.SettingDescriptions.DescriptionPrompt[#Settings.SettingDescriptions.DescriptionPrompt]=toast
		elseif toast==nil and desc~=nil then
			Game.Toast.New().print(desc)
		end
	else
		local toast = Game.Toast.New()
		local toast2 = Game.Toast.New()
		local t1t = ""
		local t2t = ""
		local t2use = false
		if desc~=nil then
			toast.print(desc)
			t1t=desc
		else
			toast.print("No Description Assigned!")
			t1t="No Description Assigned!"
		end
		Settings.SettingDescriptions.DescriptionPrompt[#Settings.SettingDescriptions.DescriptionPrompt+1]={toast=toast,timer=(5.5*60),text=t1t}
	end
end
function DescriptionTimerOperator()
	for k,v in pairs(Settings.SettingDescriptions.DescriptionPrompt) do
		v.timer=math.clamp(v.timer-1,0,math.huge)
		if v.timer <= 0 then Settings.SettingDescriptions.DescriptionPrompt[k]=nil else Settings.SettingDescriptions.DescriptionPrompt[k]=v end
	end
end
function OperateConfigure()
	DescriptionTimerOperator()
	local indexfore = ConvertToCFGIndex(Settings.CannotBeEdited.ConfigureIndex-1)
	local index = ConvertToCFGIndex(Settings.CannotBeEdited.ConfigureIndex)
	local indexfuture = ConvertToCFGIndex(Settings.CannotBeEdited.ConfigureIndex+1)
	if indexfore == nil and index ~= nil and indexfuture ~= nil then
		print(string.format("%s\n\n\n\n%s: %s (Editing)\n%s: %s\n================\nKey (-): Decrease Variable\nKey (=): Increase Variable\nKey ([): Change Index Back\nKey (]): Change Index Forward\nKey(;) Display Description 	Key ('): Finish Editing",math.random(0,math.huge),index,VariableToString(ElementAtNumIndex(Settings.CannotBeEdited.ConfigureIndex)),indexfuture,VariableToString(ElementAtNumIndex(Settings.CannotBeEdited.ConfigureIndex+1))))
	elseif indexfore ~= nil and index ~= nil and indexfuture == nil then
		print(string.format("%s\n\n\n\n%s: %s\n%s: %s (Editing)\n================\nKey (-): Decrease Variable\nKey (=): Increase Variable\nKey ([): Change Index Back\nKey (]): Change Index Forward\nKey(;) Display Description 	Key ('): Finish Editing",math.random(0,math.huge),indexfore,VariableToString(ElementAtNumIndex(Settings.CannotBeEdited.ConfigureIndex-1)),index,VariableToString(ElementAtNumIndex(Settings.CannotBeEdited.ConfigureIndex))))
	else
		print(string.format("%s\n\n\n\n%s: %s\n%s: %s (Editing)\n%s: %s\n================\nKey (-): Decrease Variable\nKey (=): Increase Variable\nKey ([): Change Index Back\nKey (]): Change Index Forward\nKey(;) Display Description 	Key ('): Finish Editing",math.random(0,math.huge),indexfore,VariableToString(ElementAtNumIndex(Settings.CannotBeEdited.ConfigureIndex-1)),index,VariableToString(ElementAtNumIndex(Settings.CannotBeEdited.ConfigureIndex)),indexfuture,VariableToString(ElementAtNumIndex(Settings.CannotBeEdited.ConfigureIndex+1))))
	end
	if Input.GetKeyDown("[") then
		Settings.CannotBeEdited.ConfigureIndex=math.clamp(Settings.CannotBeEdited.ConfigureIndex-1,0,GetConfigLength()-1)
	elseif Input.GetKeyDown("]") then
		Settings.CannotBeEdited.ConfigureIndex=math.clamp(Settings.CannotBeEdited.ConfigureIndex+1,0,GetConfigLength()-1)
	elseif Input.GetKeyDown("-") then
		DecreaseConfig(Settings.CannotBeEdited.ConfigureIndex)
	elseif Input.GetKeyDown("=") then
		IncreaseConfig(Settings.CannotBeEdited.ConfigureIndex)
	elseif Input.GetKey("left shift") and Input.GetKey("-") then
		DecreaseConfig(Settings.CannotBeEdited.ConfigureIndex)
	elseif Input.GetKey("left shift") and Input.GetKey("=") then
		IncreaseConfig(Settings.CannotBeEdited.ConfigureIndex)
	elseif Input.GetKeyDown(";") then
		DisplayDesciption(index)
	elseif Input.GetKeyDown("'") then
		Settings.CannotBeEdited.ConfigureIndex=0
		Settings.CannotBeEdited.IsConfigured=true
	end
end
local pff = 0
local dtw = 0
function Update()
	if not Enabled then return end
	if Settings.CannotBeEdited.IsConfigured then
		pff=math.clamp(pff-1,0,math.huge)
		SpawnEntities()
		WaitForSpawn()
		PlayerController()
		OperateGTSCooldowns()
		OperateScout()
		if pff <= 0 then Per5Frames(); pff=5 end
	else
		OperateConfigure()
	end
end
function OperateGTSCooldowns()
	for i,v in pairs(Data.GTS) do
		if v~=nil and v.isAIEnabled then
			if v.stateCooldown >= -1 then v.stateCooldown = math.clamp(v.stateCooldown-1,-1,math.huge) else v.stateCooldown = math.clamp(v.stateCooldown+1,-math.huge,-2) end
			Data.GTS[i]=v
		end
	end
end
function Per5Frames()
	OperateMicro()
	OperateGTS()
end
local overmasterTimer = -1
function OperateOvermaster()
	if Settings.Difficulty == 0 then return end
	overmasterTimer=clamp(overmasterTimer-1,-1,math.huge)
	if overmasterTimer <= -1 then
		local spawnmin = clamp(math.floor(100/Settings.Difficulty))
		local spawnmax = clamp(math.floor(1000/Settings.Difficulty))
		for gts_i,gts in pairs(Data.GTS) do
			if gts~=nil and gts.isAIEnabled and gts.state=="Wandering" and gts.entity~=nil and math.random(0,100) <= 50 then
				local targets = {}
				for _,v in pairs(Data.MICRO) do
					if v~=nil and v.entity~=nil and not IsTargetted(v.entity,false,gts.friendly) and not (v.entity.IsDead() or v.entity.IsCrushed()) and not v.hidden and not v.cannotBeTargetted then
						targets[#targets+1]=v
					end
				end
				local tar = targets[math.random(#targets)]
				if tar~=nil then
					local targetpos = tar.entity.position+Vector3.New(math.random(spawnmin,spawnmax),0,math.random(spawnmin,spawnmax))
					if math.random(0,1) == 0 then targetpos = tar.entity.position+Vector3.New(math.random(-spawnmin,-spawnmax),0,math.random(-spawnmin,-spawnmax)) end
					gts.state="Investigating"
					gts.lastState="None"
					gts.lostPosition = targetpos
					Data.GTS[gts_i]=gts
				end
			end
		end
		for gts_i,gts in pairs(Data.SCOUT) do
			if gts~=nil and gts.entity~=nil and gts.state=="Wandering" and math.random(0,100) <= 50 then
				local targets = {}
				for _,v in pairs(Data.MICRO) do
					if v~=nil and v.entity~=nil and not IsTargetted(v.entity,true) and not (v.entity.IsDead() or v.entity.IsCrushed()) and not v.hidden and not v.cannotBeTargetted then
						targets[#targets+1]=v
					end
				end
				local tar = targets[math.random(#targets)]
				if tar~=nil then
					local targetpos = tar.entity.position+Vector3.New(math.random(spawnmin,spawnmax),0,math.random(spawnmin,spawnmax))
					if math.random(0,1) == 0 then targetpos = tar.entity.position+Vector3.New(math.random(-spawnmin,-spawnmax),0,math.random(-spawnmin,-spawnmax)) end
					gts.state="Investigating"
					gts.lastState="None"
					gts.lostPosition = targetpos
					Data.SCOUT[gts_i]=gts
				end
			end
		end
		overmasterTimer = (math.random(10,20)*60)
	end
end
local completedSpawning = 0
function WaitForSpawn()
	for i,v in pairs(Data.GTS) do
		if v~= nil and not v.isAIEnabled then
			local s,d = pcall(function() return v.entity.senses~=nil end)
			if s and d then v.isAIEnabled =true; Data.GTS[i]=v; completedSpawning=completedSpawning+1; else completedSpawning=completedSpawning-1 end
		end
	end
	for i,v in pairs(Data.MICRO) do
		if v~= nil and v.entity~=nil and not v.isAIEnabled then
			local s,d = pcall(function() return v.entity.senses~=nil end)
			if s and d then v.isAIEnabled =true; Data.MICRO[i]=v; completedSpawning=completedSpawning+1; else completedSpawning=completedSpawning-1 end
		end
	end
end
function GetMicroIndex(micro)
	for i,v in pairs(Data.MICRO) do
		if v~=nil and v.entity ~= nil and v.entity.id == micro.id then return i end
	end
	return #Data.MICRO
end
function GetGTSIndex(gts)
	for i,v in pairs(Data.GTS) do
		if v~=nil and v.entity ~= nil and v.entity.id == gts.id then return i end
	end
	return #Data.GTS
end
local keybinds = {"m","w","a","s","d","left shift","right shift"}
local pressedKeys = {m=false,w=false,a=false,s=false,d=false,left_shift=false,right_shift=false}
function KeyPressedDown()
	for _,key in pairs(keybinds) do
		if Input.GetKey(key) then pressedKeys[key]=true; return end
	end
end
function KeyPressedUp()
	for _,key in pairs(keybinds) do
		if Input.GetKey(key) then pressedKeys[key]=false; return end
	end
end
function TriDotStringToTable(...)
	local a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z=...
	local tb = {}
	if a~=nil then table.insert(tb,a) end
	if b~=nil then table.insert(tb,b) end
	if c~=nil then table.insert(tb,c) end
	if d~=nil then table.insert(tb,d) end
	if e~=nil then table.insert(tb,e) end
	if f~=nil then table.insert(tb,f) end
	if g~=nil then table.insert(tb,g) end
	if h~=nil then table.insert(tb,h) end
	if i~=nil then table.insert(tb,i) end
	if j~=nil then table.insert(tb,j) end
	if k~=nil then table.insert(tb,k) end
	if l~=nil then table.insert(tb,l) end
	if m~=nil then table.insert(tb,m) end
	if n~=nil then table.insert(tb,n) end
	if o~=nil then table.insert(tb,o) end
	if p~=nil then table.insert(tb,p) end
	if q~=nil then table.insert(tb,q) end
	if r~=nil then table.insert(tb,r) end
	if s~=nil then table.insert(tb,s) end
	if t~=nil then table.insert(tb,t) end
	if u~=nil then table.insert(tb,u) end
	if v~=nil then table.insert(tb,v) end
	if w~=nil then table.insert(tb,w) end
	if x~=nil then table.insert(tb,x) end
	if y~=nil then table.insert(tb,y) end
	if z~=nil then table.insert(tb,z) end
	return tb
end
function IsKeysDown( ... )
	local c = 0
	local tb = TriDotStringToTable(...)
	local operator = tb[#tb]
	table.remove(tb,#tb)
	for _,v in pairs(tb) do
		local s,state = pcall(function() return Input.GetKey(v) end)
		if s and state then c=c+1 end
	end
	if operator == "or" then
		return c>0
	else return c==#tb end
end
function IsOnlyKeyDown(test_key)
	for key,state in pairs(pressedKeys) do
		if key~=test_key and state then return false end
	end
	return Input.GetKey(test_key)
end
local plrindex = -1
local plrToast = Game.Toast.New()
local plrToast2 = Game.Toast.New()
function PlayerChanged()
	local plr = Game.GetLocalPlayer()
	if plr ~= nil and plr.isMicro() then
		local data = { --most of these functions wont be used but it is required as alot of scripts call these values
			entity = plr,
			model = plr.name,
			cannotBeTargetted = false,
			hidden = false,
			isAI = false,
			isAIEnabled = false,
			state="None",
			lastState = "None",
			ourpos = nil,
			stucktimer = -1,
			blacklistedStates={},
			stateCooldown = -1,
			fleeTimer = -1,
			noise = 0
		}
		plrindex=GetMicroIndex(plr)
		if Data.MICRO[plrindex]~= nil and Data.MICRO[plrindex].entity ~= nil then data = Data.MICRO[plrindex]
		elseif Data.MICRO[plrindex] ~= nil and Data.MICRO[plrindex].entity == nil then Data.MICRO[plrindex]=nil;plrToast = Game.Toast.New() end --this is for kinda performance sake. Prevents users from overfilling the MICRO table by rapidly spawning and removing player micros.
		Data.MICRO[plrindex]=data
	end
end
function PlayerController()
	local s,plr = pcall(function() return Data.MICRO[plrindex] end)
	if s and plr~=nil then
		if IsOnlyKeyDown("m") then
			if plr.lastState ~= "PlayDead" then
				plr.entity.animation.SetAndWait("Falling Down")
				plr.entity.animation.SetAndWait("Down")
				plr.hidden=true
				plr.lastState = "PlayDead"
			end
		elseif not IsOnlyKeyDown("m") then plr.lastState="None"; plr.hidden = false
		elseif IsKeysDown("w","a","s","d","or") and Input.GetKey("left shift") then
			plr.noise = math.clamp(plr.noise+(2/(60/Settings.Difficulty)),0,100)
		elseif IsKeysDown("w","a","s","d","or") then
			plr.noise = math.clamp(plr.noise+(1/(60/Settings.Difficulty)),0,100)
		elseif IsKeysDown("w","a","s","d","or") and Game.GetLocalPlayerSettings().climbing then
			if Input.GetKey("left shift") then
				plr.noise = math.clamp(plr.noise+(0.75/(60/Settings.Difficulty)),0,100)
			else
				plr.noise = math.clamp(plr.noise+(0.5/(60/Settings.Difficulty)),0,100)
			end
		elseif not IsKeysDown("w","a","s","d","and") then
			plr.noise = math.clamp(plr.noise-(1/(60*Settings.Difficulty)),0,100)
		end
		if Settings.Difficulty >= NoisePromptOffDiff then
			plrToast2.Print(string.format("%s",math.floor(plr.noise)))
		end
		local m = GetTargetter(plr.entity,false,false)
		local mname = nil
		local s1,r1 = pcall(function() return m.entity.name end)
		if s1 then mname=string.sub(r1, 1, 14) end
		plrToast.Print(string.format("%s\n%s Is Targetting You",ternary(plr.hidden,"You are hidden","You are visible"),ternary(mname,mname,"Nothing")))
		Data.MICRO[plrindex]=plr
	end
end
function GetTargetter(entity,include_friendly,only_friendly)
	for _,v in pairs(Data.GTS) do
		if only_friendly~=nil and only_friendly then if v~=nil and v.friendly and include_friendly then
			if v.target~=nil and v.target.entity~=nil and v.target.entity.id == entity.id then return v end end
		elseif v~=nil and v.friendly and include_friendly then
			if v.target~=nil and v.target.entity~=nil and v.target.entity.id == entity.id then return v end
		elseif v~=nil and not include_friendly then
			if v.target~=nil and v.target.entity~=nil and v.target.entity.id == entity.id then return v end
		end
	end
	if not only_friendly then
		for i,v in pairs(Data.SCOUT) do
			if v~=nil and v.target~=nil and v.target.entity~=nil and v.target.entity.id == entity.id then return v end
		end
	end
	return nil
end
function OperateNoise(data)
	if data.entity==nil then return data end
	if data.entity.animation.Get() == "Low Crawl" then
		data.noise=math.clamp(data.noise+(0.5/(60/Settings.Difficulty)),0,100)
	elseif table.Contains(Settings.CannotBeEdited.MicroRunAnimation,data.entity.animation.Get()) then
		data.noise=math.clamp(data.noise+(2/(60/Settings.Difficulty)),0,100)
	elseif data.entity.animation.Get() == Settings.CannotBeEdited.MicroWalkAnimation then
		data.noise=math.clamp(data.noise+(1/(60/Settings.Difficulty)),0,100)
	else
		data.noise=math.clamp(data.noise-(1/(60*Settings.Difficulty)),0,100)
	end
	return data
end
function GetAIMicroCount()
	local c = 0
	for _,v in pairs(Data.MICRO) do
		if v~=nil and v.isAI then c=c+1 end
	end
	return c
end
function ResetState(gts,state)
	gts.state = state
	if pcall(function() gts.target.entity=gts.target.entity end) then
		gts.target = nil
	end
	if gts.hidden==true or gts.hidden==false then gts.hidden = false end
	gts.stateCooldown=-1
	--gts.entity.LookAt(nil)
	gts.lastState = "None"
	return gts
end
function ClearActions(data,resume_looking)
	local entity = data
	if not pcall(function() data.position=data.position end) then
		if data.entity~=nil then
			entity = data.entity
		end
		if resume_looking and data.target ~= nil and data.target.entity~=nil then entity.LookAt(data.target.entity) end
	end
	if entity.ai.IsActionActive() then entity.ai.StopAction() end
	if entity.ai.IsBehaviorActive() then entity.ai.StopBehavior() end
	if entity.ai.HasQueuedBehaviors() then entity.ai.CancelQueuedBehaviors() end
	if entity.ai.HasQueuedActions() then entity.ai.CancelQueuedActions() end
	if not pcall(function() data.position=data.position end) then
		if data.entity~=nil then
			entity = data.entity
		end
		if resume_looking and data.target ~= nil and data.target.entity~=nil then entity.LookAt(data.target.entity) end
	end
	return data
end
function AllowedState(state,lastState,allowed_states,allowed_lastState)
	local as = {"Wandering","Searching","Protect"}
	local las = {"ProtectFollowWait"}
	if allowed_states ~= nil then as = allowed_states end
	if allowed_lastState ~= nil then las = allowed_lastState end
	if lastState ~= nil then
		for _,v in pairs(as) do
			if state == v then return true end
		end
		for _,v in pairs(las) do
			if lastState == v then return true end
		end
		return false
	else
		for _,v in pairs(as) do
			if state == v then return true end
		end
		return false
	end
end
function ValidTarget(entity,target,allow_dead,require_see)
	if entity == nil or entity.entity == nil then return false end
	if allow_dead then
		if require_see then 
			return target~=nil and target.entity ~= nil and CanSeeTryCatch(entity.entity,target.entity)
		else 
			return target~=nil and target.entity ~= nil 
		end
	else
		if require_see then 
			return target~=nil and target.entity ~= nil and CanSeeTryCatch(entity.entity,target.entity) and not (target.entity.IsDead() or target.entity.IsCrushed())
		else 
			return target~=nil and target.entity ~= nil and not (target.entity.IsDead() or target.entity.IsCrushed()) 
		end
	end
	return false
end
function ReachedMoveTo(gts,target,divider) --At a set distance the GTS will autosprint to the target. Will stop sprinting at distance/1.5 (Half of half)
	local reached=false
	local pos = target
	local div = 2
	if divider ~= nil then div=divider end
	if pcall(function() target.position=target.position end) then pos = target.position end
	return Vector3.Distance(Vector3.New(gts.entity.position.x,pos.y,gts.entity.position.z),pos) <= gts.entity.scale/divider
end
function GetNearestGTS(pos,include_friendly,require_no_knockout)
	local rk = ternary(require_no_knockout ~= nil,require_no_knockout,false)
	local m = nil
	local d = math.huge
	for i,v in pairs(Data.GTS) do
		if v ~= nil and rk and v.canBeKnockedOut then
			if include_friendly then
				if v.entity ~= nil and Vector3.Distance(v.entity.position,pos) < d and Vector3.Distance(v.entity.position,pos) > 1 then
					d= Vector3.Distance(v.entity.position,pos)
					m=v
				end
			else
				if v.entity ~= nil and not v.friendly and Vector3.Distance(v.entity.position,pos) < d and Vector3.Distance(v.entity.position,pos) > 1 then
					d= Vector3.Distance(v.entity.position,pos)
					m=v
				end
			end
		elseif v~= nil and not rk then
			if include_friendly then
				if v.entity ~= nil and Vector3.Distance(v.entity.position,pos) < d and Vector3.Distance(v.entity.position,pos) > 1 then
					d= Vector3.Distance(v.entity.position,pos)
					m=v
				end
			else
				if v.entity ~= nil and not v.friendly and Vector3.Distance(v.entity.position,pos) < d and Vector3.Distance(v.entity.position,pos) > 1 then
					d= Vector3.Distance(v.entity.position,pos)
					m=v
				end
			end
		end
	end
	return m
end
function IsTargetted(entity,is_heli,is_friendly)
	if is_heli then
		for i,v in pairs(Data.SCOUT) do
			if v~=nil and v.target ~= nil and v.target.entity~= nil and v.target.entity.id == entity.id then return true end
		end
	else
		for i,v in pairs(Data.GTS) do
			if v~=nil and v.target ~= nil and v.target.entity~= nil and v.friendly == is_friendly and v.target.entity.id == entity.id then return true end
		end
	end
	return false
end
function table.Contains(t1,item)
	for _,v1 in pairs(t1) do
		if v1==item then return true end
	end
	return false
end
function ternary ( cond , T , F )
    if cond then return T else return F end
end
function ProbabilitySortedLTH(_table)
	local s = table.sort(_table, function(a,b) return a.probability > b.probability end)
	return s
end
function ProbabilitySortedHTL(_table)
	local s = table.sort(_table, function(a,b) return a.probability < b.probability end)
	return s
end
function GetTargetPriority(target,is_friendly,prioritise_dead,can_target_dead,is_heli)
	if IsTargetted(target.entity,ternary(is_heli==nil,false,is_heli),is_friendly) then return -1 end
	if target.hidden then return 0.25 end
	if can_target_dead then
		return ternary(prioritise_dead and target.entity.IsDead() or target.entity.IsCrushed(),2,1)
	else
		return ternary(target.entity.IsDead() or target.entity.IsCrushed(),-1,1)
	end
end
function GetNearestMicro(gts,pos,friendly,require_sight,prioritise_dead,can_target_dead,excludedEntities,is_heli)
	local m = nil
	local d = math.huge
	local exclude = {}
	if type(excludedEntities) == "table" then exclude=excludedEntities end
	local validTargets = {}
	for _,v in pairs(Data.MICRO) do
		if v~=nil and v.entity~= nil and not table.Contains(exclude,v.entity) then
			if not require_sight then
				local p = GetTargetPriority(v,friendly,prioritise_dead,can_target_dead,is_heli)
				validTargets[#validTargets+1]={data=v,probability=p}
			else
				if is_heli then
					if Vector3.Angle(v.entity.transform.forward, gts.entity.transform.position - Vector3.New(v.entity.transform.position.x,gts.entity.transform.position.y,v.entity.transform.position.z)) < Settings.ScoutFOV then
						local p = GetTargetPriority(v,false,prioritise_dead,can_target_dead,is_heli)
						validTargets[#validTargets+1]={data=v,probability=p}
					end
				elseif gts.entity.senses.CanSee(v.entity) or IsTargetted(v.entity,true,false) then
					local p = GetTargetPriority(v,friendly,prioritise_dead,can_target_dead,is_heli)
					validTargets[#validTargets+1]={data=v,probability=p}
				end
			end
		end
	end
	local sortedtable = ProbabilitySortedLTH(validTargets)
	for _,v in pairs(sortedtable) do
		if v.probability > 0 and Vector3.Distance(v.data.entity.position,gts.entity.position) < d then
			d = Vector3.Distance(v.data.entity.position,gts.entity.position)
			m = v
		end
	end
	if m~=nil then
		return m.data
	else return nil end
end
function DeadEntities()
	local c = 0
	for _,v in pairs(Data.MICRO) do
		if v~=nil and v.entity ~= nil and (v.entity.IsDead() or v.entity.IsCrushed()) then c=c+1 end
	end
	return c
end
function IsEntityIncapacitated(entity)
	return entity.IsDead() or entity.IsCrushed()
end
function StuckCheck(data)
	if Vector3.Distance(data.entity.position,data.ourpos) < 1 then --we havent moved!
		data.ourpos = data.entity.position
		data.stucktimer = data.stucktimer + 1
	else --we moved!
		data.ourpos = data.entity.position
		data.stucktimer = -1
	end
	if data.stucktimer > (3*60) then data.lastState = "None";data.stucktimer=-1; end
	return data
end
function CanSeeTryCatch(entity,target)
	if entity==nil or entity.senses==nil or target==nil then return false end
	local s,p = pcall(function() return entity.senses.CanSee(target) end)
	if s then return p else return false end
end
function IsInState(data,state,lastState)
	local sb = false
	for _,v in pairs(data.blacklistedStates) do
		if v~=nil and v.state~=nil and state==v.state then sb=true; break end
		if v~=nil and v.lastState~=nil and lastState==v.lastState then sb=true; break end
	end
	return data.state==state and not sb
end
function VerifyMicros()
	local s = false
	for i,v in pairs(Data.MICRO) do
		if v == nil or v.entity == nil then Data.MICRO[i] = nil; s = true end
	end
	return s
end
function VerifyGTS()
	local s = false
	for i,v in pairs(Data.GTS) do
		if v == nil or v.entity == nil then Data.GTS[i] = nil; s = true end
	end
	return s
end
function TargettingEntities(entity)
	local c = 0
	local ct = table.combine(Data.GTS,Data.SCOUT)
	for _,v in pairs(ct) do
		if ct ~= nil and ct.target~=nil and ct.target.entity ~=nil and ct.target.entity.id == entity.id then c=c+1 end
	end
	return c
end
function RemoveMicro(entity)
	for i,v in pairs(Data.MICRO) do
		if v~=nil and v.entity ~= nil and v.entity.id == entity.id then Data.MICRO[i] = nil; return true end
	end
	return false
end
function ReplaceTargetRider(data)
	local m = GetMicroRider(data.entity)
	if m~=nil and data.target~=nil and data.target.entity~=nil and data.target.entity.id ~= m.entity.id then
		ClearActions(data.entity)
		data.target=m
		if data.friendly then data.state="Protect" else data.state="Hunting" end
		data.lastState = "None"
	end
	return data
end
function GetMicroRider(entity)
	for _,v in pairs(Data.MICRO) do
		if v~=nil and v.entity~=nil then
			if v.entity.transform.IsChildOf(entity.transform) then return v end
		end
	end
	return nil
end
function RemoveGTS(entity)
	for i,v in pairs(Data.GTS) do
		if v~=nil and v.entity ~= nil and v.entity.id == entity.id then Data.GTS[i] = nil; return true end
	end
	return false
end
function ClearTargetters(entity,friendly_only)
	for k,v in pairs(Data.GTS) do
		if friendly_only and v.friendly and v.target~=nil and v.target.entity~=nil and v.target.entity.id == entity.id then
			v.target=nil
			v.state="Searching"
			v.lastState="None"
			Data.GTS[k]=v
		elseif not friendly_only and v.target~=nil and v.target.entity~=nil and v.target.entity.id == entity.id then
			v.target=nil
			v.state="Searching"
			v.lastState="None"
			Data.GTS[k]=v
		end
	end
end
function AquireTarget(gts,force_change,search_on_fail)
	if gts==nil or gts.entity==nil then return gts end
	local fc = false
	local sf = false
	if search_on_fail ~=nil then sf=search_on_fail end
	if force_change ~= nil then fc=force_change end
	if gts.target~=nil and gts.target.entity~=nil and not fc then return gts end
	local m = nil
	if gts.friendly then 
		m=GetMicroRider(gts.entity);
		if m~=nil then
			ClearTargetters(m.entity,true)
		end
	end
	m = GetNearestMicro(gts,gts.entity.position,gts.friendly,true,gts.friendly,gts.friendly)
	if m~= nil then
		gts.target=m
		gts.lastState = "None"
		if gts.friendly then gts.state="Protect" else gts.state="Hunting" end
	elseif sf then
		gts=ResetState(gts,"Searching")
	end
	return gts
end
function OperateGTS()
	for gts_i,gts in pairs(Data.GTS) do
		if gts ~= nil and gts.isAIEnabled then
			--Friendly GTS needs to change target IF there is a dead person within her FOV
			if gts.friendly then 
				if AllowedState(gts.state,gts.lastState,{"Wandering","Searching"},{"None"}) then gts=AquireTarget(gts,DeadEntities() > 0) end
				OperateFriendlyGTS(gts,gts_i) 
			else
				if AllowedState(gts.state,gts.lastState,{"Wandering","Searching"}) then gts=AquireTarget(gts) end
				OperateHostileGTS(gts,gts_i) 
			end
		end
	end
end
function OperateHostileGTS(gts,index)
	--print("Hostile GTS: "..gts.entity.name.." is in state: "..gts.state.." last state: "..gts.lastState.." state cooldown: "..gts.stateCooldown)
	if gts.state == "SpawnCooldown" and gts.stateCooldown <= -1 then gts.state="Searching"
	elseif IsInState(gts,"Wandering") then
		gts = StuckCheck(gts)
		if gts.lastState ~= "Wandering" then
			ClearActions(gts.entity)
			gts.entity.animation.Set(Settings.CannotBeEdited.GTSWalkAnimation)
			gts.entity.Wander()
			gts.lastState = "Wandering"
		end
		if gts.stateCooldown <= -1 then
			gts.stateCooldown = (5*60)
			if math.random(0,100) <= 5 then gts = ResetState(gts,"Searching") end
		end
	elseif IsInState(gts,"GetKnockedout") and gts.target~=nil and gts.target.state=="KnockoutTarget" then
		gts.entity.LookAt(gts.target.entity)
		if gts.lastState == "None" then
			ClearActions(gts.entity)
			gts.entity.transform.LookAt(gts.target.entity.transform)
			gts.entity.transform.rotation = Quaternion.Euler(0,gts.entity.transform.rotation.eulerAngles.y,0)
			gts.entity.transform.rotation = Quaternion.Euler(0,gts.entity.transform.rotation.eulerAngles.y,0)
			gts.entity.animation.Set("Knocked Out")
			gts.canBeKnockedOut=false
			gts.lastState = "WaitingForKOCompletion"
		elseif gts.lastState == "WaitingForKOCompletion" then
			if gts.entity.animation.Get() and gts.entity.animation.IsCompleted() then
				gts.lastState = "CooldownStart"
			end
		elseif gts.lastState == "CooldownStart" then
			gts.stateCooldown = (20*60)
			gts.lastState = "CooldownEnd"
		elseif gts.lastState == "CooldownEnd" then
			if gts.stateCooldown <= -1 then
				gts.lastState = "Recovering"
			end
		elseif gts.lastState == "Recovering" then
			gts.entity.animation.SetAndWait("Collapsed Recover")
			gts.entity.animation.SetAndWait("Scratch Head")
			gts.lastState = "WaitingForRecover"
		elseif gts.lastState == "WaitingForRecover" then
			if gts.entity.animation.Get() == "Scratch Head" and gts.entity.animation.IsCompleted() then
				gts.entity.animation.Set("Idle")
				gts.target=nil
				gts.state="Searching"
				gts.lastState = "None"
				gts.canBeKnockedOut=true
			end
		end
	elseif IsInState(gts,"Hunting") then
		if gts.lastState=="HuntingI" or gts.lastState=="None" then
			if gts.entity.ai.IsActionActive() then gts.entity.ai.StopAction() end
			if gts.entity.ai.IsBehaviorActive() then gts.entity.ai.StopBehavior() end
			gts.entity.animation.Set(Settings.CannotBeEdited.GTSWalkAnimation)
			gts.entity.MoveTo(gts.target.entity)
			gts.stateCooldown = (2*60)
			gts.lastState="HuntingWait"
		elseif gts.lastState=="HuntingWait" then
			local separation = (gts.entity.scale + gts.target.entity.scale) * 0.2
			local targetVector = gts.target.entity.position - gts.entity.position
			if targetVector.magnitude < separation * 1.4 then
				if gts.target.entity.IsDead() or gts.target.entity.IsCrushed() then --our target died when we were moving to them.. Skip to post-stomp
					gts.lastState="StompWait"
				else gts.lastState="Stomp" end
			elseif not gts.entity.ai.IsActionActive() and gts.stateCooldown <= -1 then gts.lastState="HuntingI" end
		elseif gts.lastState=="Stomp" then
			gts.entity.ai.StopAction()
			gts.entity.animation.Set(stompAnimModule.getRandomStompAnim())
			gts.entity.Stomp(gts.target.entity)
			gts.lastState="StompWait"
		elseif gts.lastState=="StompWait" then
			if not gts.entity.ai.IsActionActive() then
				if gts.target.entity.IsDead() or gts.target.entity.IsCrushed() then
					if gts.hungry then gts.lastState="Eating" else ClearActions(gts.entity); gts.state="Searching"; gts.target=nil; gts.lastState="None" end --if we are a hungry GTS move onto the eating behavior. if not just go to searching.
				else
					gts.lastState="HuntingWait" --completed stomping and failed to kill target, retrying..
				end
			end
		elseif gts.lastState=="Eating" then
			if gts.entity.ai.IsActionActive() then gts.entity.ai.StopAction() end
			if gts.entity.ai.IsBehaviorActive() then gts.entity.ai.StopBehavior() end
			gts.entity.animation.Set("Idle")
			gts.entity.ai.SetBehavior("GtsEatBHAS",gts.target.entity)
			gts.stateCooldown = (2*60)
			gts.lastState="EatingWait"
		elseif gts.lastState=="EatingWait" then
			if gts.stateCooldown<=-1 and not gts.entity.ai.IsActionActive() and not gts.entity.ai.IsBehaviorActive() then
				if gts.target==nil or gts.target.entity==nil then
					ClearActions(gts.entity); gts.state="Searching"; gts.target=nil; gts.lastState="None"; --we have consumed the target. Searching.
				else
					gts.lastState="None"; --Failed to consume target. Retrying Hunting..
				end
			end
		end
	elseif IsInState(gts,"Searching") then
		if gts.lastState ~= "Searching" then
			ClearActions(gts.entity)
			gts.entity.animation.SetAndWait("Searching Pockets")
			gts.entity.animation.SetAndWait("Look Around")
			gts.lastState = "Searching"
		elseif gts.entity.animation.Get() == "Look Around" and gts.entity.animation.IsCompleted() then
			gts.entity.animation.Set("Idle")
			gts = ResetState(gts,"Wandering")
		end
	elseif IsInState(gts,"Investigating") and gts.lostPosition ~= nil then
		if not Settings.GTSInvestigate then
			gts=ResetState(gts,"Wandering")
			Data.GTS[index]=gts
			return
		end
		if Vector3.Distance(gts.entity.position,gts.lostPosition) <= gts.entity.scale/2 then
			ClearActions(gts.entity)
			gts.entity.animation.Set("Idle")
			gts=ResetState(gts,"Searching")
		else
			gts = StuckCheck(gts)
			if Vector3.Distance(gts.entity.position,gts.lostPosition) > gts.entity.scale * 3 then
				if gts.lastState ~= "InvestigatingSprint" then
					ClearActions(gts.entity)
					gts.entity.animation.Set(Settings.CannotBeEdited.GTSRunAnimation[math.random(#Settings.CannotBeEdited.GTSRunAnimation)])
					gts.entity.MoveTo(gts.lostPosition)
					gts.lastState="InvestigatingSprint"
				end
			elseif Vector3.Distance(gts.entity.position,gts.lostPosition) <= gts.entity.scale * 2 then
				if gts.lastState ~= "InvestigatingWalk" then
					ClearActions(gts.entity)
					gts.entity.animation.Set(Settings.CannotBeEdited.GTSWalkAnimation)
					gts.entity.MoveTo(gts.lostPosition)
					gts.lastState="InvestigatingWalk"
				end
			end
		end
	end
	Data.GTS[index]=gts
end
function ToSameHeight(pos1,pos2) --makes it so pos1 has same height as pos2
	return Vector3.New(pos1.x,pos2.y,pos1.z)
end
function UpdateMicro(micro_data)
	for i,v in pairs(Data.MICRO) do
		if v~=nil and v.entity ~= nil and v.entity.id == micro_data.entity.id then
			Data.MICRO[i]=micro_data
			return;
		end
	end
end
function IsObserved(target,include_friendly)
	local o = false
	for _,v in pairs(Data.GTS) do
		if not include_friendly and not v.friendly and v.canBeKnockedOut then
			if v ~= nil and v.entity ~= nil and CanSeeTryCatch(v.entity,target) then o=true end
		elseif include_friendly and v.canBeKnockedOut then
			if v ~= nil and v.entity ~= nil and CanSeeTryCatch(v.entity,target) then o=true end
		end
	end
	for _,v in pairs(Data.SCOUT) do
		if v ~= nil and v.entity ~= nil and Vector3.Angle(target.transform.forward, v.entity.transform.position - Vector3.New(target.transform.position.x,v.entity.transform.position.y,target.transform.position.z)) < Settings.ScoutFOV then o=true end
	end
	return o
end
function GetBehindPosition(target, distanceBehind)
     return target.position - (target.forward * distanceBehind);
end
function GetFrontPosition(target, distanceAhead)
    return (target.position+(target.forward*distanceAhead))
end
function IsGTSNear(entity,distance,include_friendly)
	local o = false
	for _,v in pairs(Data.GTS) do
		if not include_friendly and not v.friendly and v.canBeKnockedOut then
			if v ~= nil and v.entity ~= nil and Vector3.Distance(v.entity.position,entity.position) <= distance then o=true end
		elseif include_friendly and v.canBeKnockedOut then
			if v ~= nil and v.entity ~= nil and Vector3.Distance(v.entity.position,entity.position) <= distance then o=true end
		end
	end
	return o
end
function CanFriendlyKnockout(gts)
	local ngts = GetNearestGTS(gts.entity.position,false,true)
	return ngts ~= nil and Vector3.Distance(ngts.entity.position,gts.entity.position) < gts.entity.scale and gts.canKnockOut
end
function SetTargetKnockout(gts,index)
	local ngts = GetNearestGTS(gts.entity.position,false,true)
	if ngts ~= nil and Vector3.Distance(ngts.entity.position,gts.entity.position) < gts.entity.scale and gts.canKnockOut then
		local tgi = GetGTSIndex(ngts.entity)
		local ngd = Data.GTS[tgi]
		if ngd~=nil and ngd.entity~=nil and ngd.entity.id == ngts.entity.id then
			gts.state="KnockoutTarget"
			gts.lastState="None"
			gts.entity.animation.Set("Idle")
			gts.target=ngts
			ngd.state="GetKnockedout"
			ngd.entity.LookAt(gts.entity)
			ngd.target=gts
			ngd.lastState="None"
			Data.GTS[index]=gts
			Data.GTS[tgi]=ngd
			ngd.entity.transform.LookAt(gts.entity.transform)
			gts.entity.transform.LookAt(ngd.entity.transform)
			gts.entity.transform.rotation = Quaternion.Euler(0,gts.entity.transform.rotation.eulerAngles.y,0)
			ngd.entity.transform.rotation = Quaternion.Euler(0,ngd.entity.transform.rotation.eulerAngles.y,0)
		end
	end
	return gts
end
function IsProtectClose(distance,gts) --returns 3 values: 1=Get closer,0=At distance,-1=Too Close
	if distance <= gts.entity.scale/3 then return -1
	elseif distance > gts.entity.scale/4 and distance <= gts.entity.scale/2 then return 0
	else return 1 end
end
function OperateFriendlyGTS(gts,index)
	gts=ReplaceTargetRider(gts)
	--print("Friendly: "..gts.entity.name.." is in state: "..gts.state.." last State: "..gts.lastState)
	if gts.state == "SpawnCooldown" and gts.stateCooldown <= -1 then gts.state="Wandering"
	elseif IsInState(gts,"MoveAway") then
		if gts.lastState ~= "MoveFrom" then
			ClearActions(gts.entity)
			gts.entity.animation.Set(Settings.CannotBeEdited.GTSWalkAnimation)
			gts.entity.Flee(gts.target.entity,0)
			gts.stateCooldown = (5*60)
			gts.lastState = "MoveFrom"
		elseif gts.stateCooldown <= -1 then
			ClearActions(gts.entity)
			gts=ResetState(gts,"Searching")
		end
	elseif IsInState(gts,"KnockoutTarget") and gts.target~=nil then
		gts.entity.LookAt(gts.target.entity)
		if gts.lastState == "Fleeing" or gts.lastState == "FleeTransition" then
			if gts.lastState ~= "Fleeing" then
				local target = gts.target
				ClearActions(gts.entity,true)
				gts.entity.animation.Set("Walk")
				gts.entity.Flee(target.entity,6)
				gts.stateCooldown=(5*60)
				gts.lastState = "Fleeing"
			elseif gts.stateCooldown<=-1 then --completed fleeing
				gts.entity.animation.Set("Idle")
				gts.canKnockOut=true
				gts=ResetState(gts,"Searching")
			end
		else
			if gts.lastState ~= "KnockoutTarget" then
				ClearActions(gts.entity,true)
				gts.canKnockOut = false
				gts.entity.animation.SetAndWait("Punch Combo")
				gts.entity.animation.SetAndWait("Victory Idle")
				gts.entity.transform.LookAt(gts.target.entity.transform)
				gts.entity.transform.rotation = Quaternion.Euler(0,gts.entity.transform.rotation.eulerAngles.y,0)
				gts.lastState = "KnockoutTarget"
			elseif gts.entity.animation.Get() == "Victory Idle" and gts.entity.animation.IsCompleted() then
				gts.lastState = "FleeTransition"
			end
		end
	elseif IsInState(gts,"Protect") then
		local tec = TargettingEntities(gts.target.entity)
		if tec > 1 then
			gts.target=nil
			gts.state="Searching"
			gts.lastState="None"
			Data.GTS[index]=gts
			return;
		end
		gts.entity.LookAt(gts.target.entity)
		if gts.lastState == "None" then
			if gts.target.entity.IsDead() or gts.target.entity.IsCrushed() then gts.lastState = "ProtectReviveGoto" else gts.lastState = "ProtectFollow" end
		elseif gts.lastState == "ProtectReviveGoto" then --this starts the protect revive branch
			ClearActions(gts.entity)
			gts.entity.animation.Set(Settings.CannotBeEdited.GTSWalkAnimation)
			gts.entity.MoveTo(gts.target.entity)
			gts.lastState = "WaitToArriveReviveGT"
		elseif gts.lastState == "WaitToArriveReviveGT" then
			if CanFriendlyKnockout(gts) then
				gts=SetTargetKnockout(gts,index)
				return;
			end
			local separationclose = (gts.entity.scale + gts.target.entity.scale) * 0.2
			local separationfar = (gts.entity.scale + gts.target.entity.scale) * 0.4
			local targetVector = gts.target.entity.position - gts.entity.position
			if targetVector.magnitude < separationfar * 1.4 and targetVector.magnitude >= separationclose * 1.4 then
				if not IsObserved(gts.target.entity,false) and not IsGTSNear(gts.target.entity,gts.entity.scale*3,false) then
					gts.lastState="ProtectReviveI"
				else gts.lastState="ProtectReviveIWait" end
			end
		elseif gts.lastState == "ProtectReviveIWait" then
			ClearActions(gts.entity)
			gts.entity.animation.Set("Sit 7")
			gts.lastState="ProtectReviveWait"
		elseif gts.lastState == "ProtectReviveWait" then
			if not IsObserved(gts.target.entity,false) and not IsGTSNear(gts.target.entity,gts.entity.scale*3,false) then
				gts.lastState="ProtectReviveI"
			end
		elseif gts.lastState == "ProtectReviveI" then
			ClearActions(gts.entity)
			gts.entity.animation.Set("Dig and Plant Seeds")
			gts.lastState="ProtectRevive"
		elseif gts.lastState=="ProtectRevive" then
			if gts.entity.animation.Get()=="Dig and Plant Seeds" and gts.entity.animation.IsCompleted() then
				gts.target.entity.StandUp()
				gts.lastState = "None"
			end
		elseif gts.lastState == "ProtectFollow" then --this starts the GTS follow of an alive protection
			if Vector3.Distance(ToSameHeight(gts.entity.position,gts.target.entity.position),gts.target.entity.position) <= gts.entity.scale then
				gts.lastState = "ProtectCrouchIWait"
			elseif Vector3.Distance(ToSameHeight(gts.entity.position,gts.target.entity.position),gts.target.entity.position) <= gts.entity.scale*Settings.FriendlyFollowMulti then
				gts.lastState = "ProtectFollowIWait"
			else gts.lastState = "ProtectFollowIGoto" end
		elseif gts.lastState == "ProtectFollowIWait" then
			ClearActions(gts.entity)
			gts.entity.animation.Set("Idle")
			gts.lastState = "ProtectFollowWait"
		elseif gts.lastState == "ProtectFollowWait" then
			if CanFriendlyKnockout(gts) then
				gts=SetTargetKnockout(gts,index)
				return;
			end
			if gts.target.entity.IsDead() or gts.target.entity.IsCrushed() then
				gts.lastState="ProtectReviveGoto"
			elseif Vector3.Distance(ToSameHeight(gts.entity.position,gts.target.entity.position),gts.target.entity.position) > gts.entity.scale*Settings.FriendlyFollowMulti then gts.lastState = "ProtectFollowIGoto"
			elseif gts.target.entity.isPlayer() and Vector3.Distance(ToSameHeight(gts.entity.position,gts.target.entity.position),gts.target.entity.position) <= gts.entity.scale then
				gts.lastState="ProtectCrouchIWait"
			end
		elseif gts.lastState == "ProtectFollowIGoto" then
			ClearActions(gts.entity)
			gts.entity.animation.Set(Settings.CannotBeEdited.GTSWalkAnimation)
			gts.entity.MoveTo(gts.target.entity)
			gts.lastState = "ProtectFollowGoto"
		elseif gts.lastState == "ProtectFollowGoto" then
			if Vector3.Distance(ToSameHeight(gts.entity.position,gts.target.entity.position),gts.target.entity.position) <= gts.entity.scale*Settings.FriendlyFollowMulti then gts.lastState = "None" end --end of follow path
		elseif gts.lastState == "ProtectCrouchIWait" then --start of crouch flee path (this branch only works for player targets)
			ClearActions(gts.entity)
			gts.entity.animation.Set("Sit 7")
			gts.lastState = "ProtectCrouchWait"
		elseif gts.lastState == "ProtectCrouchWait" then
			if CanFriendlyKnockout(gts) then
				gts=SetTargetKnockout(gts,index)
				return;
			end
			if gts.target.entity.IsDead() or gts.target.entity.IsCrushed() then
				gts.lastState="ProtectReviveCheck"
			else
				if (Game.GetLocalPlayerSettings().climbing or gts.target.entity.transform.IsChildOf(gts.entity.transform)) and gts.target.entity.position.y >= (gts.entity.position.y+gts.entity.scale/2) then
					gts.lastState = "ProtectIFleeFrom"
				elseif Vector3.Distance(ToSameHeight(gts.entity.position,gts.target.entity.position),gts.target.entity.position) > gts.entity.scale*Settings.FriendlyFollowMulti then gts.lastState = "ProtectFollowIGoto" end
			end
		elseif gts.lastState == "ProtectIFleeFrom" then
			if (Game.GetLocalPlayerSettings().climbing or gts.target.entity.transform.IsChildOf(gts.entity.transform)) and gts.target.entity.position.y >= (gts.entity.position.y+gts.entity.scale/2) then
				local m = GetNearestGTS(gts.entity.position,false,true)
				if m~= nil and Vector3.Distance(m.entity.position,gts.entity.position) <= gts.entity.scale*8 then
					ClearActions(gts.entity)
					gts.entity.animation.Set(Settings.CannotBeEdited.GTSRunAnimation[math.random(#Settings.CannotBeEdited.GTSRunAnimation)])
					gts.entity.Flee(m.entity,6)
					gts.stateCooldown = (5*60)
					gts.lastState = "ProtectWFleeFrom"
				elseif m~= nil then gts.lastState = "ProtectPIFleeFrom" else
					gts.lastState="ProtectRI"
				end
			else
				gts.lastState = "ProtectCrouchIWait"
			end
		elseif gts.lastState == "ProtectRI" then
			ClearActions(gts.entity)
			if gts.entity.animation.Get()==Settings.CannotBeEdited.GTSWalkAnimation then
				gts.entity.animation.Set(Settings.CannotBeEdited.GTSWalkAnimation)
			end
			gts.entity.Wander(4)
			gts.stateCooldown = (3*60)
			gts.lastState = "ProtectRIWait"
		elseif gts.lastState == "ProtectRIWait" then
			if gts.stateCooldown <= -1 then
				gts.lastState = "ProtectIFleeFrom"
			end
		elseif gts.lastState == "ProtectWFleeFrom" then
			if gts.stateCooldown <= -1 then gts.lastState="None" end
		elseif gts.lastState == "ProtectPIFleeFrom" then
			ClearActions(gts.entity)
			gts.entity.animation.Set("Reflesh")
			gts.stateCooldown = (5*60)
			gts.lastState = "ProtectPFleeFrom"
		elseif gts.lastState == "ProtectPFleeFrom" then
			if gts.stateCooldown <= -1 then
				if (Game.GetLocalPlayerSettings().climbing or gts.target.entity.transform.IsChildOf(gts.entity.transform)) and gts.target.entity.position.y >= (gts.entity.position.y+gts.entity.scale/2) then
					gts.lastState = "ProtectTIFleeFrom"
				else
					gts.lastState = "ProtectIFleeFrom"
				end
			end
		elseif gts.lastState == "ProtectTIFleeFrom" then
			ClearActions(gts.entity)
			gts.entity.animation.Set("Thinking 2")
			gts.stateCooldown = (5*60)
			gts.lastState = "ProtectPFleeFrom"
		elseif gts.lastState == "ProtectTFleeFrom" then
			if gts.stateCooldown <= -1 and gts.entity.animation.Get() == "Thinking 2" and gts.entity.animation.IsCompleted() then
				if (Game.GetLocalPlayerSettings().climbing or gts.target.entity.transform.IsChildOf(gts.entity.transform)) and gts.target.entity.position.y >= (gts.entity.position.y+gts.entity.scale/2) then
					gts.lastState = "ProtectTIFleeFrom"
				else
					gts.lastState = "ProtectIFleeFrom"
				end
			end
		end
	elseif IsInState(gts,"Wandering") then
		if CanFriendlyKnockout(gts) then
			gts=SetTargetKnockout(gts,index)
			return;
		end
		gts = StuckCheck(gts)
		if gts.lastState ~= "Wandering" then
			ClearActions(gts.entity)
			gts.entity.animation.Set(Settings.CannotBeEdited.GTSWalkAnimation)
			gts.entity.Wander()
			gts.lastState = "Wandering"
		end
		if gts.stateCooldown <= -1 then
			gts.stateCooldown = (5*60)
			if math.random(0,100) <= 5 then gts = ResetState(gts,"Searching") end
		end
	elseif IsInState(gts,"Searching") then
		if CanFriendlyKnockout(gts) then
			gts=SetTargetKnockout(gts,index)
			return;
		end
		if gts.lastState ~= "TurningAround" then
			if gts.lastState ~= "Searching" then
				ClearActions(gts.entity)
				gts.entity.animation.SetAndWait("Searching Pockets")
				gts.entity.animation.SetAndWait("Look Around")
				gts.lastState = "Searching"
			elseif gts.entity.animation.Get() == "Look Around" and gts.entity.animation.IsCompleted() then
				gts.entity.animation.Set("Idle")
				gts=ResetState(gts,"Wandering")
			end
		else
			if gts.lastState ~= "TurnedAround" then
				ClearActions(gts.entity)
				local pos = GetBehindPosition(gts.entity.transform,gts.entity.scale)
				gts.lostPosition = pos
				gts.entity.animation.Set("Walk")
				gts.entity.MoveTo(pos)
				gts.lastState = "TurnedAround"
			elseif gts.lostPosition~= nil and Vector3.Distance(gts.entity.position,gts.lostPosition) <= 1 then
				if gts.lastState ~= "LookedBehind" then
					ClearActions(gts.entity)
					gts.entity.animation.SetAndWait("Searching Pockets")
					gts.entity.animation.SetAndWait("Look Around")
					gts.lastState = "LookedBehind"
				elseif gts.entity.animation.Get() == "Look Around" and gts.entity.animation.IsCompleted() then
					gts = ResetState(gts,"Wandering")
				end
			end
		end
	elseif IsInState(gts,"Investigating") and gts.lostPosition ~= nil then
		if CanFriendlyKnockout(gts) then
			gts=SetTargetKnockout(gts,index)
			return;
		end
		if not Settings.GTSInvestigate then
			gts=ResetState(gts,"Wandering")
			Data.GTS[index]=gts
			return
		end
		if Vector3.Distance(gts.entity.position,gts.lostPosition) <= gts.entity.scale/2 then
			ClearActions(gts.entity)
			gts.entity.animation.Set("Idle")
			gts=ResetState(gts,"Searching")
		else
			gts = StuckCheck(gts)
			if Vector3.Distance(gts.entity.position,gts.lostPosition) > gts.entity.scale * 3 then
				if gts.lastState ~= "InvestigatingSprint" then
					ClearActions(gts.entity)
					gts.entity.animation.Set(Settings.CannotBeEdited.GTSRunAnimation[math.random(#Settings.CannotBeEdited.GTSRunAnimation)])
					gts.entity.MoveTo(gts.lostPosition)
					gts.lastState="InvestigatingSprint"
				end
			elseif Vector3.Distance(gts.entity.position,gts.lostPosition) <= gts.entity.scale * 2 then
				if gts.lastState ~= "InvestigatingWalk" then
					ClearActions(gts.entity)
					gts.entity.animation.Set(Settings.CannotBeEdited.GTSWalkAnimation)
					gts.entity.MoveTo(gts.lostPosition)
					gts.lastState="InvestigatingWalk"
				end
			end
		end
	end
	Data.GTS[index]=gts
end
function MicroGetNearestGTS(micro)
	local m = nil
	local d = math.huge
	for _,v in pairs(Data.GTS) do
		if v~=nil and v.entity ~= nil and (CanSeeTryCatch(v.entity,micro.entity) or CanSeeTryCatch(micro.entity,v.entity)) then
			if Vector3.Distance(v.entity.position,micro.entity.position) < d then
				d = Vector3.Distance(v.entity.position,micro.entity.position)
				m = v
			end
		end
	end
	return m
end
function BooleanToString(bool,capitalize)
	if bool ~= true and bool ~= false then return "false" end
	if bool then if capitalize then return "True" else return "true" end
	else if capitalize then return "False" else return "false" end end
	return "false"
end
function OperateMicro()
	for micro_i,micro in pairs(Data.MICRO) do
		if micro~=nil and micro.isAI and micro.isAIEnabled then micro=OperateNoise(micro) end
		if micro ~= nil and micro.isAI and micro.isAIEnabled and not (micro.entity.IsDead() or micro.entity.IsCrushed()) then
			if micro.target == nil or micro.target.entity == nil then
				local m = MicroGetNearestGTS(micro)
				if m~=nil then
					micro.target = m
					if m.friendly then micro.state = "RoamTowards" else micro.state="FleeFrom" end
					micro.fleeTimer = (30*60)
					micro.lastState = "None"
					micro.hidden=false
				elseif micro.state ~= "Wandering" then
					micro.target=nil
					micro.state = "Wandering"
					micro.lastState = "None"
					micro.hidden=false
				end
			elseif micro.state == "FleeFrom" then
				local m = MicroGetNearestGTS(micro)
				if m~=nil and m.entity.id ~= micro.target.entity.id then
					micro.target = m
					if m.friendly then micro.state = "RoamTowards" else micro.state="FleeFrom" end
					micro.fleeTimer = (30*60)
					micro.lastState = "None"
					micro.hidden=false
				elseif m == nil and micro.state ~= "Wandering" then
					micro.target=nil
					micro.state = "Wandering"
					micro.lastState = "None"
					micro.hidden=false
				end
			end
			--print(micro.entity.name.." is in state: "..micro.state.." last State: "..micro.lastState.." state Cooldown: "..micro.stateCooldown)
			micro.stateCooldown = math.clamp(micro.stateCooldown-1,-1,math.huge)
			micro.fleeTimer = math.clamp(micro.fleeTimer-1,-1,math.huge)
			if IsTargetted(micro.entity,false,false) and IsTargetted(micro.entity,false,true) and micro.state~="Fear" then --this means both a friendly and hostile are targetting this micro
				micro.state="Fear";
				micro.lastState="None"
				micro.target=GetTargetter(micro.entity,true,true)
			end
			if micro.state == "SpawnCooldown" then if micro.stateCooldown <= -1 then micro.state="Wandering" end
			elseif IsInState(micro,"Fear") and micro.target~=nil then
				if micro.lastState=="None" or micro.lastState=="Goto" then
					micro.entity.animation.Set(Settings.CannotBeEdited.MicroRunAnimation[math.random(#Settings.CannotBeEdited.MicroRunAnimation)])
					micro.entity.MoveTo(micro.target.entity,0)
					micro.lastState="GotoWait"
				elseif micro.lastState=="GotoWait" then
					local separationclose = (micro.entity.scale + micro.target.entity.scale) * 0.2
					local separationfar = (micro.entity.scale + micro.target.entity.scale) * 0.4
					local targetVector = micro.target.entity.position - micro.entity.position
					if targetVector.magnitude < separationfar * 1.4 and targetVector.magnitude >= separationclose * 1.4 then
						micro.lastState="Cower"
					elseif not micro.entity.ai.IsActionActive() then
						micro.lastState="Goto"
					end
				elseif micro.lastState=="Cower" then
					local s,am=pcall(function() 
						local separationclose = (micro.entity.scale + micro.target.entity.scale) * 0.2
						local separationfar = (micro.entity.scale + micro.target.entity.scale) * 0.4
						local targetVector = micro.target.entity.position - micro.entity.position
						if not targetVector.magnitude < separationfar * 1.4 and not targetVector.magnitude >= separationclose * 1.4 then
							micro.lastState="Goto"
						elseif not micro.entity.animation.Get()~="Terrified" then
							micro.entity.animation.Set("Terrified")
						end
						return micro
					end)
					if not s then micro.state="Wandering" elseif am~=nil then micro=am end
				end
			elseif IsInState(micro,"Wandering") then
				if math.random(0,100) <= 75 and micro.stateCooldown <= -1 then
					micro.lastState = "TransitionCrawlWander"
					micro.hidden=false
					micro.stateCooldown = (3*60)
				elseif math.random(0,100) <= 75 and micro.stateCooldown <= -1 and not IsTargetted(micro.entity,false,false) then
					micro.lastState = "TransitionFakeDead"
					micro.hidden=true
					micro.stateCooldown = (10*60)
				elseif micro.stateCooldown <= -1 then
					micro.hidden=false
					micro.lastState = "TransitionWander"
					micro.stateCooldown = (3*60)
				end
				if micro.lastState ~= "CrawlWander" and micro.lastState == "TransitionCrawlWander" then
					ClearActions(micro.entity)
					micro.entity.animation.Set("Low Crawl")
					micro.entity.Wander()
					micro.lastState = "CrawlWander"
				elseif micro.lastState ~= "FakeDead" and micro.lastState == "TransitionFakeDead" then
					ClearActions(micro.entity)
					micro.entity.animation.SetAndWait("Falling Down")
					micro.entity.animation.SetAndWait("Down")
					micro.lastState = "FakeDead"
				elseif micro.lastState ~= "Wander" and micro.lastState == "TransitionWander" then
					ClearActions(micro.entity)
					micro.entity.animation.Set(Settings.CannotBeEdited.MicroWalkAnimation)
					micro.entity.Wander()
					micro.lastState = "Wander"
				elseif micro.lastState == "FakeDead" and IsTargetted(micro.entity,false,false) then micro.stateCooldown=-1; micro.lastState="TransitionWander" end
			elseif IsInState(micro,"RoamTowards") and micro.target~=nil and micro.stateCooldown <= -1 then
				if math.random(0,4) <= 1 then
					if micro.lastState ~= "FleeTo" then
						ClearActions(micro.entity)
						micro.entity.animation.Set(Settings.CannotBeEdited.MicroWalkAnimation)
						micro.entity.MoveTo(micro.target.entity)
						micro.lastState = "FleeTo"
						micro.stateCooldown = (5*60)
					end
				else
					if micro.lastState ~= "FleeFrom" then
						ClearActions(micro.entity)
						micro.entity.animation.Set(Settings.CannotBeEdited.MicroRunAnimation[math.random(#Settings.CannotBeEdited.MicroRunAnimation)])
						micro.entity.Flee(micro.target.entity,0)
						micro.lastState = "FleeFrom"
						micro.stateCooldown = (5*60)
					end
				end
			elseif IsInState(micro,"FleeFrom") then
				micro = StuckCheck(micro)
				if micro.lastState ~= "FleeFrom" then
					ClearActions(micro.entity)
					micro.entity.animation.Set(Settings.CannotBeEdited.MicroRunAnimation[math.random(#Settings.CannotBeEdited.MicroRunAnimation)])
					micro.entity.Flee(micro.target.entity,0)
					micro.lastState = "FleeFrom"
				end
			end
			Data.MICRO[micro_i]=micro
		end
	end
end
function math.Lerp(o,s,t)
	return Vector3.Lerp(Vector3.New(o,0,0),Vector3.New(s,0,0),t).x
end
function AquireHeliTarget(scout)
	if scout.target~= nil and scout.target.entity~=nil then return scout end
	local m = GetNearestMicro(scout,scout.entity.position,false,true,false,false,{},true)
	if m~=nil then
		scout.state="Hunting"
		scout.lastState="None"
		scout.target=m
	end
	return scout
end
function GetNearestScout(scout)
	local m = nil
	local d = math.huge
	for _,v in pairs(Data.SCOUT) do
		if v~=nil and v.entity~=nil and v.entity.id ~= scout.entity.id then
			if Vector3.Distance(v.entity.position,scout.entity.position) < d then
				m=v
				d=Vector3.Distance(v.entity.position,scout.entity.position)
			end
		end
	end
	return m,d
end
function CollisionWarningCheck(scout)
	local ns,nsd = GetNearestScout(scout)
	if ns ~= nil and nsd < 15 then --uh oh we are too close to another scout!
		scout.state="FleeFrom"
		scout.target=ns
		scout.lastState="None"
	end
	return scout
end
function AllowedHeliState(state)
	return (state == "Wandering")
end
function ConfirmTarget(data)
	local changed=false
	if data.hungry then
		if data.target == nil or data.target.entity==nil or data.target.entity.IsDead() or data.target.entity.IsCrushed() then
			data.target=nil
			if pcall(function() data.entity.animation.GetProgress() end) then
				data.state="Searching"
			else data.state="Wandering" end
			data.lastState = "None"
			changed=true
		end
	else
		if data.target == nil or data.target.entity==nil then
			data.target=nil
			if pcall(function() data.entity.animation.GetProgress() end) then
				data.state="Searching"
			else data.state="Wandering" end
			data.lastState = "None"
			changed=true
		end
	end
	return data,changed
end
function OperateScout()
	for scout_i,scout in pairs(Data.SCOUT) do
		if scout ~= nil and scout.entity ~= nil then
			if scout.target ~= nil and scout.target.cannotBeTargetted then scout=ResetState(scout,"Wandering") end
			if AllowedHeliState(scout.state) and scout.canChangeTargets then scout=AquireHeliTarget(scout) end
			scout.stateCooldown = math.clamp(scout.stateCooldown-1,-1,math.huge)
			scout.positionCheckTimer = math.clamp(scout.positionCheckTimer-1,-1,math.huge)
			--print("Scout: "..scout_i.." in state: "..scout.state.." last state: "..scout.lastState.." state cooldown: "..scout.stateCooldown)
			local targetPosition = nil
			local canMoveForward = true
			if scout.state == "SpawnCooldown" then if scout.stateCooldown <=-1 then scout=ResetState(scout,"Wandering") end
			elseif IsInState(scout,"Wandering") then
				scout=CollisionWarningCheck(scout)
				if scout.lastState ~= "Wandering" then
					scout.wanderPosition = Vector3.New(scout.entity.position.x+math.random(-100,100),Settings.ScoutCruiseAlt,scout.entity.position.z+math.random(-100,100))
					scout.lastState = "Wandering"
				elseif scout.wanderPosition ~= nil then
					targetPosition=scout.wanderPosition
				end
			elseif IsInState(scout,"FleeFrom") and scout.target ~= nil then
				if scout.target==nil or scout.target.entity==nil or scout.target.entity.IsCrushed() or scout.target.entity.IsDead() then scout.target=nil; scout.state="Wandering";scout.lastState="None"
				else
					direction = (scout.entity.transform.position - scout.target.entity.transform.position).normalized;
					targetPosition=Vector3.New(scout.entity.position.x,scout.target.entity.position.y+scout.target.entity.scale*1.5,scout.entity.position.z)+direction*Settings.ScoutSpeed
				end
			elseif IsInState(scout,"Hunting") then
				--Need scouts to hunt the target stopping at separation distance and readjusting height due to nearby GTS
				scout,changed=ConfirmTarget(scout)
				if not changed then
					if scout.lastState == "None" then
						if scout.target==nil or scout.target.entity==nil or scout.target.entity.IsCrushed() or scout.target.entity.IsDead() then scout.state="Wandering"; scout.lastState="None"; scout.target=nil
						else scout.lastState="CheckDistance" end
					end if scout.lastState=="CheckDistance" then
						if scout.target==nil or scout.target.entity==nil or scout.target.entity.IsCrushed() or scout.target.entity.IsDead() then scout.state="Wandering"; scout.lastState="None"; scout.target=nil
						else
							local separation = (scout.entity.scale + scout.target.entity.scale) * 4
							local targetVector = ToSameHeight(scout.target.entity.position,scout.entity.position) - scout.entity.position
							if targetVector.magnitude < separation * 8 then
								local off = 100
								local m = GetNearestGTS(scout.entity.position,true,true)
								if m~=nil and m.entity~=nil and Vector3.Distance(scout.entity.position,m.entity.position) <= (scout.entity.scale+m.entity.scale)*2 then
									off=m.entity.position.y+m.entity.scale*1.5
								end
								targetPosition=Vector3.New(scout.entity.position.x,scout.target.entity.position.y+off,scout.entity.position.z); 
								canMoveForward=false
							else
								local off = 100
								local m = GetNearestGTS(scout.entity.position,true,true)
								local s = GetNearestScout(scout)
								if (s ~=nil and s.entity~=nil and Vector3.Distance(scout.entity.position,s.entity.position) <= (scout.entity.scale+s.entity.scale)*2) or (m ~=nil and m.entity~=nil and Vector3.Distance(scout.entity.position,m.entity.position) <= (scout.entity.scale+m.entity.scale)) then
									if s~=nil and s.entity~=nil then scout.target=s else scout.target=m end
									scout.state="FleeFrom"
								else
									if m~=nil and m.entity~=nil and Vector3.Distance(scout.entity.position,m.entity.position) <= (scout.entity.scale+m.entity.scale)*2 then
										off=m.entity.position.y+m.entity.scale*1.5
									end
									targetPosition=scout.target.entity.position+Vector3.New(0,off,0)
								end
							end
						end
					end
				end
			elseif IsInState(scout,"Investigating") then
				scout=CollisionWarningCheck(scout)
				local m = GetNearestGTS(scout.entity.position,true)
				if m ~= nil and Vector3.Distance(Vector3.New(m.entity.position.x,scout.entity.position.y,m.entity.position.z),scout.entity.position) <= m.entity.scale*2 then
					targetPosition = scout.lostPosition + Vector3.New(0,m.entity.scale*3,0)
				elseif m ~= nil and Vector3.Distance(Vector3.New(m.entity.position.x,scout.entity.position.y,m.entity.position.z),scout.entity.position) > m.entity.scale*3 then
					targetPosition = scout.lostPosition + Vector3.New(0,25,0)
				end
			end

			--Controls movement
			if targetPosition~=nil then
				if Vector3.Distance(scout.entity.position,targetPosition) < 10 then 
					scout.hasArrived = true 
				elseif scout.positionCheckTimer <= -1 then 
					scout.positionCheckTimer = (5*12)
					scout.hasArrived = false 
				end
			end
			if targetPosition ~= nil and not scout.hasArrived then
				local distance = Vector3.Distance(scout.entity.position,ToSameHeight(scout.entity.position,targetPosition))
				if distance > 10 then
					local scoutTilt = math.Lerp(scout.tiltMotion,Settings.ScoutSpeed,Time.deltaTime)
					if not canMoveForward then 
						scoutTilt = math.Lerp(scout.tiltMotion,0,Time.deltaTime)
					end			
					local ftilt = (scoutTilt/Settings.ScoutSpeed)*30	
					scout.tiltMotion = scoutTilt
					local scoutSpeed = math.Lerp(scout.forwardMotion,Settings.ScoutSpeed,Time.deltaTime/4)
					scout.forwardMotion = scoutSpeed
					scout.entity.transform.position = Vector3.Lerp(scout.entity.transform.position,Vector3.New(scout.entity.transform.position.x,scout.entity.transform.position.y+math.clamp(targetPosition.y-scout.entity.transform.position.y,-scoutSpeed,scoutSpeed),scout.entity.transform.position.z),scoutSpeed)
					scout.entity.transform.rotation = Quaternion.Lerp(scout.entity.transform.rotation,Quaternion.Euler(ftilt,scout.entity.transform.rotation.eulerAngles.y,scout.entity.transform.rotation.eulerAngles.z),scoutTilt)
				else
					local scoutSpeed = math.Lerp(scout.forwardMotion,Settings.ScoutSpeed,Time.deltaTime/4)
					local scoutTilt = math.Lerp(scout.tiltMotion,Settings.ScoutSpeed,Time.deltaTime/2)
					local targetRot = scout.entity.transform.rotation
					direction = (targetPosition - scout.entity.transform.position).normalized;
					lookRotation = Quaternion.LookRotation(direction);
					targetRot=lookRotation
					scout.entity.transform.rotation = Quaternion.Slerp(scout.entity.transform.rotation, targetRot, scoutSpeed/25);
					scout.entity.transform.rotation = Quaternion.Euler(0,scout.entity.transform.rotation.eulerAngles.y,0)
					--Manages flying up
					scout.entity.transform.position = Vector3.Lerp(scout.entity.transform.position,Vector3.New(scout.entity.transform.position.x,scout.entity.transform.position.y+math.clamp(targetPosition.y-scout.entity.transform.position.y,-scoutSpeed,scoutSpeed),scout.entity.transform.position.z),scoutSpeed)
					--Manages our forward movement alt change
					scout.forwardMotion = scoutSpeed
					scout.tiltMotion = scoutTilt
					scout.entity.transform.rotation = Quaternion.Lerp(scout.entity.transform.rotation,Quaternion.Euler((scoutTilt/Settings.ScoutSpeed)*30,scout.entity.transform.rotation.eulerAngles.y,scout.entity.transform.rotation.eulerAngles.z),scoutTilt)
				end
			else
				local scoutTilt = math.Lerp(scout.tiltMotion,0,Time.deltaTime)
				scout.tiltMotion = scoutTilt
				scout.entity.transform.rotation = Quaternion.Lerp(scout.entity.transform.rotation,Quaternion.Euler((scoutTilt/Settings.ScoutSpeed)*30,scout.entity.transform.rotation.eulerAngles.y,scout.entity.transform.rotation.eulerAngles.z),scoutTilt)
			end
			local forward = (scout.entity.transform.position+(scout.entity.transform.forward*math.clamp(scout.entity.transform.rotation.eulerAngles.x/30,0,Settings.ScoutSpeed)))
			local opos = scout.entity.transform.position
			scout.entity.transform.position = Vector3.Lerp(scout.entity.transform.position,forward,math.Lerp(scout.forwardMotion,Settings.ScoutSpeed,Time.deltaTime/4))
			scout.entity.transform.position = Vector3.New(scout.entity.transform.position.x,opos.y,scout.entity.transform.position.z)
			Data.SCOUT[scout_i]=scout
		end
	end
end
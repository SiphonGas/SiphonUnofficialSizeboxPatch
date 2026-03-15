--By Debolte, 220922; Based on the original by @SUCC-u-lent#3660, 210304.

allinone = RegisterBehavior("allinone")
allinone.data = {
    menuEntry = "Size/All In One",
    secondary = true,
	flags = { "AllInOneGrow" },
    agent = {
        type = { "humanoid" }
    },
    target = {
        type = { "oneself" }
    },
    settings = {
        {"leg", "Leg Expansion Mode", "bool", false},
        {"breast", "Breast Expansion Mode", "bool", true},
        {"regular", "Normal Growth Mode", "bool", true},
        {"linear", "Linear Growth", "bool", false},
		{"legBoneNames", "Leg Bone Names", "string", "Hip"},
		{"breastBoneNames", "Breasts Bone Names", "string", "Breast,Ichichi"},
		{"speed", "Rate of Change", "float", "0.05", {"0.03", "0.5"}},
		{"plusKey", "Grow Key", "string", "="},
		{"minusKey", "Shrink Key", "string", "-"},
		{"onlySelected", "Affect only Selected", "bool", true},
		--{"limit", "Limit Size", "float", "3", {"1.2", "8"}}
    }
}

function allinone:Start()
	PrepModes(self)
end
function allinone:Update()
	if AffectedEntity(self) and (Input.GetKey(self.plusKey) or Input.GetKey(self.minusKey)) then
		growBody(self)
	end
end

function PrepModes(self)
	self.legBoneNames,self.breastBoneNames = not self.leg or self.legBoneNames, not self.breast or self.breastBoneNames
	local modes = {leg = self.legBoneNames, breast = self.breastBoneNames} --Note: the table keys here are self.bone[keys]

	for i,mode in pairs(modes) do
		if type(mode) == "boolean" then
			modes[i] = nil
		end
	end

	if next(modes) == nil then return end

	local missing
	self.bones, missing = FindBoneNames(self.agent, modes)
	if missing then
		self.MissingWarning = Game.Toast.New()
		self.MissingWarning.Print("Missing bones. Check console for details (F12).")
		self.MissingWarning = nil
		print(MissingBones(missing))
	end

	if not self.bones then self.agent.ai.StopSecondaryBehavior("AllInOneGrow") end
end

function FindBoneNames(entity, boneNames)
	local foundBones = {}
	local missing = false

	for boneTypeIdx,boneType in pairs(boneNames) do
		local matchFound = false
		foundBones[boneTypeIdx] = {}
		for boneName in string.gmatch(boneType, '([^,]+)') do
			local matchingBones = entity.bones.GetBonesByName(boneName, true)
			if matchingBones then
				for i,v in ipairs(matchingBones) do
					table.insert(foundBones[boneTypeIdx], v)
				end
				matchFound = true
			end
		end
		if not matchFound then
			if not missing then missing = {} end
			missing[#missing + 1] = boneTypeIdx
			foundBones[boneTypeIdx] = nil
		end
	end

	if next(foundBones) == nil then
		return nil,""
	else
		return foundBones,missing
	end
end

function MissingBones(missing)
	if type(missing) == "table" then
		if #missing > 1 then

			local strings = {}

			for i,v in pairs(missing) do
				if i <= 2 then
					if i == 1 then
						strings[i] = v.." "
					else
						strings[i] = v.." or "
					end
				else
					strings[i] = v..", "
				end
			end

			missing = strings[1]
			for i=2, #strings do
				missing = strings[i]..missing
			end

		else
			missing = missing[1].." "
		end
	end

	missing = "No "..missing.."bones were found. Use the Model Editor to find the proper bone names for your model, and add those in the Behavior Manager Settings!"
	return missing
end

function AffectedEntity(self)
	if self.onlySelected then
		return self.agent.GetSelectedEntity() == self.agent
	else
		return true
	end
end

function growBody(self)
	if self.leg or self.breast then
		for boneType,boneTypeList in pairs(self.bones) do
			for boneIdx,bone in ipairs(boneTypeList) do
				--if bone.localScale.y < self.limit then
					bone.localScale = growBodyPart(self,bone.localScale,true)
				--end
			end
		end
	end
	if self.regular then
		self.agent.scale = growBodyPart(self,self.agent.scale)
	end
end

function growBodyPart(self,part,isBone)
	local polarity = 1
	if Input.GetKey(self.minusKey) then polarity = -1 end
	local growAmount = self.speed * polarity * Time.deltaTime
	if self.linear then
		if isBone then growAmount = Vector3.New(growAmount,growAmount,growAmount) end
		return part + growAmount
	else
		return part * (1 + growAmount)
	end
end

allinonestop = RegisterBehavior("allinonestop")
allinonestop.data = {
    menuEntry = "Size/All In One (X)",
    secondary = true,
	flags = { "AllInOneGrow" },
    agent = {
        type = { "humanoid" }
    },
    target = {
        type = { "oneself" }
    },
}
function allinonestop:Start()
	self.agent.ai.StopSecondaryBehavior("AllInOneGrow")
    print("Stopped All In One")
end
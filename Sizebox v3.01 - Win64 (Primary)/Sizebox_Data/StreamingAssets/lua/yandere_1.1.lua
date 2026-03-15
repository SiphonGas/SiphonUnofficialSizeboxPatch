Yandere = RegisterBehavior("Yandere")
Yandere.data = {
	menuEntry = "Scenario/Yandere 1.1",
	agent = {
        type = { "giantess"}
    },
    target = {
        type = {"micro"}
    },
    settings = {
    	{"distanceToKill", "Distance To Kill", "string", "4"}, --size of radius in which to search for unwanted micros; corresponds to height of GTS
    	{"stressDistance", "Stress Distance", "string", "1.5"} --size of radius beyond which the GTS will catch up to player
    }
}

micros = {}
embarAnimations = {"Idle 3", "Idle 3", "Idle 3", "Idle 3", "Idle 4", "Idle 4", "Idle 4", "Breathing Idle", "Breathing Idle", "Happy", "Happy", "Embar", "Embar", "Embar 2", "Scratch Head", "Waving", "Greet 2", "Crouch Idle"}
walkAnimation = "Walk"
idleAnimation = "Idle 2"

stompAnimModule = require "stomp_anim"

function Yandere:Start()
    --(In this script, I refer to the target, the 'crush' of the yandere, as the 'player', for clarity's sake; the 'targets to kill' are the surrounding micros.)--

	--Only do randomseed once per scene
    if not globals["yandereRand"] then math.randomseed(tonumber(os.time()) + self.agent.id) globals["yandereRand"] = true end

	self.distanceToKill = tonumber(self.distanceToKill)
	self.stressDistance = tonumber(self.stressDistance)
	self.agent.dict.yandereActive = true --used for Stop behavior log
	self.animTransSpeed = self.agent.animation.transitionDuration --stores default animation transition duration, to be restored at the end
	self.seeking = false
	self.microToKill = nil

	self.agent.LookAt(self.target)
	self.agent.animation.Set(walkAnimation)
	self.agent.MoveTo(self.target)
	self.agent.animation.Set(idleAnimation)
	self.firstWalk = true --used to make GTS pause at player if there are micros around, before going to them (instead of going straight away, GTS stares at player for a couple seconds)
end

function Yandere:Update()
	
	micros[1] = nil

	--IF PLAYER DEAD
	if self.missionFailed then
		if (Time.time > self.timer + 1 and self.agent.animation.GetProgress() >= 1 and not self.agent.animation.IsInTransition()) or self.failed3 or self.agent.animation.Get() == idleAnimation then
			if not self.failed1 then
				self.agent.animation.Set("Idle 5")
				log("...")
				self.failed1 = true
			elseif not self.failed2 then
				self.agent.animation.Set("Idle")
				log("!")
				self.failed2 = true
				self.failed3 = true
				self.agent.Wait(1)
			elseif self.failed3 then
				self.agent.animation.Set("Falling Down")
				self.failed3 = false
			elseif not self.failed4 then
				self.failed4 = false
				log("You crush got crushed... whoops.")
				self.agent.ai.StopBehavior()
			end
			self.timer = Time.time
		end
		return
	end

	--AFTER MOVING TO PLAYER or AFTER A SUCCESSFUL STOMP
	if not self.agent.ai.IsActionActive() then
		
		--IF GTS HAS TARGET
		if self.microToKill ~= nil then
			
			--IS IT DEAD?   
			  --Yes. Mission compree, return to player.
			if self.microToKill.IsDead() then
				self.seeking = false
			   
			  --No. Seek and eliminate.
			elseif not self.firstWalk or Time.time > self.timer then
				if self.firstWalk then self.firstWalk = nil end
				self.agent.LookAt(self.microToKill)
				self.agent.animation.Set(walkAnimation)
				self.agent.MoveTo(self.microToKill)
				self.stompAnim = stompAnimModule.getRandomStompAnim()
				self.agent.animation.Set(self.stompAnim)
				self.agent.Stomp(self.microToKill)
			end
		
		--IF GTS HAS NO TARGET
		else
			
		end

		--GTS IS NOT SEEKING A TARGET
		if self.seeking == false then

			if self.target.IsDead() then
				if self.target.IsCrushed() then
					log("X_X")
					self.missionFailed = true
					self.timer = Time.time
					return
				else
					log("Your crush disappeared...")
					self.agent.ai.StopBehavior()
					return
				end
			end

			--IF PLAYER AWAY, GO BACK TO IT
			if self.agent.DistanceTo(self.target) > self.stressDistance * self.agent.scale then
				self.agent.LookAt(self.target)
				self.agent.animation.Set(walkAnimation)
				self.agent.MoveTo(self.target)
				self.agent.animation.Set(idleAnimation)
				self.agent.Wait(1.5)

			--IF NEAR PLAYER
			else
				--CREATE micros TABLE
				if self.agent.senses.getMicrosInRadius(self.distanceToKill) then
					micros = self.agent.senses.getMicrosInRadius(self.distanceToKill)
					--log("MICROS")
				end

				--REMOVE PLAYER and LAST KILLED TARGET FROM micros TABLE
				for key,value in pairs(micros) do --actualcode
					if micros[key] == self.target or micros[key].IsDead() then
						table.remove(micros, key)
					end
				end

				--IF MICROS NEARBY, SET ONE AS TARGET
				if micros[1] ~= nil then
					self.seeking = true
					if self.firstWalk then self.timer = Time.time + 2 end
					if micros[1] ~= self.target then
						self.microToKill = micros[1]
					end

				--IF NO MICROS NEARBY
				else						
					--WAIT FOR EMBARRASSED ANIMATION TO FINISH						
					if not self.embarrassed or (Time.time > self.timer + 1 and self.agent.animation.GetProgress() >= 1 and not self.agent.animation.IsInTransition()) then

						--IF RIGHT BESIDES PLAYER, SET 'EMBARRASSED' ANIMATION
						if self.agent.DistanceTo(self.target) < self.stressDistance * self.agent.scale * 0.4 then
							self.agent.LookAt(self.target)
							local embarAnim = embarAnimations[math.random(#embarAnimations)]
							if embarAnim == "Crouch Idle" then
								self.agent.animation.transitionDuration = self.animTransSpeed * 2.5
								self.raisePrep = true
							else
								if self.raisePrep then 
									self.agent.animation.transitionDuration = self.animTransSpeed * 3
									self.raisePrep = false
								else
									self.agent.animation.transitionDuration = self.animTransSpeed
								end
							end
							self.timer = Time.time
							self.agent.animation.Set(embarAnim)
							self.embarrassed = true

						--IF NOT QUITE RIGHT BESIDES PLAYER, PLAY A NEUTRAL IDLE AFTER EMBARRASSED ANIMATION FINISHES
						elseif self.embarrassed then
							self.embarrassed = false
							local idleAnim = math.random() < 0.5
							if idleAnim then idleAnim = idleAnimation else idleAnim = "Neutral Idle" end
							self.agent.animation.Set(idleAnim)
						end
					end
				end
			end
		end
	end
end

function Yandere:Exit()
	self.agent.animation.transitionDuration = self.animTransSpeed
	log("(Yandere scenario ended)")
end


YandereStop = RegisterBehavior("Yandere Stop")
YandereStop.data = {
	menuEntry = "Scenario/Yandere [Stop]",
	agent = {
        type = { "giantess"}
    },
    target = {
        type = {"micro"}
    }
}

function YandereStop:Start()
	self.agent.ai.StopBehavior()
end

function YandereStop:Exit()
	if self.agent.dict.yandereActive then
		log(self.agent.name.." has stopped being a big Yandere...")
	else
		log(self.agent.name.." wasn't acting Yandere at all anyway.")
	end
	self.agent.dict.yandereActive = nil
end
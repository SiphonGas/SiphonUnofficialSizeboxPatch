Patrol = RegisterBehavior("Patrol Here")

Patrol.data = {
	menuEntry = "Walk/Patrol Here",
	agent = {
		type = { "humanoid" }, 
		exclude = { "player" }
	},
	target = {
		type = { "none" }
	}
}

walkAnimation = "Walk"
idleAnimation = "Idle 5"
findAnimation = "Pointing Forward"

function Patrol:Start()
	self.currentPos = 0
	self.startPos = self.agent.position
	self.patrolPos = self.cursorPoint
	self.nextPos = self.patrolPos
	self.agent.animation.Set(walkAnimation)
	self.agent.MoveTo(self.nextPos)
	self.agent.animation.Set(idleAnimation)
	self.agent.Wait(4)
end

function Patrol:Update()
	if self.agent.animation.Get() == "Idle 5" and not self.agent.animation.IsInTransition() then
		if self.currentPos == 0 then
			self.currentPos = 1
			self.nextPos = self.startPos
		else 
			self.currentPos = 0
			self.nextPos = self.patrolPos
		end
		self.agent.animation.Set(walkAnimation)
		self.agent.MoveTo(self.nextPos)
		self.agent.animation.Set(idleAnimation)
		self.agent.Wait(4)
	end
end

function Patrol:End()
	self.agent.animation.Set("Idle 4")
end
--patrol v1.1 by Debolte - Edit 2021/12/18

Patrol = RegisterBehavior("Patrol Here")
Patrol.data = {
	menuEntry = "Walk/Patrol Here",
	agent = {
		type = { "humanoid" }
	},
	target = {
		type = { "none" }
	},
	tags = "movement",
	settings = {
		{"walkAnim", "Movement Animation", "string", "Walk"},
		{"idleAnim", "Idle Animation", "string", "Idle 5"},
		{"idleDuration", "Idle Duration", "string", "4"}
	}
}

function Patrol:Start()
	self.currentPos = 0
	self.startPos = self.agent.position
	self.patrolPos = self.cursorPoint
	self.nextPos = self.patrolPos
	self.idleDuration = tonumber(self.idleDuration)
	self:GoTo()
end

function Patrol:Update()
	if self.agent.animation.Get() == self.idleAnim and not self.agent.animation.IsInTransition() then
		if self.currentPos == 0 then
			self.currentPos = 1
			self.nextPos = self.startPos
		else 
			self.currentPos = 0
			self.nextPos = self.patrolPos
		end
		self:GoTo()
	end
end

function Patrol:Exit()
	self.agent.animation.Set("Idle 4")
end

function Patrol:GoTo()
	self.agent.animation.Set(self.walkAnim)
	self.agent.MoveTo(self.nextPos)
	self.agent.animation.Set(self.idleAnim)
	self.agent.Wait(self.idleDuration)
end
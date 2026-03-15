LegacySpurts = RegisterBehavior("Grow Basic (Spurts)")
LegacySpurts.data = {
	menuEntry = "Size/Grow Basic (Spurts)",
	flags = { "grow" },
	agent = {
		type = { "humanoid" }
	},
	target = {
		type = { "oneself" }
	}
}

GROW_DURATION = 1
GROW_SPEED = 0.2
IDLE_DURATION = 5


function LegacySpurts:Start()
	self.timer = Time.time
	self.growing = true
end

function LegacySpurts:Update()
	if self.growing then
		self.agent.scale = self.agent.scale * (1 + GROW_SPEED * Time.deltaTime)

		if Time.time >= self.timer + GROW_DURATION then
			self.growing = false
			self.timer = Time.time + IDLE_DURATION
		end
	else

		if Time.time >= self.timer then
			self.growing = true
		end

	end
end
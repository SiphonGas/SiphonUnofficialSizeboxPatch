Crawl = RegisterBehavior("Crawl Here")

Crawl.data = {
    menuEntry = "Walk/Crawl Here",
    agent = {
        type = { "humanoid" },
        exclude = { "player" }
    },
    target = {
        type = { "none" }
    }
}

animationList = {"Sit 7", "Crouch Idle"} -- idle animation list
idleAnimation = "Crouch Idle"

walkAnimationList = {"Crawl",} -- crawling animations. feel free to add.

function Crawl:Start()

	local index = math.random(#walkAnimationList) -- gets a random movement animation from the animation list.
    local walkAnimation = walkAnimationList[index]
    self.agent.animation.Set(walkAnimation)

    self.agent.MoveTo(self.cursorPoint)

	local index = math.random(#animationList) -- gets a random idle animation from the animation list.
    local animation = animationList[index]
    self.agent.animation.Set(animation)

end

function Crawl:Exit()
    self.agent.animation.Set(idleAnimation)
end

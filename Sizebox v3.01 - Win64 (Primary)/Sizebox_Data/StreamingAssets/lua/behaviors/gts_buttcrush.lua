Buttcrush = RegisterBehavior("Buttcrush")
Buttcrush.scores = {
    normal = 15
}
Buttcrush.data = {
    menuEntry = "Interaction/Buttcrush",
    ai = true,
    agent = { type = {"giantess"} },
    target = { type = {"micro", "player"} },
    tags = "macro, interaction, evil",
    settings = {
        {"tauntAfter", "Taunt After Crush", "bool", true},
        {"groundPound", "Ground Pound (jump)", "bool", false}
    }
}

SIT_ANIM = "Sit 6"
JUMP_ANIM = "Jump 4"
IDLE_ANIM = "Idle 2"
WALK_ANIM = "Walk"
TAUNT_ANIMS = {"Laughing", "Insult", "Loser", "Taunt 3", "Happy", "Fist Pump", "Whatever Gesture"}

WALKING, WAITING, SITTING, TAUNTING = 0, 1, 2, 3

function Buttcrush:Start()
    if not self.target then
        self.agent.ai.StopBehavior()
        return
    end
    self.state = WALKING
    self.agent.animation.Set(WALK_ANIM)
    self.agent.MoveTo(self.target)
end

function Buttcrush:Update()
    if not self.target then
        self.agent.ai.StopBehavior()
        return
    end

    if self.state == WALKING then
        -- Wait until GTS arrives near target
        if not self.agent.ai.IsActionActive() then
            -- Arrived. Look at target then sit.
            self.agent.lookAt(self.target)
            self.state = WAITING
            self.waitStart = Time.time
            self.agent.animation.Set(IDLE_ANIM)
        end

    elseif self.state == WAITING then
        -- Brief pause before sitting (lets her face the target)
        if (Time.time - self.waitStart) > 0.5 then
            self.state = SITTING
            self.sitStart = Time.time

            if self.groundPound then
                self.agent.animation.Set(JUMP_ANIM)
            else
                self.agent.animation.Set(SIT_ANIM)
            end
        end

    elseif self.state == SITTING then
        -- Wait for animation to reach impact, then crush
        local delay = self.groundPound and 0.8 or 1.2
        if (Time.time - self.sitStart) > delay then
            self.agent.Stomp(self.target)

            if self.tauntAfter then
                self.state = TAUNTING
                self.tauntStart = Time.time
                local anim = TAUNT_ANIMS[math.random(1, #TAUNT_ANIMS)]
                self.agent.animation.Set(anim)
            else
                self.agent.ai.StopBehavior()
            end
        end

    elseif self.state == TAUNTING then
        if (Time.time - self.tauntStart) > 3 then
            self.agent.ai.StopBehavior()
        end
    end
end

function Buttcrush:Exit()
    self.agent.animation.Set(IDLE_ANIM)
    self.agent.lookAt(nil)
end

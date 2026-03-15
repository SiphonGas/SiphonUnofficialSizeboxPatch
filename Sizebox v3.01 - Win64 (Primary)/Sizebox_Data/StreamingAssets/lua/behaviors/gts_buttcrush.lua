Buttcrush = RegisterBehavior("Buttcrush")
Buttcrush.data = {
    agent = { type = {"giantess"} },
    target = { type = {"micro"} },
    menuEntry = "Interaction/Buttcrush",
    ai = true,
    tags = "giantess, interaction"
}

function Buttcrush:Start()
    self:_MoveToSeatPoint()
    self.lastTargetPos = self.target.position
    self.pauseDelay = 0
end

function Buttcrush:Update()
    if not self.agent.ai.IsActionActive() then
        if self.pauseDelay == 0 then
            self.agent.animation.Set("Sit 6")
            self.pauseDelay = self.agent.animation.GetLength() + 1
        else
            self.pauseDelay = self.pauseDelay - Time.deltaTime
            if self.pauseDelay <= 0 then
                self.agent.ai.StopBehavior()
                return
            end
        end
        return
    end

    -- re-path only if target moved noticeably
    if Vector3.Distance(self.lastTargetPos, self.target.position) > (self.target.scale * 0.25) then
        self.agent.ai.StopAction()
        self:_MoveToSeatPoint()
        self.lastTargetPos = self.target.position
    end
end

function Buttcrush:Exit()
    self.agent.animation.Set("Idle")
end

-- === New helpers ===

-- Distance to sit behind the target along the agent→target line
function Buttcrush:_DesiredBackoff()
    -- tweakable cushion: half a target, quarter an agent
    return (self.target.scale * 0.5) + (self.agent.scale * 0.25)
end

-- Tiny lateral jitter so we don't aim exactly center (optional)
function Buttcrush:_SideJitter()
    return 0 -- or: (math.random() - 0.5) * 0.15 * (self.target.scale + self.agent.scale)
end

function Buttcrush:_SeatPoint()
    local toTarget = (self.target.position - self.agent.position).normalized
    local right = Vector3.Cross(toTarget, Vector3.up).normalized
    local p = self.target.position
              - toTarget * self:_DesiredBackoff()
              + right * self:_SideJitter()

    -- keep on ground plane near agent
    p.y = self.agent.position.y
    return p
end

function Buttcrush:_MoveToSeatPoint()
    local dst = self:_SeatPoint()
    self.agent.animation.Set("Walk")
    self.agent.MoveTo(dst)
end

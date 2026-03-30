Stuff = RegisterBehavior("Stuff")
Stuff.data = {
    menuEntry = "Interaction/Stuff In Panties",
    agent = { type = {"giantess"} },
    target = { type = {"micro", "player"} },
    tags = "macro, interaction",
    settings = {
        {"offsetBack", "Back Offset", "float", 0.03},
        {"offsetDown", "Down Offset", "float", 0.02},
        {"wiggle", "Wiggle", "bool", true}
    }
}

WALK_ANIM = "Walk"
CROUCH_ANIM = "Crouch Idle"
GRAB_ANIM = "Acknowledging"
IDLE_ANIM = "Idle 2"
TAUNT_ANIMS = {"Laughing", "Happy", "Excited", "Insult", "Taunt 3", "Whatever Gesture"}
WALK_ANIMS = {"Walk", "Female Walk", "Feminine Walking"}

APPROACHING, CROUCHING, GRABBING, STUFFING, CARRYING, RELEASING = 0, 1, 2, 3, 4, 5

function Stuff:Start()
    if not self.target then
        self.agent.ai.StopBehavior()
        return
    end
    self.state = APPROACHING
    self.agent.animation.Set(WALK_ANIM)
    self.agent.MoveTo(self.target)
    self.agent.LookAt(self.target)
    self.wiggleTime = 0
    self.wiggleOffset = 0
end

function Stuff:Update()
    if not self.target then
        self.agent.ai.StopBehavior()
        return
    end

    if self.state == APPROACHING then
        if not self.agent.ai.IsActionActive() then
            -- Arrived. Crouch down to grab
            self.state = CROUCHING
            self.stateStart = Time.time
            self.agent.animation.Set(CROUCH_ANIM)
            self.agent.LookAt(self.target)
        end

    elseif self.state == CROUCHING then
        if (Time.time - self.stateStart) > 1.5 then
            -- Reach for the micro
            self.state = GRABBING
            self.stateStart = Time.time
            self.agent.Grab(self.target)
            self.agent.animation.Set(GRAB_ANIM)
        end

    elseif self.state == GRABBING then
        if (Time.time - self.stateStart) > 2.0 then
            -- Now stuff them
            self.state = STUFFING
            self.stateStart = Time.time
            -- Play a taunt
            local anim = TAUNT_ANIMS[math.random(1, #TAUNT_ANIMS)]
            self.agent.animation.Set(anim)
            Game.Toast.New().Print("*tucks you in*")
        end

    elseif self.state == STUFFING then
        -- Move target to hips area
        self:PositionAtHips()

        if (Time.time - self.stateStart) > 2.0 then
            self.state = CARRYING
            self.stateStart = Time.time
            -- Start walking around
            local walkAnim = WALK_ANIMS[math.random(1, #WALK_ANIMS)]
            self.agent.animation.Set(walkAnim)
            self.agent.Wander()
            Game.Toast.New().Print("*walks around casually*")
        end

    elseif self.state == CARRYING then
        -- Keep target at hips while walking
        self:PositionAtHips()

        -- Periodically change direction or taunt
        if not self.agent.ai.IsActionActive() then
            local roll = math.random(1, 10)
            if roll <= 3 then
                -- Pause and taunt
                local anim = TAUNT_ANIMS[math.random(1, #TAUNT_ANIMS)]
                self.agent.animation.Set(anim)
                self.pauseUntil = Time.time + 3
            elseif roll <= 5 then
                -- Wiggle/dance
                self.agent.animation.Set("Air Squat")
                self.pauseUntil = Time.time + 2
                Game.Toast.New().Print("*squirms*")
            else
                -- Keep walking
                local walkAnim = WALK_ANIMS[math.random(1, #WALK_ANIMS)]
                self.agent.animation.Set(walkAnim)
                self.agent.Wander()
            end
        end

        -- Check for pause
        if self.pauseUntil and Time.time > self.pauseUntil then
            self.pauseUntil = nil
            local walkAnim = WALK_ANIMS[math.random(1, #WALK_ANIMS)]
            self.agent.animation.Set(walkAnim)
            self.agent.Wander()
        end
    end
end

function Stuff:PositionAtHips()
    if not self.target or not self.agent.bones.hips then return end

    local hips = self.agent.bones.hips
    local scale = self.agent.scale

    -- Position behind and slightly below hips
    local backOffset = self.offsetBack * scale
    local downOffset = self.offsetDown * scale

    -- Get the GTS facing direction to place micro behind her
    local forward = self.agent.transform.forward
    local pos = hips.position - forward * backOffset
    pos.y = pos.y - downOffset

    -- Add subtle wiggle
    if self.wiggle then
        self.wiggleTime = self.wiggleTime + Time.deltaTime * 3
        self.wiggleOffset = math.sin(self.wiggleTime) * 0.005 * scale
        pos.y = pos.y + self.wiggleOffset
    end

    self.target.position = pos
end

function Stuff:Exit()
    self.agent.animation.Set(IDLE_ANIM)
    self.agent.LookAt(nil)
    -- Release: place target on ground near GTS
    if self.target then
        local pos = self.agent.position
        pos.y = self.agent.transform.position.y
        self.target.position = pos
    end
end

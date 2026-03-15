Shrink = RegisterBehavior("Shrink")
Shrink.data = {
    menuEntry = "Size/Shrink",
    secondary = true,
    agent = {
        type = { "humanoid" }
    },
    target = {
        type = { "oneself" }
    }
}

function Shrink:Start() 
    self.agent.Grow(-0.1)
end
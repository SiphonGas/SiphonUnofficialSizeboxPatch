return {
    {
        START = function(data,entity)
            entity.animation.Set("Jump Up")
            data.teleported=false
            return data
        end,
        UPDATE = function(data,entity)
            if entity.animation.GetProgress() < 0.6 and entity.animation.GetProgress() > 0.5 and not data.teleported then
                entity.position = entity.position + Vector3.New(math.random(-100,100),500,math.random(-100,100))
                data.teleported=true
            end
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return entity.animation.IsCompleted()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 50
    },
    {
        START = function(data,entity)
            entity.animation.Set("Walk")
            entity.Wander(math.random(3,7))
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return not entity.ai.IsActionActive()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 50
    },
    {
        START = function(data,entity)
            entity.animation.Set("Angry")
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return entity.animation.IsCompleted()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 40,
        bypassWait = true
    },
    {
        START = function(data,entity)
            entity.animation.Set("Wait Strech Arms")
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return entity.animation.IsCompleted()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 40,
        bypassWait = true
    },
    {
        START = function(data,entity)
            entity.animation.Set("Lifting")
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return entity.animation.IsCompleted()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 40,
        bypassWait = true
    },
    {
        START = function(data,entity)
            entity.animation.Set("Excited")
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return entity.animation.IsCompleted()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 40,
        bypassWait = true
    },
    {
        START = function(data,entity)
            entity.animation.Set("Roar")
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return entity.animation.IsCompleted()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 40,
        bypassWait = true
    },
    {
        START = function(data,entity)
            entity.animation.Set("Talking on Phone")
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return entity.animation.IsCompleted()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 40,
        bypassWait = true
    }
}
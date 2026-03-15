return {
    POI = {
        "Randomness"
    },
    {
        START = function(data,entity)
            entity.animation.Set("Masturbation 1")
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return entity.animation.IsCompleted()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 80,
        bypassWait = true
    },
    {
        START = function(data,entity)
            entity.animation.Set("Massage Breasts 5")
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return entity.animation.IsCompleted()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 80,
        bypassWait = true
    }
}
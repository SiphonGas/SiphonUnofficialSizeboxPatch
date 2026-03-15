stompAnimModule = require "stomp_anim"
return {
    POI = {
        "Attacking Tinies",
        "Feminine Walking",
        "Attacking Buildings"
    },
    {
        REQUIRED=function(data)
            return data.entity.FindRandomBuilding(data.entity) ~= nil
        end, 
        START = function(data,entity)
            data.target = entity.FindRandomBuilding(entity)
            entity.animation.Set("Female Walk")
            entity.MoveTo(data.target)
            data.state = 0
            return data
        end,
        UPDATE = function(data,entity)
            if data.state == 0 and not entity.ai.IsActionActive() then
                entity.animation.Set(stompAnimModule.getRandomStompAnim())
                entity.Stomp(data.target)
                data.state=1
            end
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return not entity.ai.IsActionActive() and data.state == 1
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 50
    }
}
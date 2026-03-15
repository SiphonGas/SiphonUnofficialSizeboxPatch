return {
    POI = {
        "Proximity",
        "Feminine Walking",
    },
    {
        REQUIRED=function(data)
            return Game.GetLocalPlayer() ~= nil
        end, 
        START = function(data,entity)
            data.target = Game.GetLocalPlayer()
            entity.animation.Set("Female Walk")
            entity.MoveTo(data.target)
            return data
        end,
        UPDATE = function(data,entity)
            if Vector3.Distance(entity.position,data.target.position) < (entity.scale + data.target.scale) * 2 then
                entity.ai.StopAction()
                entity.animation.Set("Idle")
            end
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return not data.target or not entity.ai.IsActionActive()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 90
    },
    {
        REQUIRED=function(data)
            return Game.GetLocalPlayer() ~= nil
        end, 
        START = function(data,entity)
            data.duration = math.random(10,20)
            data.target = Game.GetLocalPlayer()
            entity.animation.Set("Female Walk")
            entity.Wander()
            return data
        end,
        UPDATE = function(data,entity)
            data.duration = data.duration - Time.deltaTime
            data.target.position = entity.bones.hips.position
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return not data.target or data.duration <= 0
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
            if data.target then
                data.target.position = entity.position
            end
        end,
        Weight = 25
    }
}
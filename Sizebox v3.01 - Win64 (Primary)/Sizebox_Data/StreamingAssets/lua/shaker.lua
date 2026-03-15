return {
    POI = {
        "Squats",
        "Fist Shaking",
        "Dancing",
        "Sitting",
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
            if data.state == 0 and Vector3.Distance(entity.position,data.target) <= entity.scale * 3 then
                entity.ai.StopAction()
                entity.animation.Set("Air Squat Bent Arms")
                data.duration = math.random(3,6)
                data.state=1
            elseif data.state == 1 then
                if data.duration <= 0 then
                    data.state = 2
                else
                    data.duration = data.duration - Time.deltaTime
                end
            end
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return data.state == 2
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 50
    },
    {
        START = function(data,entity)
            data.duration = math.random(10,20)
            entity.animation.Set("Shake Fist")
            return data
        end,
        UPDATE = function(data,entity)
            data.duration = data.duration - Time.deltaTime
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return data.duration <= 0
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 80,
        bypassWait = true
    },
    {
        START = function(data,entity)
            data.duration = math.random(10,20)
            entity.animation.Set("Hip Hop Dancing 3")
            return data
        end,
        UPDATE = function(data,entity)
            data.duration = data.duration - Time.deltaTime
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return data.duration <= 0
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 80,
        bypassWait = true
    },
    {
        START = function(data,entity)
            data.duration = math.random(10,20)
            entity.animation.Set("Pike Walk")
            return data
        end,
        UPDATE = function(data,entity)
            data.duration = data.duration - Time.deltaTime
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return data.duration <= 0
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 80,
        bypassWait = true
    },
    {
        START = function(data,entity)
            data.duration = math.random(10,20)
            entity.animation.Set("Sit 7")
            return data
        end,
        UPDATE = function(data,entity)
            data.duration = data.duration - Time.deltaTime
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return data.duration <= 0
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 80,
        bypassWait = true
    }
}
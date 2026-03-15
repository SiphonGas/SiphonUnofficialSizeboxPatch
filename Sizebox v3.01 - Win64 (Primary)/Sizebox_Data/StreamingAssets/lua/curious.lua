EVENTS = {}

ENTITIES = {}

function EVENTS:OnSpawn(data)
    table.insert(ENTITIES,data.entity)
end

Event.Register(self,EventCode.OnSpawn,EVENTS.OnSpawn)

return {
    POI = {
        "Proximity",
        "Feminine Walking",
        "Attacking Tinies",
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
                entity.animation.Set("Crouch Idle")
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
        REQUIRED=function(data)
            for _,v in ipairs(ENTITIES) do
                if v ~= nil and (v.name == "Muscle Car" or v.name == "Sleek Car") then
                    return v.rigidbody.velocity.magnitude > 10
                end
            end
            return false
        end, 
        START = function(data,entity)
            data.target = nil
            for _,v in ipairs(ENTITIES) do
                if v ~= nil and (v.name == "Muscle Car" or v.name == "Sleek Car") then
                    data.target = v
                end
            end
            entity.animation.Set("Slow Run")
            entity.MoveTo(data.target)
            entity.LookAt(data.target)
            data.state = 0
            data.interest = 10
            return data
        end,
        UPDATE = function(data,entity)
            if not data.target then return data end
            if not entity.ai.IsActionActive() and data.state == 0 then
                entity.animation.Set("Slow Run")
                entity.MoveTo(data.target)
                entity.LookAt(data.target)
                data.interest = 10
                data.state=1
            elseif data.state == 1 then
                if Vector3.Distance(entity.position,data.target.position) < entity.scale * 3 and entity.animation.Get() ~= "Crouch Idle" then
                    entity.ai.StopAction()
                    entity.animation.Set("Crouch Idle")
                    data.interest = data.interest - Time.deltaTime
                    entity.LookAt(data.target)
                elseif Vector3.Distance(entity.position,data.target.position) > entity.scale * 4 then
                    data.state = 0
                else
                    data.interest = data.interest - Time.deltaTime
                end
            end
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return not data.target or data.interest <= 0
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
        end,
        Weight = 75
    }
}
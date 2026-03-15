EVENTS = {}

ENTITIES = {}

function EVENTS:OnSpawn(data)
    table.insert(ENTITIES,data.entity)
end

Event.Register(self,EventCode.OnSpawn,EVENTS.OnSpawn)

return {
    POI = {
        "Attacking Tinies",
    },
    {
        REQUIRED=function(data)
            return #ENTITIES > 0
        end, 
        START = function(data,entity)
            data.target = ENTITIES[math.random(1,#ENTITIES)]
            entity.ai.SetBehavior("GtsEat",data.target)
            return data
        end,
        ENDREQUIRE = function(data,entity)
            return data.target == nil or not entity.ai.IsBehaviorActive()
        end,
        END = function(data,entity)
            entity.animation.Set("Idle")
            entity.ai.StopAction()
        end,
        Weight = 50
    }
}
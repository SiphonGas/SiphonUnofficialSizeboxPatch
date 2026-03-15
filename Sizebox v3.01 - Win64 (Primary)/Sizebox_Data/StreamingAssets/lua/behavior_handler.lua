local handler_functions = {}

function handler_functions:Start(behaviors,giant)
    handler_functions.behaviors = behaviors
    handler_functions.entity=giant
    handler_functions.actionCooldown = math.random(3,6)
    handler_functions.targetbehaviors = nil
    handler_functions.lastBehavior = nil
    handler_functions.data = {}
end
function weighted_random(choices)
    -- Calculate the total weight (sum of all probabilities)
    local total_weight = 0
    for _, choice in ipairs(choices) do
        total_weight = total_weight + choice.Weight
    end

    -- Pick a random number between 0 and total_weight
    local random_value = math.random() * total_weight

    -- Find the item based on the random number
    local cumulative_weight = 0
    for _, choice in ipairs(choices) do
        cumulative_weight = cumulative_weight + choice.Weight
        if random_value <= cumulative_weight then
            return choice
        end
    end
end
function allowedbehaviors(data)
    local tbl = {}
    for _,v in ipairs(handler_functions.behaviors) do
        if (v.REQUIRED == nil or (v.REQUIRED ~= nil and v.REQUIRED(data))) and v ~= handler_functions.lastBehavior then
            table.insert(tbl,v)
        end
    end
    return tbl
end
function handler_functions:Operate()
    if handler_functions.actionCooldown > 0 then
        handler_functions.actionCooldown = handler_functions.actionCooldown - Time.deltaTime
    end
    if not handler_functions.targetbehaviors then
        if handler_functions.actionCooldown <= 0 then
            local pos = allowedbehaviors(handler_functions)
            handler_functions.targetbehaviors = weighted_random(pos)
            if handler_functions.targetbehaviors ~= nil then
                handler_functions.data = {}
                if handler_functions.targetbehaviors.START ~= nil then
                    handler_functions.data = handler_functions.targetbehaviors.START(handler_functions.data,handler_functions.entity)
                end
            end
        end
    else
        if handler_functions.targetbehaviors.ENDREQUIRE ~= nil and handler_functions.targetbehaviors.ENDREQUIRE(handler_functions.data,handler_functions.entity) then
            if handler_functions.targetbehaviors.END ~= nil then
                handler_functions.targetbehaviors.END(handler_functions.data,handler_functions.entity)
            end
            handler_functions.lastBehavior = handler_functions.targetbehaviors
            if not handler_functions.targetbehaviors.bypassWait then
                handler_functions.actionCooldown = math.random(1,3)
            else
                handler_functions.actionCooldown = 0.5
            end
            handler_functions.targetbehaviors = nil
        elseif handler_functions.targetbehaviors.UPDATE ~= nil then
            handler_functions.data = handler_functions.targetbehaviors.UPDATE(handler_functions.data,handler_functions.entity)
        end
    end
end
function handler_functions:End()
	if handler_functions.targetbehaviors ~= nil then
		if handler_functions.targetbehaviors.END ~= nil then
			handler_functions.targetbehaviors.END(handler_functions.data,handler_functions.entity)
		end
	end
    handler_functions.entity=nil
    handler_functions.actionCooldown = nil
    handler_functions.targetbehaviors = nil
    handler_functions.data =nil
    handler_functions.lastBehavior = nil
end

return handler_functions
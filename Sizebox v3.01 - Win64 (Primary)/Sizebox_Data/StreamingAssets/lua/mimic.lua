local allTables = {
    require "megalophobia_personalities/aggressive",
    require "megalophobia_personalities/curious",
    require "megalophobia_personalities/scalar",
    require "megalophobia_personalities/shaker"
}

local c =  {
    POI = {
        "Feminine Walking",
        "Proximity",
        "Attacking Tinies",
        "Squats",
        "Fist Shaking",
        "Dancing",
        "Sitting",
        "Attacking Buildings"
    }
}

for _, value in ipairs(allTables) do
    for _,v in ipairs(value) do
        table.insert(c,v)
    end
end

return c
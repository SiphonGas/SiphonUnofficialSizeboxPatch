function Update()
	for k,v in pairs(gts.list) do
		if v~=nil and v.position.y <= -500 then
			v.position=Vector3.New(v.position.x,500,v.position.z)
		end
	end
	for k,v in pairs(micros.list) do
		if v~=nil and v.position.y <= -500 then
			v.position=Vector3.New(v.position.x,500,v.position.z)
		end
	end
end
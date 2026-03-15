using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityBuilder : EntityBase {
	public enum TileType { NoBuild, CanBuild, Intersection, Road, Occupied }
	Tile[,] tiles;
	float[,] perlinValue;
	GameObject[] buildings0;
	GameObject[] buildings1;
	GameObject[] buildings2;
	// Street Prefabs
	GameObject intersectionPrefab;
	GameObject straightPrefab;
	GameObject endPrefab;
	GameObject curvePrefab;
	GameObject junctionPrefab;

	public float buildingDensity = 0.4f;
	public float tileSize = 15;
	public int radius = 120;
	int diameter;
	public int streetModule = 5;
	GameObject buildingRoot;
	GameObject roadRoot;
	public float superiorLimit = 300f;
	public float inferiorLimit = 100f;
	public float maxSlope = 30f;
	public float perlinThreshold = 0.5f;
	public float mediumThreshold = 0.6f;
	public float tallThreshold = 0.8f;
	public float scaleNoise = 1f;
	public float buildScaleModification = 0.3f;

	enum Direction {Horizontal, Vertical};
	float cityScale = 1f;
	float streetOffset = 0.05f;


	// sound settings
	AudioSource audioSource;
	public float minSoundDistance;
	public float maxSoundDistance;
	Transform placeHolder;
	Camera mainCamera;
	bool isPlaced = false;
	Collider parentCollider;

	public class Tile {
		public int x;
		public int y;
		// unsigned x and y (for array search)
		public int ux;
		public int uy;
		public TileType type = TileType.NoBuild;
		public Vector3 localPosition;
		public Vector3[] points;

		public Tile(int x, int y) {
			this.x = x;
			this.y = y;
		}
	}

	protected override void Awake() {
		base.Awake();
		rotationEnabled = false;
	}

	// Use this for initialization
	void Start () {
		

		mainCamera = Camera.main;

		placeHolder = myTransform.GetChild(0);
		placeHolder.localScale = new Vector3(radius * tileSize * 1.4f , inferiorLimit / 1.5f, radius * tileSize * 1.4f);




		buildings0 = Resources.LoadAll<GameObject>("City/Building/Zone0");
		buildings1 = Resources.LoadAll<GameObject>("City/Building/Zone1");
		buildings2 = Resources.LoadAll<GameObject>("City/Building/Zone2");

		foreach(GameObject building in buildings0) {
			InitializeBuilding(building);
		}
		foreach(GameObject building in buildings1) {
			InitializeBuilding(building);
		}
		foreach(GameObject building in buildings2) {
			InitializeBuilding(building);
		}

		// 4 connections
		intersectionPrefab = Resources.Load<GameObject>("City/Road/Intersection");
		// 3 connections
		junctionPrefab = Resources.Load<GameObject>("City/Road/Junction");
		// 2 connections
		straightPrefab = Resources.Load<GameObject>("City/Road/Straight");
		curvePrefab = Resources.Load<GameObject>("City/Road/Curve");
		// 1 connection
		endPrefab = Resources.Load<GameObject>("City/Road/End");

		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.dopplerLevel = 0f;
		audioSource.spatialBlend = 0f;
		minSoundDistance = radius * tileSize;
		maxSoundDistance = radius * tileSize * 2;

		ResetCity();
		// BuildCity();
	}

	void ResetCity() {
		if(roadRoot != null) GameObject.Destroy(roadRoot);
		if(buildingRoot != null) GameObject.Destroy(buildingRoot);

		buildingRoot = new GameObject("Buildings");
		buildingRoot.transform.SetParent(transform, false);

		roadRoot = new GameObject("Roads");
		roadRoot.transform.SetParent(transform, false);

		placeHolder.gameObject.SetActive(true);
		isPlaced = false;
	}

	public override void Move(Vector3 position) {
		if(isPlaced) ResetCity();
		base.Move(position);
	}

	public override void OnPlaced() {
		placeHolder.gameObject.SetActive(false);
		BuildCity();
	}

	public override void ChangeRotation(Vector3 newRotation) {
		// do nothing		
	}

	public override void ChangeOffset(float newOffset)
	{
		// do nothing
	}

	public override void ChangeScale(float newScale)
	{
		if(!isPlaced) base.ChangeScale(newScale);
	}


	void BuildCity() {

		cityScale = transform.lossyScale.y;
		if(transform.parent != null) {
			parentCollider = transform.parent.GetComponent<Collider>();
		} else {
			parentCollider = null;
		}
		

		diameter = radius * 2 + 1;

		perlinValue = SamplePerlinNoise(diameter); 
		tiles = new Tile[diameter, diameter];
		
		DetermineBuildableArea();
		CreateStreets();
		CreateBuildings();
		
		isPlaced = true;
	}


	void DetermineBuildableArea() {
		for(int x = -radius; x < radius; x++) {
			for(int y = -radius; y < radius; y++) {
				int coordX = x + radius;
				int coordY = y + radius;

				Tile tile = new Tile(x,y);
				tile.ux = coordX;
				tile.uy = coordY;

				if(perlinValue[coordX, coordY] > perlinThreshold) tile.type = TileType.CanBuild;
				else tile.type = TileType.NoBuild;

				tiles[coordX, coordY] = tile;
			}
		}
	}

	void CreateStreets() {
		List<Tile> intersections = new List<Tile>();
		for(int x = -radius; x < radius; x++) {
			for(int y = -radius; y < radius; y++) {
				int coordX = x + radius;
				int coordY = y + radius;
				Tile tile = tiles[coordX, coordY];
				if(tile.type != TileType.CanBuild) continue;

				if(x % streetModule == 0 || y % streetModule == 0) {
					if(x % streetModule == 0 && y % streetModule == 0) {
						
						if(PlanIntersection(tile)) {
							CheckCloseIntersection(coordX, coordY);
							intersections.Add(tile);
						}
					}
				}
			}
		}

		foreach(Tile intersection in intersections) {
			SpawnIntersection(intersection);
		}

		StaticBatchingUtility.Combine(roadRoot);

	}

	void CreateBuildings() {
		for(int x = -radius; x < radius; x++) {
			for(int y = -radius; y < radius; y++) {
				int coordX = x + radius;
				int coordY = y + radius;
				if(tiles[coordX, coordY].type == TileType.CanBuild) {	
					float buildingValue = perlinValue[coordX, coordY]; // - perlinThreshold;
					InstantiateBuilding(x,y, buildingValue);
				} 				
			}
		}
	}

	void Update() {
		float distance = (mainCamera.transform.position - transform.position).magnitude;

		if(distance < maxSoundDistance && !audioSource.isPlaying) {
			audioSource.clip = SoundManager.This.GetCitySound();
			audioSource.Play();
		}
		// Calculate Distance to the Player
		
		if(distance < minSoundDistance) {
			audioSource.volume = SoundManager.ambientVolume;
		} else if (distance < maxSoundDistance) {
			audioSource.volume = SoundManager.ambientVolume * (1 - (distance - minSoundDistance) / (maxSoundDistance - minSoundDistance));
			
		} else {
			audioSource.volume = 0f;
			audioSource.Stop();
		}
		SoundManager.This.ambientLevel = 1 - audioSource.volume / SoundManager.ambientVolume;
	}


	void InitializeBuilding(GameObject building)
	{
		building.gameObject.layer = Layers.buildingLayer;
		CityBuilding buildingComponent = building.GetComponent<CityBuilding>();
		if(buildingComponent == null) {
			buildingComponent = building.AddComponent<CityBuilding>();
		}
	}

	public int plantedBuildings = 0;
	GameObject InstantiateBuilding(int x, int y, float buildingValue) {
		if(Random.value > buildingDensity) return null;

		int coordX = x + radius;
		int coordY = y + radius;

		int rotation = Random.Range(0,4);
		bool invertXY = rotation == 1 || rotation == 3;

		// choose the building type
		Vector3 scaleModification = Vector3.zero;
		GameObject buildingPrefab = null;
		if(buildingValue > tallThreshold) {
			int i = Random.Range(0,buildings2.Length);
			buildingPrefab = buildings2[i];
			scaleModification = Vector3.up * buildScaleModification * (Random.value * 2 - 1f);
		} else if (buildingValue > mediumThreshold) {
			int i = Random.Range(0,buildings1.Length);
			buildingPrefab = buildings1[i];
			scaleModification = Vector3.up * buildScaleModification * (Random.value * 2 - 1f);
		} else {
			int i = Random.Range(0,buildings0.Length);
			buildingPrefab = buildings0[i];			
		}

		CityBuilding buildingData = buildingPrefab.GetComponent<CityBuilding>();
		int xSize = buildingData.xSize;
		int zSize = buildingData.zSize;
		if(invertXY) {
			xSize = zSize;
			zSize = buildingData.xSize;
		}

		if(coordX + xSize > diameter || coordY + zSize > diameter) return null;
		// if 1 tile is occupied, return
		for(int i = coordX; i < coordX + xSize; i++) {
			for(int j = coordY; j < coordY + zSize; j++) {
				if(tiles[i,j].type != TileType.CanBuild) return null;
			}
		}

		// choose my tiles
		Vector3[] corners = CalculateCorners(tiles[coordX, coordY], xSize, zSize);
		
		RaycastHit hit;
		Vector3 placementPosition = corners[4];
		bool placementFound = false;

		for(int i = 0; i < corners.Length; i++) {
			Vector3 raycastPoint = transform.TransformPoint(corners[i] + Vector3.up * superiorLimit);
			if(Physics.Raycast(raycastPoint, -Vector3.up, out hit, (superiorLimit + inferiorLimit) * cityScale)) {
				Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);
				if(!placementFound || localHitPoint.y < placementPosition.y) {
					placementPosition.y =  localHitPoint.y;
					float angle = Mathf.Abs(Vector3.Angle(Vector3.up, hit.normal));
					if(angle > maxSlope) return null;
					placementFound = true;
				}
			}
		}

		if(!placementFound) return null;

		// the building has passed my tests and is ready to be placed on road

		// mark tiles as occupied
		
		for(int i = coordX; i < coordX + xSize; i++) {
			for(int j = coordY; j < coordY + zSize; j++) {
				tiles[i,j].type = TileType.Occupied;
			}
		}


			
		GameObject newBuilding = Instantiate(buildingPrefab);		
		
		newBuilding.transform.SetParent(buildingRoot.transform, false);
		newBuilding.transform.localPosition = placementPosition;
		newBuilding.transform.localRotation = Quaternion.AngleAxis(90f * rotation, Vector3.up);
		newBuilding.transform.localScale += scaleModification;
		// newBuilding.transform.localScale *= 0.46f;
		if(parentCollider != null) {
			newBuilding.GetComponent<CityBuilding>().IgnoreCollision(parentCollider);
		}


		return newBuilding;

		
	}

	Vector3[] CalculateCorners(Tile tile, int xSize = 1, int zSize = 1) 
	{
		Vector3[] corners = new Vector3[5];
		corners[0] = new Vector3(tile.x * tileSize - tileSize / 2f, 0, tile.y * tileSize - tileSize / 2f);
		corners[1] = corners[0] + new Vector3(0,0, tileSize * zSize);
		corners[2] = corners[0] + new Vector3(tileSize * xSize, 0, 0);
		corners[3] = corners[0] + new Vector3(tileSize * xSize, 0, tileSize * zSize);
		corners[4] = (corners[0] + corners[3]) / 2f;
		return corners;
	}

	

	bool PlanIntersection(Tile tile) {
		Vector3[] corners = CalculateCorners(tile);

		bool placementFound = false;
		Vector3 placementPosition = corners[4];

		RaycastHit hit;
		Vector3 hitNormal = Vector3.up;
		for(int i = 0; i < corners.Length; i++) {
			Vector3 raycastOrigin = transform.TransformPoint(corners[i] + Vector3.up * superiorLimit);
			if(Physics.Raycast(raycastOrigin, -Vector3.up, out hit, (superiorLimit + inferiorLimit) * cityScale)) {
				Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);
				if(!placementFound || localHitPoint.y > placementPosition.y) {
					placementPosition.y = localHitPoint.y;
					hitNormal = hit.normal;
					placementFound = true;

					// if angle is too step, don't build here
					float angle = Mathf.Abs(Vector3.Angle(Vector3.up, hitNormal));
					if(angle > maxSlope) return false;

					
				}
			}
		}

		if(placementFound) {
			tile.localPosition = placementPosition;
			tile.localPosition.y += streetOffset;
			tile.type = TileType.Intersection;
			tile.points = corners;
		}		
		return placementFound;
	}

	void SpawnIntersection(Tile tile) {
		// check how many neighboors i have
		bool[] nb = new bool[4];
		int neighboorCount = 0;

		int x = tile.ux;
		int y = tile.uy;
		// north
		nb[0] = (y + 1 < diameter && tiles[x, y + 1].type == TileType.Road);
		// south
		nb[1] = (y - 1 >= 0 && tiles[x, y - 1].type == TileType.Road);
		// east
		nb[2] = (x - 1 >= 0 && tiles[x - 1, y].type == TileType.Road);
		// west
		nb[3] = (x + 1 < diameter && tiles[x + 1, y].type == TileType.Road);

		for(int i = 0; i < 4; i++) {
			if(nb[i]) neighboorCount++;
		}

		GameObject intersection = null;
		float angle = 0;
		switch(neighboorCount) {
			case 1: 
				intersection = Instantiate(endPrefab);
				if(nb[1]) angle = 180;
				if(nb[2]) angle = -90;
				if(nb[3]) angle = 90;
				break;

			case 2:
				if(nb[0] & nb[1] || nb[2] && nb[3]) {
					intersection = Instantiate(straightPrefab);
					if(nb[2] && nb[3]) angle = 90;
				} else {
					intersection = Instantiate(curvePrefab);
					if(nb[0] && nb[2]) angle = -90;
					if(nb[1] && nb[2]) angle = 180;
					if(nb[1] && nb[3]) angle = 90;
				}
				break;

			case 3:
				intersection = Instantiate(junctionPrefab);
				if(!nb[0]) angle = 90;
				if(!nb[1]) angle = -90;
				if(!nb[3]) angle = 180;
				break;

			case 4: 
				intersection = Instantiate(intersectionPrefab);
				break;
		}

		if(intersection == null) return;

		intersection.transform.SetParent(roadRoot.transform, false);
		intersection.transform.localPosition = tile.localPosition;
		if(angle != 0) {
			intersection.transform.localRotation = Quaternion.Euler(0, angle, 0);
		}
	}

	GameObject CheckCloseIntersection(int x, int y) {
		int mod = streetModule;
		Tile thisCross = tiles[x,y];
		if(x-mod > 0)
		{
			Tile nextCross = tiles[x-mod, y];
			if(nextCross.type == TileType.Intersection) {

				Vector3 startPoint = thisCross.localPosition - Vector3.right * tileSize / 2;
				Vector3 endPoint = nextCross.localPosition + Vector3.right * tileSize / 2;

				Vector3 distance = startPoint - endPoint;
				if(Mathf.Abs(distance.y) < tileSize * streetModule) {
					if(PlaceRoad(startPoint, endPoint, Direction.Horizontal))
						PlanRoad(thisCross, nextCross);
				}
											

			}
			
		}

		if(y-mod > 0)
		{
			Tile nextCross = tiles[x, y-mod];
			if(nextCross.type == TileType.Intersection) {
				Vector3 startPoint = thisCross.localPosition - Vector3.forward * tileSize / 2;
				Vector3 endPoint = nextCross.localPosition + Vector3.forward * tileSize / 2;

				Vector3 distance = startPoint - endPoint;
				if(Mathf.Abs(distance.y) < tileSize * streetModule) {
					if(PlaceRoad(startPoint, endPoint, Direction.Vertical))
						PlanRoad(thisCross, nextCross);
				}
					
								

			}
			
		}
		return null;
	}

	void PlanRoad(Tile start, Tile end) {
		int x_min = 0;
		int x_max = 0;

		int y_min = 0;
		int y_max = 0;

		
		if(start.ux == end.ux) {
			x_min = start.ux;
			x_max = start.ux;

			y_min = Mathf.Min(start.uy, end.uy) + 1;
			y_max = Mathf.Max(start.uy, end.uy) - 1;
		}

		if(start.uy == end.uy) {
			y_min = start.uy;
			y_max = start.uy;

			x_min = Mathf.Min(start.ux, end.ux) + 1;
			x_max = Mathf.Max(start.ux, end.ux) - 1;
		}

		for(int x = x_min; x <= x_max; x++) {
			for(int y = y_min; y <= y_max; y++) {
				tiles[x,y].type = TileType.Road;
			}
		}
	}



	bool PlaceRoad(Vector3 startPoint, Vector3 endPoint, Direction direction) {
			Vector3 start = transform.TransformPoint(startPoint);
			Vector3 end = transform.TransformPoint(endPoint);
			Vector3 directionVector = end - start;
			float distance = directionVector.magnitude;
			float localDistance = (startPoint - endPoint).magnitude;

			if(Physics.Raycast(start, directionVector, distance)) return false;
			Debug.DrawRay(start, directionVector, Color.yellow, 60f);

			Vector3 sideVector;
			if(direction == Direction.Horizontal) sideVector = Vector3.forward; 
			else sideVector = Vector3.right;

			Vector3 rightSide = transform.TransformPoint(startPoint + sideVector * tileSize / 4);
			Vector3 leftSide = transform.TransformPoint(startPoint - sideVector * tileSize / 4);

			Debug.DrawRay(rightSide, directionVector, Color.yellow, 60f);
			if(Physics.Raycast(rightSide, directionVector, distance)) return false;
			if(Physics.Raycast(leftSide, directionVector, distance)) return false;

			// check middle
			Vector3 center = (endPoint + startPoint) / 2;

			GameObject newRoad = Instantiate(straightPrefab);
			newRoad.transform.SetParent(roadRoot.transform, false);

			Vector3 verticalOffset = Vector3.up * (endPoint.y - startPoint.y);

			float sin = verticalOffset.y / localDistance;
			float angle = Mathf.Asin(sin) * Mathf.Rad2Deg;

			newRoad.transform.localPosition = center;
			if(direction == Direction.Vertical) newRoad.transform.localRotation = Quaternion.Euler(angle, 0, 0);
			else newRoad.transform.localRotation = Quaternion.Euler(angle, 90, 0);
			newRoad.transform.localScale = new Vector3(1, 1, localDistance / tileSize);

			return true;

	}
	

	float[,] SamplePerlinNoise(int length) {
		float offset = Random.value * 10f;
		float[,] perlinValues = new float[length, length];
		float radius = (length - 1) / 2;
		float radiusSquare = radius * radius;
		for(int x = 0; x < length; x++) {
			for(int y = 0; y < length; y++) {
				float xCircular = x - radius;
				float yCircular = y - radius;
				float xSquare = xCircular * xCircular;
				float ySquare = yCircular * yCircular;
				float value = 0f;
				if(xSquare + ySquare < radiusSquare) {
					value = 0f;
					float xNormalized = (float) x / (float) length;
					float yNormalized = (float) y / (float) length;
					float componentA = Mathf.PerlinNoise(offset + xNormalized * scaleNoise, offset + yNormalized * scaleNoise);
					float componentB = Mathf.PerlinNoise(offset + xNormalized * scaleNoise * 2, offset + yNormalized * scaleNoise * 2);
					Vector2 point = new Vector2(xCircular, yCircular);
					float weight = 1 - point.magnitude / radius;
					value = (0.5f * componentA + 0.5f * componentB) * weight * 2;
				}
				perlinValues[x,y] = Mathf.Clamp01(value);
				
			}
		}
		return perlinValues;
	}

}

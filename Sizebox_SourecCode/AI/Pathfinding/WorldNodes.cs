using UnityEngine;

public class WorldNodes {
	static PathNode[,] nodeArray;
	static int instanceID;
	int arrayLength;

	int vx;
	int vz;


	float tileWidth;
	float height;
	float step;
	float angleMutiplier = 0.5f;

	float rayDuration = 30f;

	public int checkCount;
	public int maxChecks = 1000;

	bool debugNodes = true;
	Vector3 down;
	Vector3 up;
	LayerMask pathfindMask;

	public WorldNodes(Vector3 virtualStart, float height, float tilewidth, float step, float angle) {
		angleMutiplier = angle;
		this.tileWidth = tilewidth;
		this.height = height;
		this.step = step;

		PathNode origin = NodeFromPoint(virtualStart);
		origin.walkable = true;

		checkCount = 0;

		down = Vector3.down;
		up = Vector3.up;
		pathfindMask = Layers.pathfindingMask;

		instanceID++;
		int center = maxChecks + 1;
		arrayLength = center * 2;

		if(nodeArray == null) {			
			nodeArray = new PathNode[arrayLength, arrayLength];
		}

		origin.instanceID = instanceID;
		nodeArray[center, center] = origin;

		vx = center - origin.x;
		vz = center - origin.z;

		Debug.DrawRay(origin.realPosition, up * height * 2, Color.red, 10f);
		
		
	}

	public void UpdateNodes(Vector3 start, int radius) {

		#if UNITY_EDITOR
		Vector3 startLocal = CenterOrigin.VirtualToWorld(start);
		startLocal.y += height;
		Debug.DrawRay(startLocal, down * height, Color.red, rayDuration);
		#endif
		
	}

	public PathNode[] GetConnections(PathNode node) {
		if(checkCount < maxChecks && !node.visited) {
			FindNeighboors(node, 2);
			UpdateWalkability(node);
			node.visited = true;
			checkCount++;
		} 

		return node.GetConnections();
	}

	void FindNeighboors(PathNode node, int levels = 0) {
		for(int x = node.x - 1; x <= node.x + 1; x++) {
			for(int z = node.z - 1; z <= node.z + 1; z++) {

				if((x == node.x && z == node.z) || (x != node.x && z != node.z)) continue;
				

				PathNode newNode = GetNodeIfExists(x,z);

				bool nodesConnected = false;

				if(newNode == null) newNode = SampleNode(x, node.y, z);
				else {
					if(newNode.visited || newNode.walkable) continue;
					if(NodesAreConnected(node, newNode)) nodesConnected = true;
				}
				
				if(newNode == null) continue;

				if(!nodesConnected && CheckWalkability(node, newNode)) {
					nodesConnected = true;
					if(node.x != newNode.x) {
						if(node.x > newNode.x) {
							node.est = newNode;
							newNode.west = node;
						} else {
							node.west = newNode;
							newNode.est = node;
						}
					} else {
						if(node.z > newNode.z) {
							node.north = newNode;
							newNode.south = node;
						} else {
							node.south = newNode;
							newNode.north = node;
						}
					}
					
				}
				
				if(nodesConnected && levels > 1) {
					FindNeighboors(newNode, levels - 1);
				}
			}
		}

	}


	void UpdateWalkability(PathNode node) {
		for(int x = node.x - 1; x <= node.x + 1; x++) {
			for(int z = node.z - 1; z <= node.z + 1; z++) {

				if(x == node.x && z == node.z) continue;

				PathNode thisNode = GetNodeIfExists(x,z);
				if(thisNode == null) continue;

				if(!thisNode.walkable && x != node.x && z != node.z) FindNeighboors(thisNode); // Look at diagonal, just for final touches
				if(thisNode.CanWalkAllDirections()) {
					thisNode.walkable = true;
					#if UNITY_EDITOR
					if(debugNodes) DrawLineBetweenNodes(node, thisNode, Color.cyan);
					#endif
				} 		
			}
		}
		
	}

	bool NodesAreConnected(PathNode a, PathNode b) {
		return (a.north == b || a.south == b || a.est == b || a.west == b);
	}

	public void DrawLineBetweenNodes(PathNode a, PathNode b, Color color, float time = 0f) {
		#if UNITY_EDITOR
		if(time == 0) time = rayDuration;
		Vector3 pointA = a.realPosition;
		Vector3 pointB = b.realPosition;

		pointA.y += step;
		pointB.y += step;

		Debug.DrawLine(pointA, pointB, color, time);
		#endif
	}

	PathNode GetNode(int x, int z) {

		int xpos = vx + x;
		int zpos = vz + z;

		if(xpos < 0 || xpos >= arrayLength || zpos < 0 || zpos >= arrayLength) {
			return new PathNode(x,z);
		}

		PathNode node = nodeArray[xpos, zpos];

		if(node == null) {
			node = new PathNode(x,z);
			nodeArray[xpos, zpos] = node;
			node.instanceID = instanceID;

		} else if(node.instanceID != instanceID) {
			node.ResetValues(x,z);
			// node = new Node(x,z);
			node.instanceID = instanceID;
			nodeArray[xpos, zpos] = node;
		}

		return node;
	}

	public PathNode GetNodeIfExists(int x, int z) {
		int xpos = vx + x;
		int zpos = vz + z;

		if(xpos < 0 || xpos >= arrayLength || zpos < 0 || zpos >= arrayLength) {
			return null;
		}

		PathNode node = nodeArray[xpos, zpos];

		if(node != null && node.instanceID == instanceID) return node;
		return null;
	}




	bool CheckWalkability(PathNode currentNode, PathNode previousNode) {
		if(previousNode == null) return false;

		float dY = currentNode.y - previousNode.y;
		if(dY < 0) dY = -dY;
		if(dY > tileWidth * angleMutiplier) return false;

		Vector3 a = currentNode.realPosition;
		Vector3 b = previousNode.realPosition;

		a.y += step;
		b.y += step;

		Vector3 distanceVector = Substract(b,a);
		float distance = distanceVector.magnitude;

		if (Physics.Raycast(a, distanceVector, distance, pathfindMask)) {
			#if UNITY_EDITOR
			if(debugNodes) DrawLineBetweenNodes(currentNode, previousNode, Color.red);
			#endif
			return false;
		}
		if (Physics.Raycast(b, Substract(a,b), distance, pathfindMask)) {
			#if UNITY_EDITOR
			if(debugNodes) DrawLineBetweenNodes(currentNode, previousNode, Color.red);
			#endif
			return false;
		}
		return true;
	}

	

	PathNode SampleNode(int x, float y, int z) {
		Vector3 localPoint = CenterOrigin.VirtualToWorld(x * tileWidth, y, z * tileWidth);
		float verticalOffset = (height + tileWidth) * angleMutiplier;
		localPoint.y += verticalOffset;
		RaycastHit hit;
		if (Physics.Raycast(localPoint, down, out hit, verticalOffset * 2, pathfindMask)) {

			Vector3 tilePoint = hit.point;
			float y_orig = tilePoint.y;
			tilePoint.y += step;

			if(Physics.Raycast(tilePoint, up, height, pathfindMask)) {
				return null;
			}

			PathNode newNode = GetNode(x,z);
			newNode.y = CenterOrigin.WorldToVirtual(0, y_orig, 0).y;

			newNode.realPosition.x = tilePoint.x;
			newNode.realPosition.y = y_orig;
			newNode.realPosition.z = tilePoint.z;

			return newNode;
		}
		return null;
	}

	Vector3 Substract(Vector3 a, Vector3 b) {
		a.x -= b.x;
		a.y -= b.y;
		a.z -= b.z;
		return a;
	}

	Vector3 Sum(Vector3 a, Vector3 b) {
		a.x += b.x;
		a.y += b.y;
		a.z += b.z;
		return a;
	}

	public Vector3 LocalizeCoordinates(PathNode node) {
		return CenterOrigin.VirtualToWorld(node.x * tileWidth, node.y, node.z * tileWidth);
	}

	public PathNode PointToNode(Vector3 point) {
		int x = QuantizeFloat(point.x);
		int z = QuantizeFloat(point.z);
		PathNode node = SampleNode(x, point.y, z);
		if(node != null) return node;

		node = GetNode(x,z); 

		node.y = point.y;
		node.walkable = true;
		node.realPosition = LocalizeCoordinates(node);

		return node;
	}

	PathNode NodeFromPoint(Vector3 point) {
		int x = QuantizeFloat(point.x);
		int z = QuantizeFloat(point.z);

		PathNode node = new PathNode(x,z); 

		node.y = point.y;
		node.walkable = true;
		node.realPosition = LocalizeCoordinates(node);

		return node;
	}

	int QuantizeFloat(float f) {
		float division = f / tileWidth;
		if(division > 0) return (int) (division + 0.5f);
		return (int) (division - 0.5f);
	}

}

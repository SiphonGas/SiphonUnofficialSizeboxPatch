using System.Collections.Generic;
using UnityEngine;

public class PathNode {
	static int normalCost = 100;
	static int diagonalCost = 141;
	public static PathNode[] connections;
	public static int connectionsLength;

	public int instanceID;
	public int x;
	public int z;
	public float y;

	public PathNode north;
	public PathNode south;
	public PathNode est;
	public PathNode west;
	public bool visited = false;

	public bool walkable = false;
	public Vector3 realPosition;

	public int cost = 0;

	public NodeRecord openRecord;

	public PathNode(int x, int z) {
		if(connections == null) connections = new PathNode[8];
		this.x = x;
		this.z = z;
	}

	public void ResetValues(int x, int z) {
		this.x = x;
		this.z = z;
		
		north = null;
		south = null;
		est = null;
		west = null;

		visited = false;
		walkable = false;

		openRecord = null;
	}

	public bool CanWalkAllDirections() {
		return (north != null && south != null && est != null && west != null);
	}

	public PathNode[] GetConnections() {
		int i = 0;
		bool norhtWalkable = false;
		bool soutWalkable = false;
		bool westWalkable = false;
		bool estWalkable = false;

		if(north != null && north.walkable) {
			north.cost = normalCost;

			connections[i] = north;
			i++;

			norhtWalkable = true;
		}

		if(est != null && est.walkable) {
			est.cost = normalCost;

			connections[i] = est;
			i++;

			estWalkable = true;
			if(norhtWalkable) {
				if(north.est != null && est.north != null && north.est.walkable) {
					north.est.cost = diagonalCost;
					connections[i] = north.est;
					i++;
				}
			}
		}

		if(south != null && south.walkable) {
			south.cost = normalCost;

			connections[i] = south;
			i++;

			soutWalkable = true;
			if(estWalkable) {
				if(south.est != null && est.south != null && south.est.walkable) {
					south.est.cost = diagonalCost;
					connections[i] = south.est;
					i++;
				}
			}

		}
		if(west != null && west.walkable) {
			west.cost = normalCost;

			connections[i] = west;
			i++;

			westWalkable = true;
			if(soutWalkable) {
				if(south.west != null && west.south != null && south.west.walkable) {
					south.west.cost = diagonalCost;
					connections[i] = south.west;
					i++;
				}
			}
		}

		if(norhtWalkable && westWalkable) {
			if(north.west != null && west.north != null && north.west.walkable) {
				north.west.cost = diagonalCost;

				connections[i] = north.west;
				i++;
			}
		}
		connectionsLength = i;
		return connections;
	}
}

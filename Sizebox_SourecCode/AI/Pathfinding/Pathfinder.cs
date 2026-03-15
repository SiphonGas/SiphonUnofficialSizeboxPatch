using System.Collections.Generic;
using SteeringBehaviors;
using UnityEngine;

public class Pathfinder {
	public static Pathfinder instance { get { return GameController.Instance.pathfinder; }}

	AStar astar;

	public List<IKinematic> PlanRoute(Vector3 realStart, Vector3 realEnd, float height, float tileWidth, float step, float angle) {
		List<IKinematic> waypoints = new List<IKinematic>();

		Vector3 virtualStart = CenterOrigin.WorldToVirtual(realStart);
		Vector3 virtualEnd = CenterOrigin.WorldToVirtual(realEnd);

		WorldNodes worldNodes = new WorldNodes(virtualStart, height, tileWidth, step, angle); 
		if(astar == null) astar = new AStar();

		worldNodes.UpdateNodes(virtualStart, 50); 

		PathNode startNode = worldNodes.PointToNode(virtualStart);
		if(startNode == null) return waypoints;

		Debug.DrawRay(realStart, Vector3.up * height * 1.5f, Color.yellow, 10f);
		Debug.DrawRay(startNode.realPosition, Vector3.up * height, Color.green, 10f);

		PathNode endNode = worldNodes.PointToNode(virtualEnd);
		if(endNode == null) return waypoints;
		Debug.DrawRay(endNode.realPosition, Vector3.up * height, Color.green, 10f);

		List<PathNode> path = astar.PathfindAStart(worldNodes, startNode, endNode);

		PathNode previousNode = null;
		foreach(PathNode node in path) {
			waypoints.Add(new Kinematic(node.realPosition));
			if(previousNode != null) {
				worldNodes.DrawLineBetweenNodes(previousNode, node, Color.blue, 120f);
			}
			previousNode = node; 
		}
		return waypoints;
	}
}

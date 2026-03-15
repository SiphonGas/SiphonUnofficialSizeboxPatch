using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;


public class NodeRecord : FastPriorityQueueNode {
	public PathNode node;
	public NodeRecord previous;
	public int costSoFar = 0;
	public int estimatedTotalCost = 0;
	public int heuristic;
	
}

public class AStar {

	public class PriorityList {
		FastPriorityQueue<NodeRecord> priorityQueue;
		public int count;

		public PriorityList(int max) {
			priorityQueue = new FastPriorityQueue<NodeRecord>(max);
		}

		public void Add(NodeRecord record) {
			priorityQueue.Enqueue(record, record.estimatedTotalCost);
			count++;
		}

		public NodeRecord GetSmallestElement() {
			count--;
			return priorityQueue.Dequeue();
		}

				
	}

	public List<PathNode> PathfindAStart(WorldNodes nodes, PathNode start, PathNode end) {
		NodeRecord startRecord = new NodeRecord();
		startRecord.node = start;
		startRecord.estimatedTotalCost = CalculateEstimatedCost(start, end);
		startRecord.heuristic = startRecord.estimatedTotalCost;

		NodeRecord closestNodeToGoal = startRecord;

		PriorityList open = new PriorityList(nodes.maxChecks * 10);
		open.Add(startRecord);
		NodeRecord current = null;

		NodeRecord nextElement = null;
		NodeRecord[] endRecordList = new NodeRecord[8];

		while(nodes.checkCount < nodes.maxChecks && (open.count > 0 || nextElement != null)) {
			
			if(nextElement != null) {
				current = nextElement;
			} else {
				current = open.GetSmallestElement();
				current.node.openRecord = null;
			}
			
			
			if(current.node.x == end.x && current.node.z == end.z) {
				current.node = end;
				break;
			}

			// loop connections
			nextElement = null;
			PathNode[] connections = nodes.GetConnections(current.node);
			for(int i = 0; i < PathNode.connectionsLength; i++) {
				endRecordList[i] = null;
				PathNode connection = connections[i];
				if(connection.visited) continue;

				PathNode endNode = connection;
				NodeRecord endNodeRecord;
				int endNodeCost = current.costSoFar + connection.cost;

				if (endNode.openRecord != null) {
					endNodeRecord = endNode.openRecord;
					if(endNodeRecord.costSoFar <= endNodeCost) continue;

				} else {
					endNodeRecord = new NodeRecord();
					endNodeRecord.node = endNode;
					endNodeRecord.heuristic = CalculateEstimatedCost(endNode, end);
				}

				endNodeRecord.costSoFar = endNodeCost;
				endNodeRecord.previous = current;
				endNodeRecord.estimatedTotalCost = endNodeCost + endNodeRecord.heuristic;

				// keep the closest node to the target as the pathfinding goal
				if(endNodeRecord.heuristic < closestNodeToGoal.heuristic) {
					closestNodeToGoal = endNodeRecord;
				}

				if(nextElement == null) {
					if(endNodeRecord.estimatedTotalCost < current.estimatedTotalCost) nextElement = endNodeRecord;
				} else if(endNodeRecord.estimatedTotalCost < nextElement.estimatedTotalCost) {
					nextElement = endNodeRecord;
				}

				if(endNode.openRecord == null) {
					endRecordList[i] = endNodeRecord;
				}

			}

			for(int i = 0; i < PathNode.connectionsLength; i++) {
				NodeRecord record = endRecordList[i];
				if(record != null && record != nextElement) {
					open.Add(record);
					record.node.openRecord = record;
				}
			}

		}

		if(current.node != end) {
			current = closestNodeToGoal;
		} 

		List<PathNode> path = new List<PathNode>();
		while(current.node != start) {
			path.Add(current.previous.node);
			current = current.previous;
		}

		path.Reverse();
		return path;

	}

	public int CalculateEstimatedCost(PathNode thisNode, PathNode goalNode) {
		int deltaX = thisNode.x - goalNode.x;
		int deltaZ = thisNode.z - goalNode.z;

		if(deltaX < 0) deltaX = - deltaX;
		if(deltaZ < 0) deltaZ = - deltaZ;

		int max, min;
		

		if(deltaX > deltaZ) {
			max = deltaX;
			min = deltaZ;
		} else {
			max = deltaZ;
			min = deltaX;
		}
		
		return (max * 100 +  min * 41) * 2;
	}

	int BetterAproximation(PathNode thisNode, PathNode goalNode) {
		int deltaX = thisNode.x - goalNode.x;
		int deltaZ = thisNode.z - goalNode.z;

		if(deltaX < 0) deltaX = - deltaX;
		if(deltaZ < 0) deltaZ = - deltaZ;

		int max, min;
		

		if(deltaX > deltaZ) {
			max = deltaX;
			min = deltaZ;
		} else {
			max = deltaZ;
			min = deltaX;
		}

		int approx = max * 94 +  min * 41;

		if(max < min * 16) {
			approx -= max * 4;
		}

		return approx * 2;

	}

	

	
}

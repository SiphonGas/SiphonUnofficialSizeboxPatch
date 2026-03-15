using UnityEngine;
using System.Collections;

public class GiantessObstacleDetector : ObstacleDetector {
	CollisionDetector collisionDetector;
	bool debugRays = true;
	float lookAhead = 400f;
	bool raycastDetected = false;
	float nextCheck = 0f;
	float timeBetweenChecks = 1f;
	float scale;
	Vector3 raycastPoint;
	Vector3 raycastNormal;

	bool walkingUp = false;
	float last_y;
	float step = 0.05f;

	void Start() {
		collisionDetector = new GameObject("Collision Detector").AddComponent<CollisionDetector>();
		collisionDetector.transform.SetParent(transform, false);
		collisionDetector.transform.localPosition = Vector3.forward * lookAhead;
	}
	
	public override bool CheckObstacle()
	{
		if(walkingUp) return false;
		float currentTime = Time.time;
		if(collisionDetector.isColliding) return true;

		if(currentTime > nextCheck)
		{
			Vector3 position = transform.position;
			walkingUp = position.y > last_y + step * scale;
			last_y = position.y;

			if(walkingUp) {

			}

			scale = transform.localScale.y;
			raycastDetected = CheckForWall();
			nextCheck = currentTime + timeBetweenChecks; 
		}
		return raycastDetected;

	}

	public override Vector3 GetPoint()
	{
		if(collisionDetector.isColliding) return collisionDetector.transform.position;
		return raycastPoint;
	}

	public override Vector3 GetNormal()
	{
		if(collisionDetector.isColliding) return -transform.forward;
		return raycastNormal;
	}

	bool CheckForWall()
	{
		// normal front collision
		RaycastHit hit;
		Vector3 origin = transform.position + transform.up * 200f * scale;
		Vector3 direction = transform.forward;
		float distance = 1000f * scale;
		if(debugRays) 
			Debug.DrawLine(origin, origin + direction * distance, Color.white, timeBetweenChecks);
		if(Physics.Raycast(origin, direction, out hit, distance, Layers.gtsCollisionCheckMask))
		{
			raycastPoint = hit.point;
			raycastNormal = hit.normal;

			if(walkingUp) return false;
			return true;
		}
		return false;
	}
}

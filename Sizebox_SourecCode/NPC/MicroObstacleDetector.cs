using UnityEngine;
using System.Collections;

public class MicroObstacleDetector : ObstacleDetector {
	
	Vector3 point;
	Vector3 normal;
	float scale;
	float nextCheck = 0f;
	float timeBetweenChecks = 0.5f;
	bool obstacleDetected = false;
	public bool debugRays = true;
	Transform myTransform;
	Vector3 position;
	Vector3 up;
	Vector3 forward;
	float wallDistance = 2.5f;
	bool walkingUp = false;
	float last_y;
	float step = 0.05f;

	void Awake() {
		myTransform = transform;
	}

	// Use this for initialization
	public override bool CheckObstacle()
	{
		float currentTime = Time.time;
		if(currentTime > nextCheck)
		{
			obstacleDetected = DoRaycasts();
			nextCheck = currentTime + timeBetweenChecks; 
		}
		return obstacleDetected;
	}

	public override Vector3 GetPoint()
	{
		return point;
	}

	public override Vector3 GetNormal()
	{
		return normal;
	}

	bool DoRaycasts()
	{
		scale = myTransform.lossyScale.y;
		position = myTransform.position;

		// if walking up reduce the wall distance
		walkingUp = position.y > last_y + step * scale;
		last_y = position.y;

		up = myTransform.up;
		forward = myTransform.forward;

		return (CheckForFalling() || CheckForWall());
	}

	bool CheckForWall()
	{
		// normal front collision
		RaycastHit hit;
		Vector3 origin = position + up * 1.4f * scale;
		Vector3 direction = forward;
		float distance = wallDistance * scale;
		if(walkingUp) distance = distance * 0.3f;
		if(debugRays) 
			Debug.DrawLine(origin, origin + direction * distance, Color.white, timeBetweenChecks);
		if(Physics.Raycast(origin, direction, out hit, distance))
		{
			point = hit.point;
			normal = hit.normal;
			return true;
		}
		return false;
	}

	bool CheckForFalling()
	{
		// high collision
		Vector3 origin = position + (up * 1.6f + forward * 1f) * scale;
		Vector3 direction = (forward - up * 1.2f);
		float distance = 4f * scale;
		if(debugRays) 
			Debug.DrawLine(origin, origin + direction * distance, Color.white, timeBetweenChecks);
		if(Physics.Raycast(origin, direction, distance))
		{
			return false;
		}
		point = origin + direction * distance;
		normal = -forward; 
		return true;
	}
}

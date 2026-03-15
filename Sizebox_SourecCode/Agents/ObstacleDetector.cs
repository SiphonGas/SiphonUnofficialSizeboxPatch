using UnityEngine;

public class ObstacleDetector : MonoBehaviour {

	public virtual bool CheckObstacle()
	{
		return false;
	}

	public virtual Vector3 GetPoint()
	{
		return Vector3.zero;
	}

	public virtual Vector3 GetNormal()
	{
		return Vector3.zero;
	}
}

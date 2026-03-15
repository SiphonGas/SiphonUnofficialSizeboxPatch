using UnityEngine;
using System.Collections;

public class CollisionDetector : MonoBehaviour {
	public LayerMask layerSphereCast;
	LayerMask colliderLayer;
	public bool isColliding {get; private set;}

	// Use this for initialization
	void Start () {	
		colliderLayer = LayerMask.NameToLayer("CollisionDetector");
		string[] layerToMask = new string[1];
		layerToMask[0] = "Map";
		layerSphereCast = LayerMask.GetMask(layerToMask);

		Rigidbody rigbody = gameObject.AddComponent<Rigidbody>();
		rigbody.isKinematic = true;
		rigbody.useGravity = false;

		CapsuleCollider capCollider = gameObject.AddComponent<CapsuleCollider>();
		capCollider.isTrigger = true;
		capCollider.center = new Vector3(0,850,0);
		capCollider.radius = 250;
		capCollider.height = 1200;

		gameObject.layer = colliderLayer;

		isColliding = false;
	}

	public RaycastHit GetCollision(Vector3 position, float distance)
	{
		RaycastHit hit;
		Vector3 p1 = position + Vector3.up * 850;
		if(Physics.SphereCast(p1, 200f, transform.forward, out hit, distance, layerSphereCast))
		{
			Debug.Log("Hit");
			Debug.Log("Hit point:" + hit.point);
		}
		return hit;
	}

	void OnTriggerEnter(Collider mapCollider)
	{
		if(mapCollider.gameObject.layer == Layers.mapLayer)
		{
			mapCollider.bounds.ClosestPoint(transform.position);
			isColliding = true;
		}
	}

	void OnTriggerExit(Collider mapCollider)
	{
		if(mapCollider.gameObject.layer == Layers.mapLayer)
		{
			isColliding = false;
		}
	} 


	public void DoSphereCast()
	{
		RaycastHit hit;
		Vector3 p1 = transform.position + Vector3.up * 850;
		if(Physics.SphereCast(p1, 1000, transform.forward, out hit, 2000, layerSphereCast))
		{
			Debug.Log("Hit");
		}
	}

}

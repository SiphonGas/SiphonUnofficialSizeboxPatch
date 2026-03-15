using UnityEngine;
using System.Collections;

public class GiantessBone : MonoBehaviour {
	public bool canCrush = true;
	public bool ignoreCameraCollision = false;
	public bool isGrabbing = false;
	public Giantess giantess;

	void Start() {
		Rigidbody kinematicRigidbody = gameObject.AddComponent<Rigidbody>();
		kinematicRigidbody.isKinematic = true;
		kinematicRigidbody.useGravity = false;
		kinematicRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

		//MeshCollider meshCollider = GetComponent<MeshCollider>();
		//meshCollider.convex = true;
	}

	public void Initialize(Giantess gts) {
		giantess = gts;
	}
}

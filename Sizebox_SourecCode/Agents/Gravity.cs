using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour {
	public static float gravity = 9.8f;
	public float baseScale = 1f;
	public bool useGravity = true;
	Rigidbody rbody;
	Transform myTransfom;

	// Use this for initialization
	void Start () {
		rbody = GetComponent<Rigidbody>();
		if(!rbody) gameObject.AddComponent<Rigidbody>();
		rbody.useGravity = false;
		myTransfom = transform;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if(!useGravity) return;
		float size = myTransfom.lossyScale.y * baseScale;
		rbody.AddForce(new Vector3(0, - gravity * size, 0));
	}
}

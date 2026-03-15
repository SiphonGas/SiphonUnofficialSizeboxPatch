using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructor : MonoBehaviour {
	public float baseSize = 1f;

	void OnCollisionEnter(Collision collision) {
		if(collision.gameObject.layer != Layers.buildingLayer) return;
		IDestructible destructible = collision.gameObject.GetComponentInParent<IDestructible>();
		if(destructible == null) return;
		destructible.Destroy(collision.contacts[0].point, transform.lossyScale.y * baseSize);
	}
}

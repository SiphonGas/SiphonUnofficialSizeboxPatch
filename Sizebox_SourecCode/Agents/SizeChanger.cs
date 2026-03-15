using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizeChanger : MonoBehaviour {
	EntityBase entity;
	public float speed = 0f;

	// Use this for initialization
	void Start () {
		entity = GetComponent<EntityBase>();
	}
	
	// Update is called once per frame
	void Update () {
		if(speed == 0f) return;
		float scale = entity.transform.localScale.y;
		float newScale = scale + scale * speed * Time.deltaTime;

		entity.ChangeScale(newScale);
		
		if(newScale > entity.maxSize || newScale < entity.minSize) {
			speed = 0f;
		}
	}
}

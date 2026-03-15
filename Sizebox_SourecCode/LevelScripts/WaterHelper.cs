using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterHelper : MonoBehaviour {
	Material mat;
	bool reflexionsEnabled = true;
	bool underWater = false;
	Transform cam;
	float maxScale = 80;
	float waterLevel = 200;

	// Use this for initialization
	void Start () {
		mat = GetComponent<MeshRenderer>().material;
		cam = Camera.main.transform.parent;
	}
	
	// Update is called once per frame
	void Update () {
		float scale = cam.localScale.y;

		// Check Reflexions
		if(scale > maxScale && reflexionsEnabled) {
			mat.SetFloat("_EnableReflections", 0f);
			reflexionsEnabled = false;
		} else if(scale < maxScale && !reflexionsEnabled) {
			mat.SetFloat("_EnableReflections", 0.6f);
			reflexionsEnabled = true;
		}

		// Check Underwater
		float cameraHeight = CenterOrigin.WorldToVirtual(cam.localPosition).y;

		if(cameraHeight > waterLevel && underWater) {
			mat.SetFloat("_UnderwaterMode", 0f);
			underWater = false;
		} else if(cameraHeight < waterLevel && !underWater) {
			mat.SetFloat("_UnderwaterMode", 0.6f);
			underWater = true;
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class PoseHandle : MonoBehaviour {
	IKEffector effector;
	IKConstraintBend bend;
	IKSolverLookAt lookAt;
	bool update = false;
	float nextUpdate;
	LineRenderer line;
	Vector3 originPosition = Vector3.zero;

	// Use this for initialization
	public void SetEffector(IKEffector effector, Transform parent, float scale) {
		SetScale(scale);
		this.effector = effector;
		bend = null;

		gameObject.SetActive(true);
		update = true;
		nextUpdate = Time.time + 0.3f;
		transform.SetParent(parent);
	}

	public void SetBendGoal(IKConstraintBend bend, Transform parent, float scale) {
		SetScale(scale);
		this.bend = bend;
		effector = null;

		if(line == null) line = gameObject.AddComponent<LineRenderer>();
		line.numPositions = 2;
		line.material = GetComponentInChildren<Renderer>().material;
		line.startColor = Color.yellow;
		line.endColor = Color.yellow;
		line.widthMultiplier = 5f;

		gameObject.SetActive(true);
		update = true;
		nextUpdate = Time.time + 0.3f;
		transform.SetParent(parent);
	}

	void SetScale(float scale) {
		transform.SetParent(null);
		transform.localScale = new Vector3(scale,scale,scale);
	}

	public void SetLookAt(IKSolverLookAt lookAt, Transform parent, float scale) {
		SetScale(scale);
		this.lookAt = lookAt;

		if(line == null) line = gameObject.AddComponent<LineRenderer>();
		line.numPositions = 2;
		line.material = GetComponentInChildren<Renderer>().material;
		line.startColor = Color.yellow;
		line.endColor = Color.yellow;
		line.widthMultiplier = 5f;

		gameObject.SetActive(true);
		update = true;
		nextUpdate = Time.time + 0.3f;
		transform.SetParent(parent);
	}

	public void UpdatePosition() {
		if(effector != null) {
			transform.position = effector.position;
			transform.rotation = effector.rotation;
		} else if(bend != null) {
			transform.position = bend.bendGoal.position;
			transform.rotation = bend.bendGoal.rotation;
		} else if(lookAt != null) {
			transform.position = lookAt.IKPosition;
		}
		
	}
	
	// Update is called once per frame
	void Update () {
		if(effector == null && bend == null && lookAt == null) {
			gameObject.SetActive(false);
			return;	
		} 
		if(update && Time.time > nextUpdate) {
			UpdatePosition();
			update = false;
		} else if(bend != null) {
			originPosition = bend.bone2.position;
		} else if(lookAt != null) {
			originPosition = lookAt.head.transform.position;
		}
	}

	void LateUpdate() {
		if(update) return;
		if(effector != null) {
			effector.position = transform.position;
			effector.rotation = transform.rotation;
		} else if(bend != null) {
			bend.bendGoal.position = transform.position;
			bend.bendGoal.rotation = transform.rotation;
			line.widthMultiplier = 5f * transform.lossyScale.y;
			line.SetPosition(0, originPosition);
			line.SetPosition(1, bend.bendGoal.position);
		} else if(lookAt != null) {
			lookAt.IKPosition = transform.position;
			line.widthMultiplier = 5f * transform.lossyScale.y;
			line.SetPosition(0, originPosition);
			line.SetPosition(1, lookAt.IKPosition);
		}
	}

}

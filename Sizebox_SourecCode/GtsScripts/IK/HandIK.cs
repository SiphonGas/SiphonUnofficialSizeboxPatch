using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class HandIK {
	FullBodyBipedIK ik;
	Giantess giantess;
	EntityBase target;
	Transform hand;
	IKBone handEffector;
	float targetWeight;
	Vector3 targetPos;


	delegate void State();
	State CurrentState;

	public HandIK(FullBodyBipedIK ik, Giantess giantess) {
		this.ik = ik;
		this.giantess = giantess;

		hand = ik.references.rightHand;
		if(giantess.ik == null) Debug.LogError("No IK");
		if(giantess.ik.rightFootEffector == null) Debug.LogError("No hand ik effector");
		handEffector = giantess.ik.rightHandEffector;

		targetWeight = 0f;
		ik.solver.leftArmMapping.weight = 0f;

		CurrentState = Idle;
	}

	public void GrabTarget(EntityBase entity) {
		// Debug.Log(ik.name + " tries to grab " +  entity.name);
		target = entity;
		CurrentState = Grab;
	}

	public void CancelGrab() {
		target = null;
		CurrentState = Return;
	}

	public bool GrabCompleted() {
		return !IsActive(); // || target == null || target.isDead;
	}
	
	public void Update () {
		CurrentState();
		UpdateEffector();
	}

	public bool IsActive() {
		return CurrentState != Idle;
	}

	void UpdateEffector() {
		Vector3 currentPosition = handEffector.position;
		Vector3 newPosition = Vector3.MoveTowards(currentPosition, targetPos, Time.fixedDeltaTime * 25f);

		float currentWeight = handEffector.positionWeight;
		float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.fixedDeltaTime);

		handEffector.position = newPosition;
		handEffector.positionWeight = newWeight;
		ik.solver.rightArmMapping.weight = newWeight;
	}

	// ================= States ======================= //
	void Idle() {
		targetWeight = 0f;
		handEffector.position = hand.position;
	}

	void Grab() {
		if(target == null || target.isDead) {CurrentState = Return; return; }
		float radius = 800f * giantess.Scale;

		targetWeight = 1f;
		targetPos = target.transform.position;


		// if is close to a certain distance, the target is grabbed
		float distanceToHand = (hand.position - targetPos).sqrMagnitude;
		float maxDistance = giantess.AccurateScale * 0.1f;
		float sqrMaxDistance = maxDistance * maxDistance;
		
		if(distanceToHand < sqrMaxDistance) {
			CurrentState = MoveUp;
			grabTime = Time.time;
			return;
		} 

		float distanceToCenter = (giantess.transform.position - targetPos).sqrMagnitude;
		if(distanceToCenter > radius * radius) {
			CurrentState = Return;
			return;
		}
	}

	float grabTime;
	float holdTime = 20f;

	void MoveUp() {
		target.transform.SetParent(hand);
		target.Lock();
		targetPos = giantess.transform.TransformPoint(new Vector3(100f, 1400f, 300f));
		if(Time.time > grabTime + holdTime) {
			CurrentState = Return;
		}
	}


	void Return() {
		targetWeight = 0f;
		targetPos = hand.transform.position;
		if(handEffector.positionWeight < 0.05f) {
			CurrentState = Idle;
		}
	}
}

/*

What i need to do

1. Fix the micros transform, to be up with the normal
2. Fix area of grabbing, check distance, don't grab in backwards
5. Fix legs
6. Try different animations
7. Fix first person camera
8. Finger position

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class HeadIK {
	public static bool lookAtPlayer = true;
	LookAtIK ik;
	Giantess giantess;
	EntityBase target;
	Vector3 lookDirection;
	Transform head;
	float speed;
	float bodyWeight = 0.2f;
	float headWeight = 1f;

	float bodyDistance = 1000f;
	float headDistance = 400f;

	float targetWeight;

	public bool unlockValues = false;


	Vector3 virtualTargetPosition;
	Vector3 targetPosition {
		get { return CenterOrigin.VirtualToWorld(virtualTargetPosition);}
		set { virtualTargetPosition = CenterOrigin.WorldToVirtual(value); }
	}

	Vector3 virtualIKPosition;
	Vector3 ikPosition {
		get { return CenterOrigin.VirtualToWorld(virtualIKPosition);}
		set { virtualIKPosition = CenterOrigin.WorldToVirtual(value); }
	}



	delegate void State();
	State CurrentState;

	public HeadIK(LookAtIK ik, Giantess giantess) {
		this.ik = ik;
		this.giantess = giantess;

		head = giantess.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);

		SetDefaultValues();
		
	}

	public void SetDefaultValues() {
		ik.enabled = true;
		ik.fixTransforms = false;

		ik.solver.bodyWeight = bodyWeight;
		ik.solver.headWeight = headWeight;
		ik.solver.eyesWeight = 0.4f;

		ik.solver.clampWeight = 0.5f;
		ik.solver.clampWeightHead = 0.6f;
		ik.solver.clampWeightEyes = 0.7f;
		ik.solver.clampSmoothing = 2;

		CurrentState = Start;
	}

	public void LookAt(EntityBase entity) {
		target = entity;
		CurrentState = Look;
	}

	public void DisableLookAt() {
		CurrentState = TotalDisable;
	}

	public void LookAtPoint(Vector3 point) {
		if(!lookAtPlayer) return;
		lookDirection = point;
		CurrentState = LookPoint;
	}

	public void Cancel() {
		target = null;
		CurrentState = Start;
	}

	public void Update() {
		CurrentState();
		UpdateEffector();
	}

	void UpdateEffector() {
		// don't look backwards
		speed = giantess.animationManager.GetCurrentSpeed();

		float distanceFromHead = (giantess.transform.InverseTransformPoint(head.position) - giantess.transform.InverseTransformPoint(targetPosition)).magnitude;
		ik.solver.headWeight = Mathf.Clamp01(distanceFromHead/headDistance) * headWeight;
		ik.solver.bodyWeight = Mathf.Clamp01(distanceFromHead/bodyDistance) * bodyWeight;

		ik.solver.IKPositionWeight = Mathf.Lerp(ik.solver.IKPositionWeight, targetWeight, Time.fixedDeltaTime * speed);
		ik.solver.IKPosition = Vector3.Slerp(ikPosition, targetPosition, Time.fixedDeltaTime * 4f * speed);

		ikPosition = ik.solver.IKPosition;
	}

	// ============== States ================//
	
	void Start() {
		if(target == null || target.isDead) {
			target = GameController.playerInstance;
		}		
		targetWeight = 0f;
		if(giantess.senses.CheckVisibility(target)) {
			CurrentState = Look;
		}
		
	}

	void CantSee() {
		targetWeight = Mathf.Lerp(targetWeight, 0.1f, Time.deltaTime * speed * 0.2f);
		if(target != null && giantess.senses.CheckVisibility(target)) {
			CurrentState = Look;
		}
	}

	void Look() {
		if(target == null || target.isDead) {
			CurrentState = Start; return;
		}


		targetPosition = target.transform.position + Vector3.up * target.Height * 0.9f;
		targetWeight = 1f;

		if(!lookAtPlayer) CurrentState = Disabled;
		else if(!giantess.senses.CheckVisibility(target)) CurrentState = CantSee;
	}

	void LookPoint()
	{
		targetWeight = 1f;
		targetPosition = lookDirection;
		if(!lookAtPlayer) CurrentState = Disabled;
	}

	void Disabled() {
		targetWeight = 0f;
		if(lookAtPlayer) CurrentState = Start;
	}

	void TotalDisable() {
		targetWeight = 0f;
	}
}

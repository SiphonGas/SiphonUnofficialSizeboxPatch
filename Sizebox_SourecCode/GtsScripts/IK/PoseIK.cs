using RootMotion.FinalIK;
using UnityEngine;
using System.Collections.Generic;

public class PoseIK {
	FullBodyBipedIK ik;
	LookAtIK headIk;
	EntityBase entity;
	public IKSolverLookAt head;
	public IKEffector body;
	public IKEffector leftHand;
	public IKEffector rightHand;
	public IKEffector leftFoot;
	public IKEffector rightFoot;
	public IKEffector leftShoulder;
	public IKEffector rightShoulder;
	public IKEffector leftThight;
	public IKEffector rightThight;
	List<IKEffector> effectors;
	public IKConstraintBend leftElbow;
	public IKConstraintBend rightElbow;
	public IKConstraintBend leftKnee;
	public IKConstraintBend rightKnee;
	List<IKConstraintBend> bendgoals;
	

	public PoseIK(FullBodyBipedIK ik, LookAtIK lookAt, EntityBase entity) {
		this.ik = ik;
		headIk = lookAt;
		this.entity = entity;
		effectors = new List<IKEffector>();
		bendgoals = new List<IKConstraintBend>();

		// Body IK

		body = ik.solver.bodyEffector;
		effectors.Add(body);

		leftHand = ik.solver.leftHandEffector;
		effectors.Add(leftHand);

		rightHand = ik.solver.rightHandEffector;
		effectors.Add(rightHand);

		rightFoot = ik.solver.rightFootEffector;
		effectors.Add(rightFoot);

		leftFoot = ik.solver.leftFootEffector;
		effectors.Add(leftFoot);

		leftShoulder = ik.solver.leftShoulderEffector;
		effectors.Add(leftShoulder);

		rightShoulder = ik.solver.rightShoulderEffector;
		effectors.Add(rightShoulder);

		rightThight = ik.solver.rightThighEffector;
		effectors.Add(rightThight);

		leftThight = ik.solver.leftThighEffector;
		effectors.Add(leftThight);

		leftElbow = ik.solver.leftArmChain.bendConstraint;
		leftElbow.bendGoal = new GameObject("Bend Goal").transform;
		leftElbow.bendGoal.SetParent(entity.transform);
		bendgoals.Add(leftElbow);

		rightElbow = ik.solver.rightArmChain.bendConstraint;
		rightElbow.bendGoal = Object.Instantiate<Transform>(leftElbow.bendGoal, entity.transform);
		bendgoals.Add(rightElbow);

		leftKnee = ik.solver.leftLegChain.bendConstraint;
		leftKnee.bendGoal = Object.Instantiate<Transform>(leftElbow.bendGoal, entity.transform);
		bendgoals.Add(leftKnee);

		rightKnee = ik.solver.rightLegChain.bendConstraint;
		rightKnee.bendGoal = Object.Instantiate<Transform>(leftElbow.bendGoal, entity.transform);
		bendgoals.Add(rightKnee);

		// headIk
		head = headIk.solver;
	}

	public void ResetEffectors() {
		Reset();
	}

	void Reset() {
		foreach(IKEffector effector in effectors) {
			effector.position = effector.bone.position;
			effector.rotation = effector.bone.rotation;
		}
		foreach(IKConstraintBend bend in bendgoals) {
			Vector3 middle = (bend.bone1.position + bend.bone3.position) / 2;
			Vector3 direction = (bend.bone2.position - middle).normalized;

			bend.bendGoal.position = bend.bone2.position + direction * 200f * entity.Scale;
		}

		head.IKPosition = head.head.transform.position + head.head.transform.forward * 500f;
		head.headWeight = 1f;
		head.eyesWeight = 1f;
		head.bodyWeight = 0f;
		head.clampWeight = 0f;
		head.clampWeightHead = 0f;
		head.clampWeightEyes = 0f;

		ik.enabled = true;
		headIk.enabled = true;

		SetWeight(1f);
	}

	public void SetWeight(float weight) {
		foreach(IKEffector effector in effectors) {
			effector.positionWeight = weight;
			effector.rotationWeight = weight;
		}
		foreach(IKConstraintBend bend in bendgoals) {
			bend.weight = weight;
		}
		head.IKPositionWeight = weight;
		ik.solver.leftArmMapping.weight = weight;
		ik.solver.rightArmMapping.weight = weight;

		if(weight > 0) {
			ik.enabled = true;
			headIk.enabled = true;
		} else {
			ik.enabled = false;
			headIk.enabled = false;
		}
		
	}

}

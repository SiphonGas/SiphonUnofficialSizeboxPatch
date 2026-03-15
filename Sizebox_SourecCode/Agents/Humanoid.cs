using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using AI;

public class Humanoid : EntityBase {
	public static RuntimeAnimatorController poseAnimator;
	public bool poseMode {get; private set;}
	Animator anim;

	Transform head;
	Transform leftEye;
	Transform rightEye;

	protected override void Awake()
	{
		isHumanoid = true;
		base.Awake();
		if(poseAnimator == null) poseAnimator = (RuntimeAnimatorController) Resources.Load("GTSStaticAnimator", typeof(RuntimeAnimatorController));
		actionManager = gameObject.AddComponent<ActionManager>();
		anim = GetComponent<Animator>();
		senses = new SenseController(this);

		// ========== AI Initialization =============== //
		ai = gameObject.AddComponent<AIController>();
		ai.Initialize(this);


		head = anim.GetBoneTransform(HumanBodyBones.Head);
		leftEye = anim.GetBoneTransform(HumanBodyBones.LeftEye);
		rightEye = anim.GetBoneTransform(HumanBodyBones.RightEye);
	}

	public override Vector3 GetEyesPosition() {
		if(head != null) {
			
		}
		if(leftEye != null && rightEye != null) {
			return (leftEye.position + rightEye.position) * 0.5f;
		} 
		return head.position;
	}

	public virtual void SetPoseMode(bool value)
	{
		if(poseMode != value) {
			poseMode = value;
			// fix bug when character changes position after changing animator
			Vector3 originalPosition = myTransform.position;

			if(poseMode) anim.runtimeAnimatorController = poseAnimator;
			else anim.runtimeAnimatorController = Giantess.giantessAnimatorController;

			myTransform.position = originalPosition;
			if(ik != null) ik.SetPoseMode(poseMode);
		}	
	}

	public virtual void OnStep()
	{
		// nothing to do here
	}

	public override List<EntityType> GetTypesEntity() {
		List<EntityType> types = base.GetTypesEntity();
		types.Add(EntityType.Humanoid);
		return types;
	}

	public override List<Behavior> GetListBehaviors()
	{
		List<Behavior> behaviors = BehaviorLists.GetBehaviors(EntityType.Humanoid);
		behaviors.AddRange(base.GetListBehaviors());
		return behaviors;
	}
}

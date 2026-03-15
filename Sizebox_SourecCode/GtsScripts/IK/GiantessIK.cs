using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;

public class GiantessIK : MonoBehaviour {
	public bool luaIKEnabled = false;
	// Character Components
	FullBodyBipedIK ik;
	LookAtIK lookAtIK;
	PlayerCamera playerCamera;
	Animator anim;	
	public GrounderFBBIK grounder;
	FootEffect footEffect;
	Giantess giantess;


	// General Data
	public EntityBase target;

	// Head Tracking
	
	public Transform headT;
	Transform rightFoot;
	Transform leftFoot;
	Transform rightShoulder;
	Transform hips;
	AnimationManager animManager;

	// Foot Tracking
	public bool useGrounder = false;
	public bool crushEnded;
	bool crushTarget = false;
	public bool cancelFootCrush = false;
	public float maxOffset = 400f;
	public float minTimeToPrepare = 1.5f;
	public float returnSpeed = 2f;
	public float maxDistanceFoot = 0.35f;
	public float crushSpeed = 3f;
	Transform footTargetTransform;
	float startPreparationTime;
	float footForwardDisplacement = 40f;
	Vector3 footTarget;
	Vector3 stayPosition;
	enum FootStates { Idle, Prepare, Crush, Wait, Return};
	FootStates footState = FootStates.Idle;
	float bothFootWeight = 0f;

	float offsetPercentage = 1f;
	public IKBone rightFootEffector;
	public IKBone leftFootEffector;
	IKBone activeFoot;

	Vector3 footSize;

	MeshCollider rightFootCollider;
	MeshCollider leftFootCollider;
	MeshCollider activeFootCollider;

	// Hip Tracking
	public IKBone leftHandEffector;
	public IKBone rightHandEffector;
	public IKBone bodyEffector;
	enum ButtState { Idle, Sitting, Sit, Standing };
	ButtState buttState = ButtState.Idle;
	public bool buttEffectorActive = false;
	Vector3 sitPosition;
	bool sit = false;
	float ButtOffset = 180f;
	bool cancelButt = false;	
	GiantessBone[] handBones;
	// Butt Parameteres

	// Others
	float gtsScale;
	float currentGtsSpeed = 1f;

	// IK Subsystems
	public HandIK hand;
	public HeadIK head;
	public PoseIK poseIK;

	bool poseMode = false;


	// Use this for initialization
	void Awake()
	{
		
		giantess = GetComponent<Giantess>();

		anim = GetComponent<Animator>();		

		headT = anim.GetBoneTransform(HumanBodyBones.Head);
		Transform spine = anim.GetBoneTransform(HumanBodyBones.Spine);
		Transform leftEye = anim.GetBoneTransform(HumanBodyBones.LeftEye);
		Transform rightEye = anim.GetBoneTransform(HumanBodyBones.RightEye);

		rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
		leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);

		rightShoulder = anim.GetBoneTransform(HumanBodyBones.RightShoulder);
		

		hips = anim.GetBoneTransform(HumanBodyBones.Hips);

		

		// Full body biped IK settings
		BipedReferences references = new BipedReferences();
		BipedReferences.AutoDetectReferences(ref references, transform, BipedReferences.AutoDetectParams.Default);
		ik = gameObject.AddComponent<FullBodyBipedIK>();

		ik.solver.iterations = 3;
		ik.SetReferences(references, null);
		ik.solver.SetLimbOrientations(BipedLimbOrientations.UMA);

		SetDefaultValues();

		// HipSettings
		bodyEffector = new IKBone(ik.solver.bodyEffector);
		leftFootEffector = new IKBone(ik.solver.leftFootEffector);
		rightFootEffector = new IKBone(ik.solver.rightFootEffector);
		leftHandEffector = new IKBone(ik.solver.leftHandEffector);
		rightHandEffector = new IKBone(ik.solver.rightHandEffector);

		// Look At IK Settings
		lookAtIK = gameObject.AddComponent<LookAtIK>();
		lookAtIK.solver.SetChain(new [] {spine}, headT, new [] {leftEye, rightEye}, transform.GetChild(0));

		// Gorunder Settings
		if(useGrounder) {
			GameObject grounderGo = new GameObject("Grounder");
			grounderGo.transform.SetParent(transform, false);
			grounder = grounderGo.AddComponent<GrounderFBBIK>();
			grounder.ik = ik;
			grounder.solver.layers = Layers.gtsWalkableMask;
			grounder.solver.footRotationSpeed = 1f;
		}

		

		playerCamera = Camera.main.GetComponent<PlayerCamera>();

		// Interaction System
		head = new HeadIK(lookAtIK, giantess);
		

		
		
	}

	public void LookAtPoint(Vector3 point) {
		if(head == null) return;
		head.LookAtPoint(point);
	}

	void Start()
	{
		hand = new HandIK(ik, giantess);
		poseIK = new PoseIK(ik, lookAtIK, giantess);

		rightFootEffector.position = rightFoot.position;
		leftFootEffector.position = leftFoot.position;
		bodyEffector.position = hips.position;

		footEffect = GetComponent<FootEffect>();
		animManager = GetComponent<AnimationManager>();
		

		if(rightShoulder) {
			handBones = rightShoulder.GetComponentsInChildren<GiantessBone>();
			for (int i = 0; i < handBones.Length; i++)
			{
				handBones[i].canCrush = false;
			}
		}		
		

		string leftFootName = leftFoot.name;

		// test foot placement
		for(int i = 0; i < rightFoot.childCount; i++) {
			if(rightFoot.GetChild(i).name.StartsWith("Coll")) {
				rightFootCollider = rightFoot.GetChild(i).GetComponent<MeshCollider>();
			}
		}

		for(int i = 0; i < leftFoot.childCount; i++) {
			if(leftFoot.GetChild(i).name.StartsWith("Coll")) {
				leftFootCollider = leftFoot.GetChild(i).GetComponent<MeshCollider>();
			}
		}

		GameObject footCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		footCube.transform.SetParent(leftFootCollider.transform, false);
		leftFootCollider.gameObject.SetActive(true);
		// Debug.Log("Size: " + leftFootCollider.sharedMesh.bounds.size);
		footCube.transform.localScale = leftFootCollider.sharedMesh.bounds.size;
		footSize = leftFootCollider.sharedMesh.bounds.size;
		
		// Debug.Log("Foot offset: " + footHeightOffset);
		// Debug.Log("Min Mesh: " + leftFootCollider.sharedMesh.bounds.min);
		footCube.SetActive(false);
		
	}

	void MakeConvex(MeshCollider collider) {
		collider.inflateMesh = true;
		collider.convex = true;
	}

	void MakeConcave(MeshCollider collider) {
		collider.enabled = false;
		collider.inflateMesh = false;
		collider.convex = false;
		collider.enabled = true;
	}


	public void SetPoseMode(bool pose) {
		if(poseMode == pose) return;
		poseMode = pose;
		if(!pose) {
			SetPoseIK(false);
			SetDefaultValues();
		}
		
	}

	public void SetPoseIK(bool value) {
		if(value) {
			ik.enabled = false;
			lookAtIK.enabled = false;
			poseIK.ResetEffectors();
		} else {
			poseIK.SetWeight(0f);
		}
	}

	void SetDefaultValues() {
		ik.fixTransforms = false;
		ik.solver.headMapping.maintainRotationWeight = 0.6f;
		
		// Optimization
		//ik.solver.leftArmChain.reach = 0f;
		//ik.solver.rightArmChain.reach = 0f;
		
		// ik.solver.spineMapping.twistWeight = 0f;
		// ik.solver.spineStiffness = 0f;
		// ik.solver.pullBodyHorizontal = 0f;
		// ik.solver.pullBodyVertical = 0f;

		// Feet Settings
		ik.solver.rightLegMapping.maintainRotationWeight = 0.6f;
		ik.solver.rightLegChain.reach = 0.2f;

		ik.solver.leftLegMapping.maintainRotationWeight = 0.6f;
		ik.solver.leftLegChain.reach = 0.2f;

		if(head != null) head.SetDefaultValues();
	}

	void FixedUpdate() {
		if(poseMode) return;
		if(!luaIKEnabled) hand.Update();
		head.Update();
	}

	void Update() {
		if(poseMode) {
			return;
		} 

		gtsScale = transform.lossyScale.y;
		currentGtsSpeed = animManager.GetCurrentSpeed();

		ik.enabled = (hand.IsActive() || footState != FootStates.Idle || luaIKEnabled);		

		if(!target) {
			SetTarget(playerCamera.target);
			if(!target) return;
		}
		bothFootWeight = 0f;
		UpdateButt();
		
		UpdateFeet();
		
		if(useGrounder) {
			UpdateGrounder();
		}
	}

	void LateUpdate() {
		if(poseMode) return;
		UpdateBones();
	}

	void UpdateBones() {
		leftFootEffector.Update();
		rightFootEffector.Update();
		leftHandEffector.Update();
		rightHandEffector.Update();
		bodyEffector.Update();
	}

	public void SetTarget(Transform target)
	{
		if(target == null) {
			this.target = null;
		} else {
			this.target = target.GetComponent<EntityBase>();
			if(this.target == null) {
                Debug.Log("No target found.");
			}
		}
		
	}

	// ----------------- BUTT ---------------------------

	public void SetButtTarget(Vector3 target){
		sitPosition = target;
		sit = true;
	}

	public void CancelButtTarget(){
		cancelButt = true;
	}

	void UpdateButt() {
		ButtState nextButtState = buttState;
		float weight = 0f;
		float footWeight = 0f;
		Vector3 position = bodyEffector.position;
		float speed = 1f;
		// float sitRotation = 0f;
		// Debug.Log(transform.localRotation.eulerAngles.x);
		switch(buttState) {
			case ButtState.Idle:
				weight = 0f;				
				if(sit) {
					buttEffectorActive = true;
					nextButtState = ButtState.Sitting;
					bodyEffector.position = hips.position;
					position = hips.position;
					Debug.Log("Starting Rotation: " + transform.localRotation);
				}
				break;
			case ButtState.Sitting:
				weight = 1f;
				speed = 0.3f;
				footWeight = 1f;
				//sitRotation = -30f;
				position = sitPosition + Vector3.up * ButtOffset * gtsScale;
				if(cancelButt) {
					cancelButt = false;
					nextButtState = ButtState.Standing;
					sit = false;
					buttEffectorActive = false;
				}
				break;

			case ButtState.Standing:
				weight = 0f;
				if(bodyEffector.positionWeight < 0.01f) {
					nextButtState = ButtState.Idle;
				} else if(sit) {
					buttEffectorActive = true;
					nextButtState = ButtState.Sitting;
				}
				break;
		}
		bodyEffector.rotationWeight = Mathf.Lerp(bodyEffector.rotationWeight, weight, Time.deltaTime * speed * currentGtsSpeed);
		bodyEffector.positionWeight = Mathf.Lerp(bodyEffector.positionWeight, weight, Time.deltaTime * speed * currentGtsSpeed);
		bodyEffector.position = Vector3.Lerp(bodyEffector.position, position, Time.deltaTime * speed * currentGtsSpeed);

		//float newXRot = Mathf.Lerp(transform.localRotation.eulerAngles.x, sitRotation, Time.deltaTime * speed * 0.1f);
		//Debug.Log(newXRot);
		//transform.localRotation = Quaternion.Euler(newXRot, 0, 0);

		// this should be overwrite by proper foot behaviour
		SetFootWeight(footWeight);

		if(buttState != nextButtState) {
			Debug.Log("Next Butt State: " + nextButtState);
			buttState = nextButtState;
		}
		
		
	}

	public bool IsSit() {
		return buttState == ButtState.Sitting;
	}

	// ---------------- FEET -----------------
	// look feet when walking
	public void CrushTarget(EntityBase entity) {
		SetTarget(entity.transform);
		crushTarget = true;
		crushEnded = false;
	}

	void UpdateFeet()
	{
		if(luaIKEnabled) {
			footState = FootStates.Idle;
			return;
		}
		// update feet with previous weights

		FootStates nextFootState = footState;

		float weigthSpeed = 1f;
		float weight = 0f;
		// activeFoot = rightFootEffector;
		switch(footState) {
			case FootStates.Idle:
				leftFootEffector.position = leftFoot.position;
				rightFootEffector.position = rightFoot.position;
				if(crushTarget && animManager.TransitionEnded()) {					
					footTargetTransform = target.transform;
					if(IsReachableByFeet(footTargetTransform) && IsClose(footTargetTransform)) {
						crushTarget = false;
						// choose closest the foot
						float distanceToLeft = (leftFoot.position - target.transform.position).magnitude;
						float distanceToRight = (rightFoot.position - target.transform.position).magnitude;
						if(distanceToLeft < distanceToRight)  {
							activeFoot = leftFootEffector;
							activeFootCollider = leftFootCollider;
						}
						else  { 
							activeFoot = rightFootEffector;
							activeFootCollider = rightFootCollider;
						}
						Transform playerParent = GameController.playerInstance.transform.parent;
						GiantessBone gtsBone = null;
						if(playerParent != null ) gtsBone = playerParent.GetComponent<GiantessBone>();
						if(playerParent == null || gtsBone == null || gtsBone.giantess != giantess) {
							MakeConvex(activeFootCollider);
						}						
						startPreparationTime = Time.time;
						nextFootState = FootStates.Prepare;
					}					
				}				
				break;

			case FootStates.Prepare:
				weight = 1f;
				offsetPercentage = 1f;
				footTarget = footTargetTransform.position + (Vector3.up * maxOffset - transform.forward * footForwardDisplacement) * gtsScale;
				if(FeetTargetIsChild(footTargetTransform)) nextFootState = FootStates.Return; 
				//if(cancelFootCrush || !IsReachableByFeet(footTargetTransform) || !IsClose(footTargetTransform)) nextFootState = FootStates.Return;
				if(Time.time - startPreparationTime > minTimeToPrepare / currentGtsSpeed) { 
					nextFootState = FootStates.Crush;
				}
				break;

			case FootStates.Crush:
				weight = 1f;
				offsetPercentage = Mathf.Lerp(offsetPercentage, 0f, Time.deltaTime * crushSpeed * currentGtsSpeed);
				float offset = maxOffset * offsetPercentage;
				footTarget = footTargetTransform.position + (Vector3.up * offset - transform.forward * footForwardDisplacement) * gtsScale;
				
				RaycastHit hit;
				if(Physics.Raycast(activeFoot.position, Vector3.down, out hit, 1000f * gtsScale, Layers.gtsWalkableMask)){
					stayPosition = hit.point;
				}
				//if(cancelFootCrush) nextFootState = FootStates.Return;
				if(offsetPercentage < 0.15f) {
					footEffect.DoStep(stayPosition + transform.forward * 100f * gtsScale);
					startPreparationTime = Time.time;
					nextFootState = FootStates.Wait;					
					stayPosition = activeFoot.position;
									
				}
				break;

			case FootStates.Wait:
				weight = 1f;
				footTarget = stayPosition;
				if(Time.time - startPreparationTime > 1f || cancelFootCrush) {
					nextFootState = FootStates.Return;
				}
				break;

			case FootStates.Return:
				cancelFootCrush = false;
				weigthSpeed = returnSpeed;
				if (activeFoot.positionWeight < 0.05) {
					nextFootState = FootStates.Idle;
					MakeConcave(activeFootCollider);
					crushEnded = true;
				}
					
				break;

		}

		
		if(footState != FootStates.Idle) {		

			activeFoot.positionWeight = Mathf.Lerp(activeFoot.positionWeight, weight, Time.deltaTime * weigthSpeed * currentGtsSpeed);
			activeFoot.position = footTarget;
		}

		if(footState == FootStates.Idle || activeFoot != leftFootEffector )
			leftFootEffector.positionWeight = Mathf.Lerp(leftFootEffector.positionWeight, bothFootWeight, Time.deltaTime * currentGtsSpeed);
		if(footState == FootStates.Idle || activeFoot != rightFootEffector )
			rightFootEffector.positionWeight = Mathf.Lerp(rightFootEffector.positionWeight, bothFootWeight, Time.deltaTime * currentGtsSpeed);

		if(footState != nextFootState) {
			footState = nextFootState;
			// Debug.Log("Next State: " + nextFootState);
		}
	}

	public bool IsClose(Transform victim) {
		Vector3 distance = transform.position - (victim.position - transform.forward * footForwardDisplacement * gtsScale);
		distance.y = 0f;
		return distance.magnitude < maxDistanceFoot * 1600f * gtsScale;
	}

	public bool IsReachableByFeet(Transform victim)
	{
		return !FeetTargetIsChild(victim) && FeetTargetIsInRange(victim);
	}

	bool FeetTargetIsChild(Transform victim) {
		return victim.parent != null && victim.parent.gameObject.layer == Layers.gtsBodyLayer;
	}


	bool FeetTargetIsInRange(Transform victim) {
		float distance = Mathf.Abs(transform.InverseTransformPoint(victim.position).y);
		return distance < maxOffset;
	}

	void UpdateGrounder() {
		float weight = 1f;
		if(buttEffectorActive) weight = 0f;
		grounder.solver.heightOffset = giantess.offset * giantess.Height; // - footHeightOffset * gtsScale
		grounder.solver.maxStep = footSize.y * gtsScale * 2f;
		grounder.solver.footRadius = footSize.y * gtsScale;
		grounder.weight = Mathf.Lerp(grounder.weight, weight, Time.deltaTime * currentGtsSpeed);
	}

	void SetFootWeight(float weight) {
		if(weight > bothFootWeight) {
			bothFootWeight = weight;
		}
	}
}

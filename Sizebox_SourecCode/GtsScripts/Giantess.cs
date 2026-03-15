using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using MoonSharp.Interpreter;
using AI;

public class Giantess : Humanoid {

	// Sync Morph and BE multiplayer
	public static bool defaultAI = true;
	public static RuntimeAnimatorController giantessAnimatorController;

	public static float maxScale = 1000f;
	public static float minScale = 0.001f;
	public static bool ignorePlayer = false;

	public struct MorphData {
		public string name;
		public float weight;
	}
	SkinnedMeshRenderer meshRenderer;

	public bool blink = true;
	MMD4MecanimMorphHelper blinkController;
	private List<CreateMesh> staticColliderList;
	private List<MeshCollider> movableColliderList;
	GameObject colliderGTS;
	public GTSMovement gtsMovement;
	
	//jiggle physics
	JiggleBone leftBreast;
	JiggleBone rightBreast;
	
	public MorphData[] morphs {get; private set;}
	MMD4MecanimModel model;
	bool breastExpantionEnabled = false;
	BodyPhysics bodyPhysics;
	public float BESpeed;
	


	// Use this for initialization
	// This creates many morph helpers to set each one of the morph of this model
	public override void OnStartAuthority() {
		EditPlacement.Instance.OnGiantessSpawned(this);
	}

	protected override void Awake () {
		defaultAI = PreferencesCentral.aiDefault.value;
		isGiantess = true;
		base.Awake();

		bodyPhysics = gameObject.AddComponent<BodyPhysics>();

		// Add rigidbody before Humanoid Awake
		colliderGTS = new GameObject("Collider of " + gameObject.name);

		colliderGTS.transform.position = myTransform.position;
		colliderGTS.transform.rotation = myTransform.rotation;
		colliderGTS.transform.localScale = myTransform.localScale;

		gtsMovement = colliderGTS.AddComponent<GTSMovement>();
		gtsMovement.SetGiantess(this);

		baseScale = 1600f;
		// disable things that mess with animation
		model = gameObject.GetComponent<MMD4MecanimModel>();
		if(model != null) {
			model.pphEnabled = false;
			model.pphEnabledNoAnimation = false;
			if(blink) {
				blinkController = (MMD4MecanimMorphHelper) gameObject.AddComponent<MMD4MecanimMorphHelper>();
				blinkController.morphSpeed = 0.0f;
				blinkController.morphName = "笑い";
				blinkController.morphWeight = 1f;
				StartCoroutine("BlinkRoutine");
			}
		}

		movableColliderList = new List<MeshCollider>();
		SearchColliders(transform);
		SearchStaticColliders();

		// Create capsule collider
		// is outside the character
		
		UpdateCapsuleColliderTransform();

		// GTS Movement
		
		
		colliderGTS.AddComponent<GiantessObstacleDetector>();
		movement = colliderGTS.AddComponent<MovementCharacter>();
		movement.anim = GetComponent<AnimationManager>();
		movement.entity = this;

		


		// set the animator
		if(!giantessAnimatorController) {
			giantessAnimatorController = Resources.Load<RuntimeAnimatorController>("Animator/Controller/GTSAnimator");
			Debug.Assert(giantessAnimatorController, "Giantess Animator Controller not found");
		}


		Animator anim = GetComponent<Animator>();
		anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
		anim.runtimeAnimatorController = giantessAnimatorController;
		anim.SetFloat(Animator.StringToHash("animationSpeed"), GameController.globalSpeed);

		ik = GetComponent<GiantessIK>();
		Debug.Assert(ik, "body ik not found");

		gameObject.AddComponent<FootEffect>();

		
		gameObject.AddComponent<Voice>();

		// make physics ignore their own collision
		// capsule collider
		// she même	

	}


	void Update()
	{
		maxSize = maxScale;
		minSize = minScale;
	}

	public static List<string> GetAnimationList() {
		if(!giantessAnimatorController) {
			giantessAnimatorController = Resources.Load<RuntimeAnimatorController>("Animator/Controller/GTSAnimator");
			Debug.Assert(giantessAnimatorController, "Giantess Animator Controller not found");
		}
		int animationCount = giantessAnimatorController.animationClips.Length;
		List<string> animationList = new List<string>();
		for(int i = 0; i < animationCount; i++) {
			animationList.Add(giantessAnimatorController.animationClips[i].name);
		}
		return animationList;
	}

	void Start()
	{
		SearchMorphs();
		movement.anim.UpdateAnimationSpeed();
	}

	void SearchMorphs()
	{
		if(model != null) {
			morphs = new MorphData[model.morphList.Length];

			for(int i = 0; i < morphs.Length; i++)
			{
				morphs[i].name = model.morphList[i].morphData.nameEn;
				if(morphs[i].name.Length == 0) {
					string japanese = model.morphList[i].morphData.nameJp;
					morphs[i].name = TranslateJapanseMorph(japanese);
				}
					
				morphs[i].weight = 0f;
			}
		} else {
			SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
			foreach(SkinnedMeshRenderer renderer in renderers) {
				meshRenderer = renderer;
				Mesh mesh = renderer.sharedMesh;
				if(mesh.blendShapeCount == 0) continue;
				morphs = new MorphData[mesh.blendShapeCount];
				for(int i = 0; i < morphs.Length; i++) 
				{
					morphs[i].name = mesh.GetBlendShapeName(i);
					morphs[i].weight = 0f;
				}
				break;
			}
		}
		
	}

	public void SetMorphValue(int i, float weight)
	{
		if(model != null) 
		{
			model.morphList[i].weight = weight;
		} else {
			meshRenderer.SetBlendShapeWeight(i, weight * 100f);
		}
		morphs[i].weight = weight;
		
		
	}

	string TranslateJapanseMorph(string japanese)
	{
		JapaneseData data = Resources.Load<JapaneseData>("japanese");
		if(data.translation == null) {
			data.translation = new List<JapaneseData.MorphTranslation>();
		}

		string english = data.GetTranslation(japanese);
		if(english == "") {
			return japanese;
		}
		
		return english;
	}

	void SearchStaticColliders()
	{
		if (staticColliderList == null)
		{
			staticColliderList = new List<CreateMesh>();
			SkinnedMeshRenderer[] meshes = GetComponentsInChildren<SkinnedMeshRenderer>();
			foreach(SkinnedMeshRenderer mesh in meshes) {
				CreateMesh cm = mesh.gameObject.AddComponent<CreateMesh>();
				cm.gtsController = this;
				staticColliderList.Add(cm);
			}
		}
		
		

	}
	
	IEnumerator BlinkRoutine() {
		float timeBetweenBlinks = 3;
		float blinkDuration = 0.05f;
		blinkController.morphSpeed = blinkDuration;
		while(true) {			
			blinkController.morphWeight = 0;
			yield return new WaitForSeconds(timeBetweenBlinks);
			blinkController.morphWeight = 1;
			yield return new WaitForSeconds(blinkDuration);
		}

	}

	

	public void UpdateAllColliders()
	{
		if(poseMode)
		{
			// Debug.Log("ReUpdatingColliders");
			
			Vector3 previousScale  = transform.localScale;
			transform.localScale = Vector3.one;
			
			if(staticColliderList == null) SearchStaticColliders(); 

			foreach(CreateMesh collider in staticColliderList) {
				collider.UpdateCollider();
			}

			transform.localScale = previousScale;
		}
		
	}

	public override void SetCollider(bool enable)
	{
		// this control the state of the colliders if is moving or not to facilitate the program
		if(enable){
			colliderGTS.gameObject.SetActive(true);
			UpdateCapsuleColliderTransform();
		} else {
			colliderGTS.gameObject.SetActive(false);
		}
		if(!poseMode)
		{
			MovableCollidersEnable(enable);
		}
		else 
		{
			StaticCollidersEnable(enable);			
		}		
	}

	public override void SetPoseMode(bool pose) {
		base.SetPoseMode(pose);
		if(!pose)
		{
			StaticCollidersEnable(false);
		}
		MovableCollidersEnable(!pose);
		gtsMovement.EnableCollider(!pose);
		if(leftBreast != null) leftBreast.enabled = !pose;
		if(rightBreast != null) rightBreast.enabled = !pose;
	}

	public override List<Behavior> GetListBehaviors()
	{
		List<Behavior> behaviors = BehaviorLists.GetBehaviors(EntityType.Giantess);
		behaviors.AddRange(base.GetListBehaviors());
		return behaviors;
	}

	public override List<EntityType> GetTypesEntity() {
		List<EntityType> types = base.GetTypesEntity();
		types.Add(EntityType.Giantess);
		return types;
	}



	public void MovableCollidersEnable(bool option)
	{
		if(bodyPhysics && !bodyPhysics.ignore) {
			bodyPhysics.ColliderEnable(option);
		}
		
		for(int i = 0; i < movableColliderList.Count; i++) {
			movableColliderList[i].enabled = option;
		}
	}

	public void StaticCollidersEnable(bool option)
	{
		for(int i = 0; i < staticColliderList.Count; i++)
		{
			staticColliderList[i].DestroyMesh();
		}

	}

	// this are the moving colliders for animations
	public void SearchColliders(Transform childTransform) {
		int childs = childTransform.childCount;
		if(childTransform.GetComponent<SABoneColliderChild>()) {
			MeshCollider collider = childTransform.GetComponent<MeshCollider>();
			movableColliderList.Add(collider);
			collider.convex = false;

			childTransform.gameObject.AddComponent<GiantessBone>().Initialize(this);			
			childTransform.gameObject.layer = Layers.gtsBodyLayer;
			 
			if(!leftBreast)
			{
				if(childTransform.name.Contains("LeftBreast") || childTransform.name.Contains("hidarichichi"))
				{
					leftBreast = childTransform.parent.gameObject.AddComponent<JiggleBone>();
				}
			} 
			if(!rightBreast)
			{
				if(childTransform.name.Contains("RightBreast") || childTransform.name.Contains("migichichi"))
				{
					rightBreast = childTransform.parent.gameObject.AddComponent<JiggleBone>();
				}
			} 
		} else if(childs > 0) 
		{
			for(int i = 0; i < childs; i++) {
				SearchColliders(childTransform.GetChild(childs - i - 1)); // look last first to found breasts
			}
		}
			
	}

	DynamicBone SetBreast(Transform bone) {
		DynamicBone breast = gameObject.AddComponent<DynamicBone>();
		Transform parentBone = bone.parent;
		breast.m_Root = parentBone;
		breast.m_Damping = 0.2f;
		breast.m_Elasticity = 0.1f;
		breast.m_Stiffness = 0.8f;
		breast.m_Inert = 0.5f;
		breast.m_EndOffset = new Vector3(0, -0.1f, 0.3f);
		breast.m_DistanceToObject = 20000;
		List<Transform> exclusion = new List<Transform>();
		for(int i = 0; i < parentBone.childCount; i++) {
			Transform child = parentBone.GetChild(i);
			if (child != bone)
				exclusion.Add(child);
		}
		for(int i = 0; i < bone.childCount; i++) {
			exclusion.Add(bone.GetChild(i));
		}
		breast.m_Exclusions = exclusion;
		breast.m_EndLength = 1;
		//breast.m_Radius = 0.05f;
		//breast.m_RadiusDistrib = AnimationCurve.Linear(0, 1.01f, 1f, 0);
		return breast;

	}

	public void UpdateStaticCollider()
	{
		Invoke("UpdateAllColliders", 0.1f);
	}

	public void UpdateCapsuleColliderTransform()
	{
		colliderGTS.transform.position = CenterOrigin.VirtualToWorld(virtualPosition);
		colliderGTS.transform.rotation = transform.rotation;
		ScaleCapsuleCollider();

	}

	public void ScaleCapsuleCollider()
	{
		colliderGTS.transform.localScale = transform.lossyScale;

	}

	void OnDestroy()
	{
		// destroy collider
		Destroy(colliderGTS);
	}

	// overrided functions from parents

	public override void ChangeRotation(Vector3 newRotation) {
		base.ChangeRotation(newRotation);
		UpdateCapsuleColliderTransform();
	} 

	public override void ChangeScale(float newScale)
	{
		base.ChangeScale(newScale);
		ScaleCapsuleCollider();
		movement.anim.UpdateAnimationSpeed();

	}

	public override void DestroyObject(bool recursive = true)
	{
		bodyPhysics.Destroy();
		Destroy(colliderGTS);
		base.DestroyObject(recursive);
		ObjectManager.Instance.RemoveGiantess(id);
	}

	public void StartBreatExpantion() {
		if(breastExpantionEnabled) return;
		if(leftBreast != null && rightBreast != null)
			StartCoroutine(BreastGrowth());
		else Debug.LogError("No Breast Bones found in this model");
	}

	IEnumerator BreastGrowth() {
		
		Transform leftBreastTransform = leftBreast.transform;
		Transform rigthBreastTransform = rightBreast.transform;		
		float xOffset = Mathf.Abs(leftBreastTransform.position.y - rigthBreastTransform.position.y) * 10f / (transform.lossyScale.y);

		breastExpantionEnabled = true;
		while(breastExpantionEnabled) {
			float scaleModifier = BESpeed * Time.deltaTime;

			leftBreastTransform.localScale *= 1f + scaleModifier;
			leftBreastTransform.localPosition -= Vector3.right * xOffset * scaleModifier;
			leftBreastTransform.localPosition -= Vector3.up * 10f * scaleModifier;

			rigthBreastTransform.localScale *= 1f + scaleModifier;
			rigthBreastTransform.localPosition += Vector3.right * xOffset * scaleModifier;
			rigthBreastTransform.localPosition -= Vector3.up * 10f * scaleModifier;
			yield return null;
		}
	}

	

	
}

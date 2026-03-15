using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPhysics : MonoBehaviour {
	public bool ignore = false;

	Transform collidersParent;
	MMD4MecanimModel model;
	Animator anim;
	List<ColliderPair> colliders;

	public class ColliderPair {
		public Transform transform;
		public Collider collider;
		public Rigidbody rigidbody;

		public ColliderPair(Transform transform, Collider collider) {
			this.transform = transform;
			this.collider = collider;
			rigidbody = collider.gameObject.GetComponent<Rigidbody>();
			rigidbody.useGravity = false;
		}

		public void Update() {
			rigidbody.MovePosition(transform.position);
		}
	}

	// Use this for initialization
	void Start () {	
		anim = GetComponent<Animator>();
		colliders = new List<ColliderPair>();
		AddDestructionColliders();

		model = GetComponent<MMD4MecanimModel>();
		if(model == null) return;


		//Debug.Log("RigidbodyCount: " + model.rigidBodyList.Length);

		//foreach(MMD4MecanimModel.RigidBody rbody in model.rigidBodyList) {
		//	Debug.Log(rbody.rigidBodyData.nameJp);
		//}

		
		Transform head = anim.GetBoneTransform(HumanBodyBones.Head);
		DynamicBone hair = gameObject.AddComponent<DynamicBone>();
		Transform parentBone = head;
		hair.m_Root = parentBone;
		hair.m_Damping = 0.2f;
		hair.m_Elasticity = 0.05f;
		hair.m_Stiffness = 0.7f;
		hair.m_Inert = 0.5f;
		hair.m_Gravity = new Vector3(0, -1f, 0);
		hair.m_EndLength = 1;
		// hair.m_EndOffset = new Vector3(0, -100f, 0);
		hair.m_DistanceToObject = 20000;
		List<Transform> exclusion = new List<Transform>();
		for(int i = 0; i < parentBone.childCount; i++) {
			Transform child = parentBone.GetChild(i);
			if(IsHairBone(child.name)) {
				Collider[] colliders = child.GetComponentsInChildren<Collider>();
				for(int j = 0; j < colliders.Length; j++) {
					exclusion.Add(colliders[j].transform);
				}
			}
				
			else {
				exclusion.Add(child);
			}
		}
		hair.m_Exclusions = exclusion;
	}

	void FixedUpdate() {
		UpdateColliderPositions();
	}

	void UpdateColliderPositions() {
		foreach(ColliderPair collider in colliders) {
			collider.Update();
		}
	}

	bool IsHairBone(string name) {
		string[] terms = new string[] { "Hair", "hair", "ponite", "Palpus", "osage", "shippo", "Burns"}; 
		for(int i = 0; i < terms.Length; i++) {
			if(name.Contains(terms[i])) return true;
		}
		return false;
	}


	

	public void ColliderEnable(bool enabled) {
		if(colliders == null) return;
		foreach(ColliderPair collider in colliders) {
			collider.collider.enabled = enabled;
		}
	}	

	public void Destroy() {
		if(!collidersParent) return;
		EntityBase[] children = collidersParent.GetComponentsInChildren<EntityBase>();
		for(int i = 0; i < children.Length; i++) {
			children[i].transform.SetParent(null);
		}
		for(int i = 0; i < children.Length; i++) {
			children[i].DestroyObject(false);
		}
		Destroy(collidersParent.gameObject);
	}

	void AddDestructionColliders() {
		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.LeftFoot));
		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.RightFoot));

		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.Spine));
		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.Head));

		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.Hips));

		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.LeftLowerArm));
		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.RightLowerArm));

		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.RightLowerLeg));

		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.LeftHand));
		AddSimpleCollider(anim.GetBoneTransform(HumanBodyBones.RightHand));
	}

	void AddSimpleCollider(Transform bone) {
		SphereCollider collider = new GameObject(bone.name + " collider").AddComponent<SphereCollider>();
		collider.transform.SetParent(transform);
		collider.transform.localScale = Vector3.one;
		collider.radius = 50f;
		collider.gameObject.AddComponent<Rigidbody>();
		collider.gameObject.AddComponent<Destructor>().baseSize = 1000f;
		collider.gameObject.layer = Layers.destroyerLayer;
		colliders.Add(new ColliderPair(bone, collider));
	}
}

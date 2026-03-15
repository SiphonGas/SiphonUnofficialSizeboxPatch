using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootEffect : MonoBehaviour {
	Giantess giantess;
	static GameObject dustPrefab;
	Animator animat;
	AudioSource audioSource;
	float scale;
	public float destructionRadius = 100f;
	Transform leftFoot;
	Collider[] leftColliders;
	Transform rightFoot;
	Collider[] rightColliders;
	Vector3 epicenter;
	GameObject thisDust;
	ParticleSystem dustEmitter;
	ParticleSystem.MainModule mainModule;
	ParticleSystem.ShapeModule shape;

	void Update() {
		FixColliders();
	}

	void FixColliders() {
		
	}


	void Start() {
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.spatialBlend = 1f;
		audioSource.dopplerLevel = 0f;

		animat = gameObject.GetComponent<Animator>();
		leftFoot = animat.GetBoneTransform(HumanBodyBones.LeftFoot);
		leftColliders = leftFoot.GetComponentsInChildren<Collider>();
		rightFoot = animat.GetBoneTransform(HumanBodyBones.RightFoot);
		rightColliders = rightFoot.GetComponentsInChildren<Collider>();

		if(dustPrefab == null) dustPrefab = Resources.Load<GameObject>("Particles/StepDust");

		giantess = GetComponent<Giantess>();

	}

	// Use this for initialization
	public void OnStep(int feet)
	{
		scale = transform.lossyScale.y;

		if(feet == 0) epicenter = leftFoot.position;
		else epicenter = rightFoot.position;
		epicenter += transform.forward * destructionRadius * scale / 2;

		RaycastHit hit;
		if(Physics.Raycast(epicenter, - Vector3.up, out hit, 200f * scale, Layers.gtsCollisionCheckMask)) {
			epicenter = hit.point;
		}

		DoStep(epicenter);
			
	}

	public void DoStep(Vector3 epicenter) {
		scale = transform.lossyScale.y;
		this.epicenter = epicenter;

		GameController.Instance.eventManager.SendEvent(new StepEvent(giantess, epicenter, audioSource));

		VisualEffect();
		DestructionEffect();
	}

	void VisualEffect() {
		if(!GameController.IsMacroMap) return;
		if(thisDust == null) {
			thisDust = Instantiate(dustPrefab, epicenter, Quaternion.identity);
			dustEmitter = thisDust.transform.GetChild(0).GetComponent<ParticleSystem>();
			mainModule = dustEmitter.main;
			shape = dustEmitter.shape;
		}
		thisDust.transform.position = epicenter;
		
		mainModule.startSize = scale * 100f;
		shape.radius = scale * 100f;
		dustEmitter.Play();
	}


	void DestructionEffect() {
		Collider[] buildings = Physics.OverlapSphere(epicenter, destructionRadius * scale, Layers.buildingMask);
		foreach(Collider n in buildings) {
			IDestructible destructible = n.GetComponentInParent<IDestructible>();
			if(destructible != null) destructible.Destroy(epicenter, scale * 1000f);
				
		}
	}
}


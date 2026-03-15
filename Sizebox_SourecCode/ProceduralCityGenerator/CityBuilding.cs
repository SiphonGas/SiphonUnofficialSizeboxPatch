using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityBuilding : MonoBehaviour, IDestructible {

	static GameObject debrisPrefab;
	static AudioSource[] audioSource;
	static int sourcesNumber = 5;
	static float minimunDistanceBetweenSounds = 1f;
	static float timeLastSound = 0.5f;
	static int sourceToUse = 0;
	static GameObject smokeParticles;
	static List<ParticleData> smokeParticleList;
	static ParticleData smokeData;

	public int xSize = 1;
	public int zSize = 1;
	float buildingScale;
	Collider thisCollider;
	float maxRotation = 30;

	float fallAcceleration = 5f;
	bool falling = false;

	

	// Use this for initialization
	void Start () {		
		thisCollider = GetComponentInChildren<Collider>();

		if(debrisPrefab == null) debrisPrefab = Resources.Load<GameObject>("City/Debris");
		if(smokeParticles == null || smokeData.main.customSimulationSpace == null) {
			smokeParticles = Instantiate(Resources.Load<GameObject>("Particles/SmokeParticles"));
			smokeData = new ParticleData(smokeParticles);
			smokeData.main.customSimulationSpace = transform.parent.parent;
		}
		if(smokeParticleList == null) smokeParticleList = new List<ParticleData>();

		buildingScale = transform.lossyScale.y;
		
	}

	IEnumerator FallAnimation(float power) {
		

		falling = true;
		thisCollider.enabled = false;

		Vector3 originalPosition = transform.localPosition;
		
		Quaternion initialRotation = transform.localRotation;
		Quaternion targetRotation = Quaternion.Euler(Random.Range(0f, maxRotation), transform.localEulerAngles.y, Random.Range(0f, maxRotation));
		GameObject debris = Instantiate(debrisPrefab);
		debris.transform.SetParent(transform.parent, false);
		debris.transform.localPosition = originalPosition;
		debris.transform.localScale *= 0.5f;
		debris.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
		Vector3 initialScale = debris.transform.localScale;
		float finalScale = 1f + Random.value;		

		float fallSpeed = 0f;
		float time = 0.1f;
		float endTime = 6f;

		PlayDestructionSound(power);

		while(time < endTime) {
			fallSpeed = 0.5f * fallAcceleration * time * time;
			transform.localPosition -= Vector3.up * fallSpeed * Time.deltaTime;
			debris.transform.localScale = initialScale * finalScale * time / endTime;
			transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, 0.5f * Time.deltaTime);
			time += Time.deltaTime;
			yield return null;
		}
		

		

		Destroy(gameObject);
	}

	public void IgnoreCollision(Collider externalCollider) {
		Collider buildingCollider = GetComponentInChildren<Collider>();
		Physics.IgnoreCollision(buildingCollider, externalCollider);
	}

	void PlayDestructionSound(float power) {
		float currentTime = Time.time;
		if(currentTime < timeLastSound + minimunDistanceBetweenSounds) return;
		timeLastSound = currentTime;

		if(power < 400) power = 400;

		SmokeEffect(power);
		

		if(audioSource == null || audioSource[sourceToUse] == null) {
			audioSource = new AudioSource[sourcesNumber];
			for(int i = 0; i < sourcesNumber; i++) {
				audioSource[i] = new GameObject("City Audio").AddComponent<AudioSource>();
				audioSource[i].transform.SetParent(transform.parent);
				audioSource[i].dopplerLevel = 0f;
				audioSource[i].spatialBlend = 1f;
				audioSource[i].minDistance = 50f;
				audioSource[i].maxDistance = 5000f;
			}
			
		}

		
		if(!audioSource[sourceToUse].isPlaying) {
			audioSource[sourceToUse].transform.localPosition = transform.localPosition;
			audioSource[sourceToUse].PlayOneShot(SoundManager.This.GetDestructionSound());
		}
			
		sourceToUse++;
		if(sourceToUse >= sourcesNumber) sourceToUse = 0;
	}

	public void Destroy(Vector3 contatPoint, float scale) {
		if(falling) return;
		if(scale < 10f * buildingScale) return;
		StartCoroutine(FallAnimation(scale));
	}

	void SmokeEffect(float power) {
		smokeData.particleRoot.position = transform.position;
		
		smokeData.main.startSize = power * buildingScale / 5f;
		smokeData.shape.radius = power * buildingScale / 10f;
		smokeData.system.Play();
	}
}

public class ParticleData {
	public Transform particleRoot;
	public ParticleSystem system;
	public ParticleSystem.MainModule main;
	public ParticleSystem.ShapeModule shape;

	public ParticleData(GameObject root) {
		particleRoot = root.transform;
		system = root.transform.GetChild(0).GetComponent<ParticleSystem>();
		main = system.main;
		shape = system.shape;
	}
}

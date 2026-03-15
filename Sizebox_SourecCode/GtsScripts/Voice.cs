using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voice : MonoBehaviour {
	Animator anim;
	// Audio Sources
	public AudioSource mouthSource;
	AudioSource chestSource;

	// Clips
	static AudioClip heartbeat;
	static AudioClip breathing;

	// Settings
	public float minDistancePercentage = 0.01f;
	public float chestDistance = 100f;
	public float mouthDistance = 120f;
	public float pitch = 1f;
	float scale;

	// Use this for initialization
	void Start () {
		anim = gameObject.GetComponent<Animator>();

		Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest);
		if(chest) {
			if(!heartbeat) heartbeat = Resources.Load<AudioClip>("Sound/Giantess/heartbeat");
			chestSource = CreateAudioSource(chest);			
			chestSource.clip = heartbeat;			
			chestSource.Play();
		}

		Transform head = anim.GetBoneTransform(HumanBodyBones.Head);
		if(head) {
			if(!breathing) breathing = Resources.Load<AudioClip>("Sound/Giantess/female_breathing");
			mouthSource = CreateAudioSource(head);
			mouthSource.clip = breathing;
			mouthSource.Play();
		}
		
	}
	
	// Update is called once per frame
	void Update () {
		scale = transform.localScale.y;	

		if(chestSource) {			
			chestSource.maxDistance = chestDistance * scale;
			chestSource.minDistance = chestSource.maxDistance * minDistancePercentage;
			chestSource.pitch = pitch;
		}

		if(mouthSource) {			
			mouthSource.maxDistance = mouthDistance * scale;
			mouthSource.minDistance = mouthSource.maxDistance * minDistancePercentage;
			mouthSource.pitch = pitch;
			if(SoundManager.femaleVoices) mouthSource.volume = 1f;
			else mouthSource.volume = 0f;
		}
		
	}

	AudioSource CreateAudioSource(Transform bone) {
		AudioSource newAudioSource = bone.gameObject.AddComponent<AudioSource>();
		newAudioSource.dopplerLevel = 0f;
		newAudioSource.spatialBlend = 1f;
		newAudioSource.volume = 1f;
		newAudioSource.loop = true;
		return newAudioSource;
	}
}

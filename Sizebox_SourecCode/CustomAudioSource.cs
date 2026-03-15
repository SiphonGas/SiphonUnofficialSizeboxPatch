using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomAudioSource : MonoBehaviour {
	public AudioSource audioSource {get; private set;}

	public float maxDistance;
	public float minDistance;
	Transform _transform;
	EntityBase entity;
	
	void Awake() {
		_transform = transform;
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
		audioSource.dopplerLevel = 0f;
		audioSource.rolloffMode = UnityEngine.AudioRolloffMode.Linear;
		FindEntity();

		maxDistance = 5;
		audioSource.maxDistance = audioSource.maxDistance * GetScale();
		
		audioSource.minDistance = 0;
		minDistance = audioSource.minDistance;



		float scale;
		if(entity != null) scale = entity.AccurateScale;
		else scale = transform.lossyScale.y;
		
	}

	void Update() {
		float scale = GetScale();
		audioSource.maxDistance = maxDistance * scale;
		audioSource.minDistance = minDistance * scale;
	}

	float GetScale() {
		if(entity == null) return _transform.lossyScale.y;
		else return entity.AccurateScale;
	}

	public void Play() {
		StartCoroutine(PlayRoutine());
	}

	IEnumerator PlayRoutine() {
		while(!audioSource.clip.isReadyToPlay) yield return null;
		audioSource.Play();

	}

	public void PlayDelayed(float delay) {
		StartCoroutine(PlayDelayedRoutine(delay));
	}

	IEnumerator PlayDelayedRoutine(float delay) {
		while(!audioSource.clip.isReadyToPlay) yield return null;
		audioSource.PlayDelayed(delay);
	}

	public void PlayOneShot(AudioClip clip, float volumeScale) {
		StartCoroutine(PlayOneShotRoutine(clip, volumeScale));
	}

	IEnumerator PlayOneShotRoutine(AudioClip clip, float volumeScale) {
		while(!clip.isReadyToPlay) yield return null;
		audioSource.PlayOneShot(clip, volumeScale);
	}

	public void PlayOneShot(AudioClip clip) {
		StartCoroutine(PlayOneShotRoutine(clip));
	}

	IEnumerator PlayOneShotRoutine(AudioClip clip) {
		while(!clip.isReadyToPlay) yield return null;
		audioSource.PlayOneShot(clip);
	}
	
	void FindEntity() {
		Transform currentTransform = _transform;
		while(entity == null && currentTransform != null)
		{
			entity = currentTransform.GetComponent<EntityBase>();
			currentTransform = currentTransform.parent;
		}
	}
}

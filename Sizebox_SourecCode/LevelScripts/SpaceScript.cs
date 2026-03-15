using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceScript : MonoBehaviour {
	AudioSource audioSource;
	AudioClip[] spaceSounds;

	void Start()
	{
		spaceSounds = Resources.LoadAll<AudioClip>("Sound/Space");
		audioSource = gameObject.AddComponent<AudioSource>();
		SoundManager.SetUpAmbientAudioSource(audioSource);
		
	}

	void Update()
	{
		audioSource.volume = SoundManager.ambientVolume * 0.5f;
		if(!audioSource.isPlaying) {
			audioSource.clip = SoundManager.GetRandomClip(spaceSounds);
			audioSource.Play();
		}
	}

}

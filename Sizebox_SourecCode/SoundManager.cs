using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour, IListener {
	PlayerCamera cam;
	public static float ambientVolume = 0.5f;
	public static float vehiclesVolume = 0.5f;
	public static bool femaleVoices = true;
	public static SoundManager This;
	public AudioClip[] giantSteps;
	AudioClip[] gigaSteps;
	public AudioClip[] playerSteps;
	public AudioClip[] crushedSound;
	public AudioClip flySound;
	public AudioClip[] destructionSounds;
	public AudioClip[] citySounds;
	public AudioClip natureSound;
	public AudioClip seaSound;
	public AudioClip windSound;
	AudioSource tinyAudioSource;
	public float ambientLevel = 1f;
	public float currentAmbientVolume { get { return ambientVolume * ambientLevel;}}
	
	int lastStep = 0;

	public void OnNotify(IEvent e){
		StepEvent se = (StepEvent) e;
		AudioSource audioSource = se.audio;
		float scale = se.gts.Scale;

		audioSource.minDistance = 200f * scale;
		audioSource.maxDistance = 10000 * scale;

		float playerHeight;
		if(cam.entity != null && cam.entity.isGiantess) {
			playerHeight = 1.6f;
		} else {
			playerHeight = GameController.playerInstance.Height;
		}

		float relativeScale = se.gts.Height / playerHeight;
		float logScale = Mathf.Log10(relativeScale);
		// Debug.Log(relativeScale + " and log: " + logScale);

		float th1 = 1.68f;
		float maxTh = 6f;
		

		AudioClip sound = null;
		// Choose sound
		if(logScale > th1) {
			sound = GetRandomClip(gigaSteps);
		} else {
			sound = GetRandomClip(giantSteps);
		}


		// Volume Adjustement
		if(logScale < 0f) se.audio.volume = 0f;
		else if(logScale < th1) se.audio.volume = logScale / th1;
		else se.audio.volume = 1f;
		
		
		// adjust pitch
		float minPitch = 0.6f;
		float minTh2 = 0.3f;
		float pitch = 1f;
		if(logScale < 0f) {
			pitch = Mathf.Clamp(1f + (1f - logScale), 1f, 3f);
		} else if (logScale < th1) {
			pitch = LinearPitchFall(minPitch, 0f, th1, logScale);
		} else if(logScale < maxTh) {
			pitch = LinearPitchFall(minTh2, th1, maxTh, logScale);
		} else {
			pitch = minTh2;
		}		
		float pitchBias = 0.2f * pitch;
		audioSource.pitch = pitch + Random.Range(-pitchBias, pitchBias);

		audioSource.PlayOneShot(sound);

	}

	public float LinearPitchFall(float minPitch, float floor, float ceil, float scale) {
		return minPitch + (1f - minPitch) * (1f - (scale - floor) / (ceil - floor));
	}

	void Awake()
	{
		ambientVolume = PreferencesCentral.ambianceVolume.value;
		femaleVoices = PreferencesCentral.femaleSounds.value;

		natureSound = Resources.Load<AudioClip>("Sound/nature2");
		seaSound = Resources.Load<AudioClip>("Sound/sea2");
		windSound = Resources.Load<AudioClip>("Sound/storm");
		flySound = Resources.Load<AudioClip>("Sound/Player/Fly3");
		gigaSteps = Resources.LoadAll<AudioClip>("Sound/Footstep/Giga/");
		destructionSounds = Resources.LoadAll<AudioClip>("Sound/Destruction/");
		citySounds = Resources.LoadAll<AudioClip>("Sound/City");
		playerSteps = Resources.LoadAll<AudioClip>("Sound/Footstep/Player");
		giantSteps = Resources.LoadAll<AudioClip>("Sound/Footstep/Giant");
		crushedSound = Resources.LoadAll<AudioClip>("Sound/Crushed");

		if(!This)
			This = this;
		tinyAudioSource = new GameObject("Audio Source").AddComponent<AudioSource>();
		tinyAudioSource.spatialBlend = 1f;
		tinyAudioSource.dopplerLevel = 0f;
		tinyAudioSource.minDistance = 10f;
		tinyAudioSource.maxDistance = 100f;

		GameController.Instance.eventManager.RegisterListener(this, Interest.OnStep);
		cam = Camera.main.GetComponent<PlayerCamera>();
		
	}

	public static void SetSoundClip(AudioSource source, AudioClip clip) {
		if(source.clip != clip) {
			// Debug.Log("changing to " + clip.name);
			source.clip = clip;
			source.Play();
		}
		if(!source.isPlaying) source.Play();
	}

	public AudioClip GetDestructionSound()
	{
		int cantSounds = destructionSounds.Length;
		return destructionSounds[Random.Range(0,cantSounds)];		
	}

	public AudioClip GetCitySound()
	{
		int cantSounds = citySounds.Length;
		return citySounds[Random.Range(0,cantSounds)];		
	}

	public AudioClip GetPlayerFootstepSound()
	{
		lastStep++;
		if(lastStep >= playerSteps.Length) lastStep = 0;
		return playerSteps[lastStep];
	}

	public void PlayCrushed(Vector3 position, float size)
	{
		tinyAudioSource.minDistance = 1f * size;
		tinyAudioSource.maxDistance = 20f * size;
		tinyAudioSource.transform.position = position;
		int cantSounds = crushedSound.Length;
		tinyAudioSource.PlayOneShot(crushedSound[Random.Range(0,cantSounds)]);
	}

	public void RegisterAmbientSound(AudioSource source, int priority) {

	}

	public struct AmbientSource
	{
		public AudioSource source;
		public float priority;
	}

	public static void SetUpAmbientAudioSource(AudioSource source) {
		source.loop = true;
		source.dopplerLevel = 0f;
	}

	public static AudioClip GetRandomClip(AudioClip[] clips) {
		return clips[Random.Range(0,clips.Length)];	
	}
}

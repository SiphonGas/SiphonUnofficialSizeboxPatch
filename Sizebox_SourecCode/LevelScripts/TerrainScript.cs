using UnityEngine;

public class TerrainScript : MonoBehaviour {
	AudioSource ambientAudioSource;
	Camera mainCamera;
	float windLevel = 1000f;
	Material skybox;
	Transform cameraTransform;

	float seaLevel = 205f;

	// Use this for initialization
	void Start () {
		mainCamera = Camera.main;
		cameraTransform = mainCamera.transform;
		skybox = Resources.Load<Material>("Shaders/IslandSky");

		ambientAudioSource = gameObject.AddComponent<AudioSource>();
		ambientAudioSource.clip = SoundManager.This.natureSound;
		ambientAudioSource.loop = true;
		ambientAudioSource.volume = SoundManager.This.currentAmbientVolume;
		ambientAudioSource.dopplerLevel = 0f;
		ambientAudioSource.Play();
	}

	void Update()
	{
		UpdateSounds();
	}

	

	void UpdateSounds() 
	{
		float thickness = 1 - (CenterOrigin.WorldToVirtual(cameraTransform.position).y / 50000);
		thickness = Mathf.Clamp(thickness, 0.5f, 1f);
		skybox.SetFloat("_AtmosphereThickness", thickness);

		float ambientVolume = SoundManager.This.currentAmbientVolume;
		RaycastHit hit;
		Vector3 cameraPosition = cameraTransform.position;

		bool hasHit = Physics.Raycast(cameraTransform.position, Vector3.down, out hit, 10000f, Layers.mapMask);
		if(hasHit) {
			float floorLevel = CenterOrigin.WorldToVirtual(hit.point).y;
			float distanceToFloor = hit.distance;
			if(floorLevel < seaLevel) {
				floorLevel = seaLevel;
				float camHeight = CenterOrigin.WorldToVirtual(cameraPosition).y;
				if(camHeight > seaLevel) {
					distanceToFloor = camHeight - seaLevel;
				} else {
					distanceToFloor = 0f;
				}
			} 
			float distanceToZero = distanceToFloor + floorLevel;

			

			float distanceToZeroMult = 1 - Mathf.Clamp(distanceToZero / windLevel, 0f, 1f);
			float distanceToFloorMult = 1 - Mathf.Clamp(distanceToFloor / 100f, 0f, 1f);
			float volume = ambientVolume * distanceToZeroMult * distanceToFloorMult;
			if(volume < 0.01f) {
				float minLimit = Mathf.Min(windLevel, floorLevel + 100f);
				volume = ambientVolume * Mathf.Clamp01((cameraPosition.y - minLimit) / 500f);
				SetAmbientClip(SoundManager.This.windSound);
			} else {
				if(floorLevel == seaLevel) SetAmbientClip(SoundManager.This.seaSound);
				else SetAmbientClip(SoundManager.This.natureSound);
			}
			ambientAudioSource.volume = volume;
		} 
	}

	void SetAmbientClip(AudioClip clip)
	{
		SoundManager.SetSoundClip(ambientAudioSource, clip);
	}
	
	
}

using MoonSharp.Interpreter;

namespace Lua 
{
	/// <summary>
    /// A representation of audio sources in 3D.
    /// </summary>
    /// You can play a single audio clip using Play, Pause and Stop. You can also adjust its volume while playing using the volume property, or seek using time. Multiple sounds can be played on one AudioSource using PlayOneShot. You can play a clip at a static position in 3D space using PlayClipAtPoint.
	
	[MoonSharpUserDataAttribute]
	public class AudioSource
	{
		CustomAudioSource customSource;
		static IOManager ioManager;

		[MoonSharpHiddenAttribute]
		public AudioSource(Transform transform) {
			if(transform == null) UnityEngine.Debug.Log("No transform.");
			customSource = transform. _tf.GetComponent<CustomAudioSource>();
			if(customSource == null) customSource = transform._tf.gameObject.AddComponent<CustomAudioSource>();
			if(ioManager == null) ioManager = IOManager.GetIOManager();
		} 

		/// <summary>
        /// Create a new audiosource on a Entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
		public static AudioSource New(Entity entity) {
			return new AudioSource(entity.transform);
		}

		/// <summary>
        /// Create a new audiosource on a transform
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
		public static AudioSource New(Transform transform) {
			return new AudioSource(transform);
		}

		
		/// <summary>
        /// MaxDistance is the distance where the sound is completely inaudible.
        /// </summary>
        /// <returns></returns>
		public float maxDistance {
			get { return customSource.maxDistance; }
			set { customSource.maxDistance = value; }
		}

		/// <summary>
        /// Within the Min distance the AudioSource will cease to grow louder in volume.
        /// </summary>
		/// Outside the min distance the volume starts to attenuate.
        /// <returns></returns>
		public float minDistance {
			get { return customSource.minDistance; }
			set { customSource.minDistance = value; }
		}


		/// <summary>
        /// The default AudioClip to play.
        /// </summary>
        /// <returns></returns>
		public string clip {
			get { return customSource.audioSource.clip.name; }
			set { customSource.audioSource.clip = GetClip(value); }
		}

		static UnityEngine.AudioClip GetClip(string clipName) {
			return ioManager.LoadAudioClip(clipName);
		}

		/// <summary>
        /// Is the clip playing right now (Read Only)?
        /// </summary>
        /// <returns></returns>
		public bool isPlaying {
			get { return customSource.audioSource.isPlaying; }
		}

		/// <summary>
        /// Is the audio clip looping?
        /// </summary>
		/// If you disable looping on a playing AudioSource the sound will stop after the end of the current loop.
        /// <returns></returns>
		public bool loop {
			get { return customSource.audioSource.loop; }
			set { customSource.audioSource.loop = value; }
		}

		/// <summary>
        /// Un- / Mutes the AudioSource. Mute sets the volume=0, Un-Mute restore the original volume.
        /// </summary>
        /// <returns></returns>
		public bool mute {
			get { return customSource.audioSource.mute; }
			set { customSource.audioSource.mute = value; }
		}

		/// <summary>
        /// The pitch of the audio source.
        /// </summary>
        /// <returns></returns>
		public float pitch {
			get { return customSource.audioSource.pitch; }
			set { customSource.audioSource.pitch = value; }
		}

		/// <summary>
        /// Sets how much this AudioSource is affected by 3D spatialisation calculations (attenuation, doppler etc). 0.0 makes the sound full 2D, 1.0 makes it full 3D.
        /// </summary>
		/// Aside from determining if this AudioSource is heard as a 2D or 3D source, this property is useful to morph between the two modes.
		/// 3D spatial calculations are applied after stereo panning is determined and can be used in conjunction with panStereo.
		/// Morphing between the 2 modes is useful for sounds that should be progressively heard as normal 2D sounds the closer they are to the listener.
        /// <returns></returns>
		public float spatialBlend {
			get { return customSource.audioSource.spatialBlend; }
			set { customSource.audioSource.spatialBlend = value; }
		}

		/// <summary>
        /// The volume of the audio source (0.0 to 1.0).
        /// </summary>
        /// <returns></returns>
		public float volume {
			get { return customSource.audioSource.volume; }
			set { customSource.audioSource.volume = value; }
		}

		/// <summary>
        /// Pauses playing the clip.
        /// </summary> 
        /// See Also: Play, Stop functions.
		public void Pause() {
			customSource.audioSource.Pause();
		}

		/// <summary>
        /// Plays the clip.
        /// </summary>
		public void Play() {
			customSource.Play();
		}

		/// <summary>
        /// Plays the clip with a delay specified in seconds.
        /// </summary>
        /// <param name="delay"></param>
		public void PlayDelayed(float delay) {
			customSource.PlayDelayed(delay);
		}

		/// <summary>
        /// Plays an AudioClip, and scales the AudioSource volume by volumeScale.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="volumeScale"></param>
		public void PlayOneShot(string clip, float volumeScale) {
			customSource.PlayOneShot(GetClip(clip), volumeScale);
		}

		/// <summary>
        /// Plays an AudioClip.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="volumeScale"></param>
		public void PlayOneShot(string clip) {
			customSource.PlayOneShot(GetClip(clip)); 
		}

		/// <summary>
        /// Stops playing the clip.
        /// </summary>
		public void Stop() {
			customSource.audioSource.Stop();
		}


		/// <summary>
        /// Unpause the paused playback of this AudioSource.
        /// </summary>
		/// This function is similar to calling Play () on a paused AudioSource, except that it will not create a new playback voice if it is not currently paused.
		/// This is also useful if you have paused one-shots and want to resume playback without creating a new playback voice for the attached AudioClip.
		public void UnPause() {
			customSource.audioSource.UnPause();
		}

		/// <summary>
        /// Plays an AudioClip at a given position in world space.
        /// </summary>
		/// This function creates an audio source but automatically disposes of it once the clip has finished playing.
        /// <param name="clip">Audio data to play.</param>
        /// <param name="position">Position in world space from which sound originates.</param>
        /// <param name="volume">Playback volume.</param>
		public static void PlayClipAtPoint(string clip, Vector3 position, float volume) {
			UnityEngine.AudioSource.PlayClipAtPoint(GetClip(clip), position.vector3, volume);
		}






	}

	
}

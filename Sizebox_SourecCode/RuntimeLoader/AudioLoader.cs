using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioLoader {

	Dictionary<string, AudioClip> audioClips;

	public AudioLoader() {
		audioClips = new Dictionary<string, AudioClip>();
	}
	public AudioClip LoadAudioClip(string clipName) 
	{
		AudioClip clip;
		if(!audioClips.TryGetValue(clipName, out clip)) {
			Debug.LogError("Audio file " + clipName + " not found.");
		}
		clip.name = clipName;
		return clip;
	}

	

	public void SearchAndLoadClips(string folder) {
		string[] files = IOManager.GetFileList(folder);
		string path;
		AudioClip clip;
		string clipname;
		for(int i = 0; i < files.Length; i++) {
			path = files[i];
			Debug.Log(path);
			if(!path.EndsWith(".wav") && !path.EndsWith(".ogg")) continue;
			clip = LoadClip(path);
			if(clip == null)  {
				Debug.LogError(path + " is empty.");
			} else {
				clipname = path.Replace(folder, "");
				audioClips.Add(clipname, clip);
				Debug.Log("Found sound clip: " + clipname + ", duration: " + clip.length);
			}
			
		}		
	}

	AudioClip LoadClip(string path) {
		WWW www = new WWW("file:///" + path);
		return www.audioClip;
	}
	
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsView : SettingsView {

	Slider ambianceSlider;
	Toggle femaleVoice;

	// Use this for initialization
	void Start () {
		GetComponentInChildren<Text>().text = "Audio";

		ambianceSlider = AddSlider("Ambiance", 0f, 1f);
		ambianceSlider.onValueChanged.AddListener((v) => OnAmbianceChanged(v));

		femaleVoice = AddToggle("Female Voice");
		femaleVoice.onValueChanged.AddListener((v) => OnFemaleVoicesChanged(v));

		UpdateValues();
		initialized = true;	
	}

	void OnAmbianceChanged(float val) {
		SoundManager.ambientVolume = val;
		PreferencesCentral.ambianceVolume.value = val;
	}

	void OnFemaleVoicesChanged(bool value) {
		PreferencesCentral.femaleSounds.value = value;
	}

	protected override void UpdateValues() {
		ambianceSlider.value = SoundManager.ambientVolume;
		femaleVoice.isOn = SoundManager.femaleVoices;
	}
	
}

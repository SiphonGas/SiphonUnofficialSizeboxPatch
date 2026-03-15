using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingsView : SettingsView {

	// Components
	// Elements
	Toggle lookAtToggle;
	Toggle crushToggle;
	Toggle slowToggle;
	Toggle ignorePlayer;
	Toggle aiDefault;
	Slider speedSlider;
	Slider mouseSensibility;

	PlayerCamera cam;

	// Use this for initialization
	void Start () {
		cam = Camera.main.GetComponent<PlayerCamera>();


		lookAtToggle = AddToggle("Look At Player");
		lookAtToggle.onValueChanged.AddListener((value) => ToggleLookAtPlayer(value));

		crushToggle = AddToggle("Crush Tinies");
		crushToggle.onValueChanged.AddListener((value) => ToggleCrush(value));

		slowToggle = AddToggle("Slowdown speed with scale");
		slowToggle.onValueChanged.AddListener((value) => GameController.SetSlowDown(value));

		ignorePlayer = AddToggle("Ignore player when crushing");
		ignorePlayer.onValueChanged.AddListener((value) => ToggleIgnorePlayer(value));

		aiDefault = AddToggle("Enable AI by Default");
		aiDefault.onValueChanged.AddListener((b) => ToggleDefaultAI(b));


		speedSlider = AddSlider("Global Speed", 0.1f, 2f);
		speedSlider.onValueChanged.AddListener((v) => GameController.ChangeSpeed(v));

		mouseSensibility = AddSlider("Mouse Sensitivity", 1f, 20f);
		mouseSensibility.onValueChanged.AddListener((v) => cam.SetMouseSensibility(v));


		UpdateValues();
		initialized = true;
	}

	protected override void UpdateValues() {
		if(aiDefault != null) aiDefault.isOn = Giantess.defaultAI;
		lookAtToggle.isOn = HeadIK.lookAtPlayer;
		crushToggle.isOn = MicroNPC.crushEnabled;
		slowToggle.isOn = AnimationManager.slowdownWithSize;
		ignorePlayer.isOn = Giantess.ignorePlayer;
		speedSlider.value = GameController.globalSpeed;
		mouseSensibility.value = cam.orbit.mouseSensibility;
	}

	public void ToggleDefaultAI(bool value) {
		Giantess.defaultAI = value;
		PreferencesCentral.aiDefault.value = value;
	}

	public void ToggleLookAtPlayer(bool value)
	{
		HeadIK.lookAtPlayer = value;
	}

	public void ToggleCrush(bool value)
	{
		MicroNPC.crushEnabled = value;
		PreferencesCentral.crushEnabled.value = value;
	}

	public void ToggleIgnorePlayer(bool val) {
		Giantess.ignorePlayer = val;
		PreferencesCentral.ignorePlayer.value = val;
	}
}

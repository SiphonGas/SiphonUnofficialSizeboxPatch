using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoSettingsView : SettingsView {
	Toggle depthOfFieldToggle;
	Slider aoSlider;
	Toggle toggleShadows;
	Toggle toggleFPS;
	Toggle split3d;
	Toggle stereo3d;
	Toggle longFarPlane;

	Toggle oculus;
	Toggle openVR;

	Slider fovSlider;
	Slider shadowDistanceSlider;
	Slider cromaticAberrationSlider;
	Slider colorGainSlider;
	Slider colorValueSlider;
	Slider bloomSlider;
	Slider haze;
	Text fovLabel;
	CameraEffectsSettings cameraEffects;
	VRCamera vrCamera;
	Camera mainCamera;

	// Use this for initialization
	void Start () {
		mainCamera = Camera.main;
		cameraEffects = mainCamera.GetComponent<CameraEffectsSettings>();
		vrCamera = mainCamera.GetComponent<VRCamera>();

		GetComponentInChildren<Text>().text = "Video";

		depthOfFieldToggle = AddToggle("Depth of Field");
		depthOfFieldToggle.onValueChanged.AddListener((value) => cameraEffects.EnableDepthOfField(value));
		

		toggleFPS = AddToggle("Display FPS");
		toggleFPS.onValueChanged.AddListener((value) => main.ShowFPS(value));

		if(vrCamera.vrSupported) {
			if(vrCamera.splitSupported) {
				split3d = AddToggle("3D Split");
				split3d.onValueChanged.AddListener((value) => vrCamera.EnableSplit(value));
			}
			
			if(vrCamera.stereoSupported) {
				stereo3d = AddToggle("3D Stereo");
				stereo3d.onValueChanged.AddListener((value) => vrCamera.EnableStereo(value));
			}

			if(vrCamera.oculusSupported) {
				oculus = AddToggle("Oculus");
				oculus.onValueChanged.AddListener((value) => vrCamera.EnableOculus(value));
			}

			if(vrCamera.openVRSupported) {
				openVR = AddToggle("Steam VR");
				openVR.onValueChanged.AddListener((value) => vrCamera.EnableOpenVR(value));
			}
			
			longFarPlane = AddToggle("Use Long Far Plane");
			longFarPlane.onValueChanged.AddListener((value) => cameraEffects.SetLongFarPlane(value));
		}

		if(cameraEffects.shadowSupported) {
			toggleShadows = AddToggle("Shadows");
			toggleShadows.onValueChanged.AddListener((value) => cameraEffects.EnableShadows(value));

			shadowDistanceSlider = AddSlider("Shadow Distance", 0f, 400f);
			shadowDistanceSlider.onValueChanged.AddListener((v) => cameraEffects.SetShadowDistance(v));
		}

		fovSlider = AddSlider("FOV", 45f, 110f);
		fovSlider.onValueChanged.AddListener((v) => OnFOVChanged(v));
		fovLabel = fovSlider.transform.parent.GetComponentInChildren<Text>();		

		cromaticAberrationSlider = AddSlider("Chromatic Aberration", 0f, 3f);
		cromaticAberrationSlider.onValueChanged.AddListener((v) => cameraEffects.SetLensAberration(v));

		colorGainSlider = AddSlider("Color Gain", 1f, 2f);
		colorGainSlider.onValueChanged.AddListener((v) => cameraEffects.SetGain(v));

		colorValueSlider = AddSlider("Color Value", 0.5f, 3f);
		colorValueSlider.onValueChanged.AddListener((v) => cameraEffects.SetValue(v));

		bloomSlider = AddSlider("Bloom", 0f, 3f);
		bloomSlider.onValueChanged.AddListener((v) => cameraEffects.SetBloom(v));

		if(cameraEffects.ambientOcclusionSupported) {
			aoSlider = AddSlider("Ambient Occlusion", 0f, 20f);
			aoSlider.onValueChanged.AddListener((v) => cameraEffects.SetAO(v));


			haze = AddSlider("Sky Haze", 0f, 0.7f);
			haze.onValueChanged.AddListener((v) => cameraEffects.SetHaze(v));
		}




		UpdateValues();
		initialized = true;
	}

	protected override void UpdateValues() {
		depthOfFieldToggle.isOn = cameraEffects.GetDepthOfFieldValue();
		if(toggleShadows != null) toggleShadows.isOn = cameraEffects.GetShadowsValue();
		toggleFPS.isOn = MainView.fpsDisplayEnabled;

		if(vrCamera.vrSupported) {
			if(vrCamera.splitSupported)
				split3d.isOn = vrCamera.GetSplit();
			if(vrCamera.stereoSupported)
				stereo3d.isOn = vrCamera.GetStereo();
			if(vrCamera.oculusSupported)
				oculus.isOn = vrCamera.GetOculus();
			if(vrCamera.openVRSupported)
				openVR.isOn = vrCamera.GetOpenVR();
				
			longFarPlane.isOn = cameraEffects.GetLongFarPlane();
		} 
		


		if(aoSlider != null) aoSlider.value = cameraEffects.GetAO();
		fovSlider.value = PlayerCamera.defaultFOV;
		cromaticAberrationSlider.value = cameraEffects.GetLensAberration();
		colorGainSlider.value = cameraEffects.GetGain();
		colorValueSlider.value = cameraEffects.GetValue();
		bloomSlider.value = cameraEffects.GetBloom();
		if(shadowDistanceSlider != null) shadowDistanceSlider.value = cameraEffects.GetShadowDistance();
		if(haze != null) haze.value = cameraEffects.GetHaze();

	}

	public void OnFOVChanged(float value) {
		PlayerCamera.defaultFOV = value;
		mainCamera.fieldOfView = value;
		fovLabel.text = "FOV (" + (int) value + ")";
		PreferencesCentral.fov.value = value;


	}

}

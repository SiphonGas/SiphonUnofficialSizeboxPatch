using UnityEngine;
using UnityStandardAssets.CinematicEffects;

public class CameraEffectsSettings : MonoBehaviour {
	// Shadow Settings
	public static float shadowDistance = 200f;
	public float shadowNearPlane = 2f;

	// Camera far and close plane settings
	public float defaultNearPlane = 0.01f;
	public float farPlaneRatio = 100000f;
	float longFarPlaneRatio = 5000000f;
	public bool longFarPlane = false;
	[Range(0.0f, 1.0f)]
	public float fogPercentage = 0.3f;
	public Color fogColor = Color.white;
	UnityStandardAssets.ImageEffects.GlobalFog globalFog;
	DepthOfField depthOfField;
	AmbientOcclusion ambientOcclusion;
	LensAberrations lensAberration;
	TonemappingColorGrading tonnemappingColorGrading;
	TonemappingColorGrading.ColorGradingSettings colorGrading;
	Bloom bloom;
	Light directionalLight;
	Camera mainCamera;
	Transform parentCamera;
	Camera farCamera;
	bool initialized = false;
	int qualityLevel;
	public bool shadowSupported;
	public bool ambientOcclusionSupported;
	

	// Use this for initialization
	void Start () {
		qualityLevel = QualitySettings.GetQualityLevel();

		shadowSupported = QualitySettings.shadows != ShadowQuality.Disable;
		ambientOcclusionSupported = qualityLevel >= 4; // level beautiful or more

		if(shadowSupported) {
			shadowDistance = PreferencesCentral.shadowDistance.value;
		}

		globalFog = GetComponent<UnityStandardAssets.ImageEffects.GlobalFog>();
		RenderSettings.fogMode = FogMode.Linear;
		

		depthOfField = GetComponent<DepthOfField>();
		depthOfField.enabled = PreferencesCentral.depthOfField.value;


		ambientOcclusion = GetComponent<AmbientOcclusion>();
		if(ambientOcclusionSupported) {
			SetAO(PreferencesCentral.ambientOcclusion.value);

			if(GameController.IsMacroMap) {
				SetHaze(PreferencesCentral.haze.value);
			} else {			
				SetHaze(0f);
			}
		} else {
			SetAO(0f);
			SetHaze(0f);
		}

		

		lensAberration = GetComponent<LensAberrations>();
		SetLensAberration(PreferencesCentral.cromaticAberration.value);

		bloom = GetComponent<Bloom>();
		SetBloom(PreferencesCentral.bloom.value);


		tonnemappingColorGrading = GetComponent<TonemappingColorGrading>();
		colorGrading = tonnemappingColorGrading.colorGrading;
		SetGain(PreferencesCentral.colorGain.value);
		SetValue(PreferencesCentral.colorValue.value);
		

		directionalLight = GameObject.Find("Directional Light").GetComponent<Light>();
		EnableShadows(PreferencesCentral.shadows.value);

		mainCamera = Camera.main;
		parentCamera = mainCamera.transform.parent;
		farCamera = mainCamera.transform.GetChild(0).GetComponent<Camera>();

		initialized = true;
	
	}

	public void LateUpdate() {
		UpdateEffectsRealtime();
	}

	public void UpdateEffectsRealtime() {
		float playerScale = parentCamera.localScale.y;
		mainCamera.nearClipPlane = defaultNearPlane * playerScale;

		if(longFarPlane) {
			mainCamera.farClipPlane = mainCamera.nearClipPlane * longFarPlaneRatio;
		} else {
			mainCamera.farClipPlane = mainCamera.nearClipPlane * farPlaneRatio;
			
		}
		
		RenderSettings.fogEndDistance = mainCamera.farClipPlane / fogPercentage;
		RenderSettings.fogColor = fogColor;

		float nearClipFarCamera = mainCamera.farClipPlane - 10;
		if(nearClipFarCamera < 0.1f) nearClipFarCamera = 0.1f;
		farCamera.nearClipPlane = nearClipFarCamera;
		
		if(farCamera.nearClipPlane > farCamera.farClipPlane)
			farCamera.farClipPlane = farCamera.nearClipPlane + 10;

		QualitySettings.shadowDistance = shadowDistance * playerScale;
		QualitySettings.shadowNearPlaneOffset = shadowNearPlane * playerScale;

		AdjustDoF(playerScale);
	}

	public void AdjustDoF(float scale)
    {
		if(scale < ResizeCharacter.minSize) scale = ResizeCharacter.minSize;

        depthOfField.focus.farFalloff = 2000 * scale;
		float size = scale / 0.1f;
        depthOfField.focus.farBlurRadius = 25f / Mathf.Sqrt(size);
    }

	// CHROMATIC ABERRATION
	public void SetLensAberration(float value) {
		if(value == 0f) {
			lensAberration.chromaticAberration.enabled = false;
		} else {
			lensAberration.chromaticAberration.enabled = true;
			lensAberration.chromaticAberration.amount = value;
		}
		PreferencesCentral.cromaticAberration.value = value;
		
	}

	public float GetLensAberration() {
		if(!lensAberration.chromaticAberration.enabled) {
			return 0f;
		}
		else return lensAberration.chromaticAberration.amount;
	}

	// Bloom

	public void SetBloom(float value) {
		if(value == 0f) {
			bloom.enabled = false;
		} else {
			bloom.enabled = true;
			bloom.settings.intensity = value;
		}
		PreferencesCentral.bloom.value = value;
		
	}

	public float GetBloom() {
		if(!bloom.enabled) {
			return 0f;
		}
		else return bloom.settings.intensity;
	}

	// GAIN

	public void SetGain(float value) {
		colorGrading.basics.gain = value;
		tonnemappingColorGrading.colorGrading = colorGrading;
		PreferencesCentral.colorGain.value = value;
		colorGrading.enabled = true;
		
	}

	public float GetGain() {
		return colorGrading.basics.gain;
	}

	// VALUE

	public void SetValue(float value) {
		colorGrading.basics.value = value;
		tonnemappingColorGrading.colorGrading = colorGrading;
		PreferencesCentral.colorValue.value = value;
		colorGrading.enabled = true;
		
	}

	public float GetValue() {
		return colorGrading.basics.value;
	}
	
	// AMBIENT OCCLUSSION

	public void SetAO(float value) {
		if(value == 0f) {
			ambientOcclusion.enabled = false;
		} else {
			ambientOcclusion.enabled = true;
			ambientOcclusion.settings.radius = value;
		}
		PreferencesCentral.ambientOcclusion.value = value;
		UpdateDepth();
		
	}

	public float GetAO() {
		if(!ambientOcclusion.enabled) {
			return 0f;
		}
		else return ambientOcclusion.settings.radius;
	}

	// SHADOW DISTANCE
	public float GetShadowDistance() {
		return shadowDistance;
	}

	public void SetShadowDistance(float distance) {
		if(distance == 0f) {
			EnableShadows(false);
		} else {
			EnableShadows(true);
			shadowDistance = distance;
			PreferencesCentral.shadowDistance.value = distance;
		}
		
	}

	// DEPTH OF FIELD	
	public void SwitchDepthOfField()
	{
		EnableDepthOfField(!GetDepthOfFieldValue());

	}

	public void SwitchAmbienOcclusion()
	{
		EnableAmbientOcclusion(!GetAmbientOcclusionValue());
	}

	public void SwitchShadows()
	{
		EnableShadows(!GetShadowsValue());
	}

	public bool GetDepthOfFieldValue()
	{
		return depthOfField.enabled;
	}

	public bool GetAmbientOcclusionValue()
	{
		return ambientOcclusion.enabled;
	}

	public bool GetShadowsValue()
	{
		if(directionalLight) {
			return (directionalLight.shadows == LightShadows.Soft);
		} else {
			Debug.Log("No light source found");
			return false;
		}
		
	}

	public void EnableDepthOfField(bool value)
	{
		depthOfField.enabled = value;
		PreferencesCentral.depthOfField.value = value;
		UpdateDepth();
	}

	public void EnableAmbientOcclusion(bool value)
	{
		ambientOcclusion.enabled = value;
		UpdateDepth();
	}

	public void EnableShadows(bool value)
	{
		if(value) {
			directionalLight.shadows = LightShadows.Soft;
		} else {
			directionalLight.shadows = LightShadows.None;
		}
		PreferencesCentral.shadows.value = value;
		
	}

	void UpdateDepth() {
		if(!initialized) return;
		DepthTextureMode depthMode = DepthTextureMode.DepthNormals;
		if(!GetAmbientOcclusionValue()) {
			if(!GetDepthOfFieldValue()) {
				depthMode = DepthTextureMode.None;
			} else {
				depthMode = DepthTextureMode.Depth;
			}
		}
		mainCamera.depthTextureMode = depthMode;
	}

	

	// === 3D OPTIONS
	public float GetHaze() {
		if(!globalFog.enabled) return 0f;
		return fogPercentage;
	}

	public void SetHaze(float val) {
		fogPercentage = val;
		if(val == 0f) {
			globalFog.enabled = false;
		} else {
			globalFog.enabled = true;
		}
		if(GameController.IsMacroMap) {
			PreferencesCentral.haze.value = val;
		}
		
	}

	// LONG FAR plane
	public void SetLongFarPlane(bool value) {
		longFarPlane = value;
		
	}

	public bool GetLongFarPlane() {
		return longFarPlane;
	}
}




using UnityEngine;
using System.Collections;

public static class PreferencesCentral {
	public static FloatStored ambientOcclusion;
	public static BoolStored shadows;
	public static BoolStored fpsCount;
	public static BoolStored crushMicros;
	public static BoolStored depthOfField;
	public static FloatStored fov;
	public static FloatStored cromaticAberration;
	public static FloatStored colorGain;
	public static FloatStored colorValue;
	public static FloatStored bloom;
	public static FloatStored shadowDistance;
	public static FloatStored haze;

	// game options
	public static BoolStored ignorePlayer;
	public static BoolStored crushEnabled;
	public static FloatStored mouseSensibility;
	public static BoolStored aiDefault;
	public static BoolStored hairPhysics;

	// sound options
	public static FloatStored ambianceVolume;
	public static BoolStored femaleSounds;

	// hidden options
	public static BoolStored cameraRTSMode;


	public static void Initialize()
	{
		depthOfField = new BoolStored("DepthOfField", true);
		ambientOcclusion = new FloatStored("Ambient Occlusion", 0f);
		shadows = new BoolStored("Shadows", true);
		fpsCount = new BoolStored("FPS Count", false);
		crushMicros = new BoolStored("Crush", true);
		fov = new FloatStored("FOV", PlayerCamera.defaultFOV);
		cromaticAberration = new FloatStored("ChromaticAberration", 0.5f);
		colorGain = new FloatStored("ColorGain", 1.2f);
		colorValue = new FloatStored("ColorValue", 1f);
		bloom = new FloatStored("Bloom", 0.7f);
		shadowDistance = new FloatStored("ShadowDistance", 200f);
		haze = new FloatStored("Haze", 0.4f);

		// sound
		ambianceVolume = new FloatStored("AmbianceVolume", 0.5f);
		femaleSounds = new BoolStored("FemaleSounds", true);

		// game settings
		ignorePlayer = new BoolStored("IgnorePlayer", false);
		crushEnabled = new BoolStored("CrushEnabled", true);
		mouseSensibility = new FloatStored("Mouse Sensibility", 8f);
		aiDefault = new BoolStored("EnableAI", false);
		hairPhysics = new BoolStored("BodyPhysics", true);

		// hidden options
		cameraRTSMode = new BoolStored("RTSCamera", true);


	}

}

public class FloatStored {
	string key;
	float val;

	public float value {get { return val;} set { val = value; SaveFloatValue(key, value);} } 

	public FloatStored(string key, float defaultValue) {
		this.key = key;
		val = GetFloatValue(key, defaultValue);
	}

	float GetFloatValue(string key, float defaultValue)
	{
		if(PlayerPrefs.HasKey(key))
		{
			// read key
			float val = PlayerPrefs.GetFloat(key);
			// return key
			return val;
		}
		else
		{
			// return default
			return defaultValue;
		}
	}

	void SaveFloatValue(string key, float newValue)
	{
		PlayerPrefs.SetFloat(key, newValue);
	}
}

public class BoolStored {
	string key;
	bool val;

	public bool value {get { return val;} set { val = value; SaveBoolValue(key, value);} } 

	public BoolStored(string key, bool defaultValue) {
		this.key = key;
		val = GetBoolValue(key, defaultValue);
	}

	bool GetBoolValue(string key, bool defaultValue)
	{
		if(PlayerPrefs.HasKey(key))
		{
			// read key
			int val = PlayerPrefs.GetInt(key);
			// return key
			return IntToBool(val);
		}
		else
		{
			// return default
			return defaultValue;
		}
	}

	bool IntToBool(int integer)
	{
		return !(integer == 0);
	}

	void SaveBoolValue(string key, bool newValue)
	{
		PlayerPrefs.SetInt(key, BoolToInt(newValue));
	}

	int BoolToInt(bool boolean)
	{
		int integer = 0;
		if(boolean) integer = 1;
		return integer;
	}
}
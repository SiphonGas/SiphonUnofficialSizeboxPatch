using System.Collections;
using UnityEngine;
#if ENABLE_VR
using UnityEngine.VR;
#endif

public class VRCamera : MonoBehaviour {
	enum Mode { None, Split, Stereo, OpenVR, Oculus};
	#if ENABLE_VR
	Mode currentMode = Mode.None;
	#endif
	public bool vrSupported = false;
	public bool oculusSupported = false;
	public bool openVRSupported = false;
	public bool stereoSupported = false;
	public bool splitSupported = false;


	// Use this for initialization
	void Start() {
		#if ENABLE_VR
		vrSupported = true;
		StartCoroutine(CheckDeviceSupport());
		#endif		
	}

	IEnumerator CheckDeviceSupport() {
		#if ENABLE_VR
		VRSettings.LoadDeviceByName("Oculus");
		yield return null;
		if(VRSettings.loadedDeviceName != "") {
			// Debug.Log("Oculus Supported");
			oculusSupported = true;
		}

		VRSettings.LoadDeviceByName("OpenVR");
		yield return null;
		if(VRSettings.loadedDeviceName != "") {
			openVRSupported = true;
			// Debug.Log("OpenVR Supported");
		}

		VRSettings.LoadDeviceByName("stereo");
		yield return null;
		if(VRSettings.loadedDeviceName != "") {
			stereoSupported = true;
			// Debug.Log("stereo Supported");
		}

		VRSettings.LoadDeviceByName("split");
		yield return null;
		if(VRSettings.loadedDeviceName != "") {
			splitSupported = true;
			// Debug.Log("split Supported");
		}

		VRSettings.LoadDeviceByName("");
		#endif
		yield return null;
    }

    IEnumerator LoadDevice(string newDevice) {
		#if ENABLE_VR
        VRSettings.LoadDeviceByName(newDevice);
        yield return null;
        VRSettings.enabled = true;
		#endif
		yield return null;
    }

	void EnableMode(bool on, string modeName, Mode mode) {
		#if ENABLE_VR
		if(on && (!VRSettings.enabled || currentMode != mode)) {
			currentMode = mode;
			StartCoroutine(LoadDevice(modeName));
		} 
		else if (!on && mode == currentMode) {
			VRSettings.LoadDeviceByName("");
		}
		#endif
	}

	public void EnableSplit(bool on) {
		EnableMode(on, "Split", Mode.Split);
		Player.vrMode = on;
	}

	public void EnableStereo(bool on) {
		EnableMode(on, "Stereo", Mode.Stereo);
	}

	public void EnableOculus(bool on) {
		EnableMode(on, "Oculus", Mode.Oculus);
		Player.vrMode = on;
	}

	public void EnableOpenVR(bool on) {
		EnableMode(on, "OpenVR", Mode.OpenVR);
		Player.vrMode = on;
	}

	public bool GetSplit()
	{
		#if ENABLE_VR
		return VRSettings.enabled && currentMode == Mode.Split;
		#else
		return false;
		#endif
	}

	public bool GetStereo()
	{
		#if ENABLE_VR
		return VRSettings.enabled && currentMode == Mode.Stereo;
		#else
		return false;
		#endif
	}

	public bool GetOpenVR()
	{
		#if ENABLE_VR
		return VRSettings.enabled && currentMode == Mode.OpenVR;
		#else
		return false;
		#endif
	}

	public bool GetOculus()
	{
		#if ENABLE_VR
		return VRSettings.enabled && currentMode == Mode.Oculus;
		#else
		return false;
		#endif
	}

	public bool IsInVR() {
		#if ENABLE_VR
		return currentMode == Mode.OpenVR || currentMode == Mode.Oculus;
		#else
		return false;
		#endif		
	}
}

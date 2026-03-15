using UnityEngine;
using System.Collections;

public class MainCameraController : MonoBehaviour {

	PlayerCamera playerCamera;
	EditPlacement editCamera;


	// Use this for initialization
	void Awake () {
		// get the references to the other components
		playerCamera = GetComponent<PlayerCamera>();
		editCamera = GetComponent<EditPlacement>();
		gameObject.AddComponent<VRCamera>();
	}
	

	public void SwitchCameraMode() {
		if(playerCamera.enabled)
		{
			GameController.inputEnabled = false;
			playerCamera.enabled = false;
			editCamera.enabled = true;
		}
		else
		{
			editCamera.enabled = false;			
			playerCamera.enabled = true;
			GameController.inputEnabled = true;
		}
	}
}

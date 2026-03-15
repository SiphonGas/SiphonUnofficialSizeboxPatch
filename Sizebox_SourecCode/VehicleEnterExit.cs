using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Vehicles.Car;
using UnityStandardAssets.Vehicles.Aeroplane;
using UnityEngine;

public class VehicleEnterExit : MonoBehaviour {
	
	VehicleInputController vehicleInput;
	AeroplaneUserControl2Axis aeroplaneControl;
	Camera mainCamera;
	PlayerCamera playerCamera;
	bool isOn;
	bool justEntered = false;


	void Awake () {
		vehicleInput = GetComponent<VehicleInputController>();
		aeroplaneControl = GetComponent<AeroplaneUserControl2Axis>();
		EnableControl(false);
		

		isOn = false;

		mainCamera = Camera.main;
		playerCamera = mainCamera.GetComponent<PlayerCamera>();
		
	}
	
	// Update is called once per frame
	void Update () {
		if(transform.parent != null) {
			transform.SetParent(null);
		}
		if(transform.localScale.y != 1) {
			transform.localScale = Vector3.one;
		}
		if(!justEntered && isOn && Input.GetKeyDown(KeyCode.Return)) {
			isOn = false;
			GetPlayerOutsideOfVehicle();
			EnableControl(false);
		}
		if(isOn)
		{
			GameController.playerInstance.transform.position = transform.position;
		}
		if(justEntered) justEntered = false;
	}

	public void EnterVehicle() {
		justEntered = true;
		isOn = true;
		GameController.playerInstance.SetActive(false);
		GameController.playerInstance.ChangeScale(1f);
		playerCamera.SetCameraTarget(transform);
		EnableControl(true);

	}

	void EnableControl(bool value) {
		if(vehicleInput) vehicleInput.enabled = value;
		if(aeroplaneControl) aeroplaneControl.enabled = value;
	}

	void GetPlayerOutsideOfVehicle()
	{
		playerCamera.SetCameraTarget(GameController.playerInstance.transform);
		GameController.playerInstance.myTransform.position = transform.position - transform.right * 2;
		GameController.playerInstance.SetActive(true);
	}

	void OnDestroy()
	{
		if(isOn)
		{
			ObjectManager.Instance.vehicles.Remove(this);
			GetPlayerOutsideOfVehicle();
		}
	}
}

using UnityEngine;
using System.Collections;

public class MainInput : MonoBehaviour {
	GameController controller;
	CameraEffectsSettings cameraEffects;
	MainView view;
	NPCSpawner npcSpawner;

	// Use this for initialization
	void Start () {
		controller = GetComponent<GameController>();
		view = controller.view;
		cameraEffects = GetComponent<CameraEffectsSettings>();
		npcSpawner = GetComponent<NPCSpawner>();
	}
	
	// Update is called once per frame
	void Update () {
		// if(Input.GetKeyDown(KeyCode.Escape)) controller.OnPauseClick();
		if(Input.GetKeyDown(KeyCode.Escape)) view.OpenPauseMenu();
		if(Input.GetKeyDown(KeyCode.Alpha1)) GameController.IncreaseSpeed();
		if(Input.GetKeyDown(KeyCode.Alpha2)) GameController.DecreaseSpeed();

		if(Input.GetKeyUp(KeyCode.Tab)) view.ChangeMode();

		if(Input.GetKeyDown(KeyCode.F1)) cameraEffects.SwitchDepthOfField();
		if(Input.GetKeyDown(KeyCode.F2)) cameraEffects.SwitchAmbienOcclusion();
		if(Input.GetKeyDown(KeyCode.F3)) cameraEffects.SwitchShadows();
		if(Input.GetKeyDown(KeyCode.F4)) ToggleLookAtPlayer();

		if(Input.GetKeyUp(KeyCode.F11)) view.TakeScreenshot();
		
		if(view.mode == MainView.Mode.Edit) {
			if(Input.GetKeyUp(KeyCode.O)) view.editMode.OnCatalogClick(0);
			if(Input.GetKeyUp(KeyCode.G)) view.editMode.OnCatalogClick(1);
			if(Input.GetKeyUp(KeyCode.P)) view.editMode.OnCatalogClick(2);
			if(Input.GetKeyUp(KeyCode.M)) view.editMode.placement.MoveCurrentGO();
		}

		if(view.mode == MainView.Mode.Play) {
			if(Input.GetButton("Spawn Male")) npcSpawner.SpawnMicro(false);
			if(Input.GetButton("Spawn Female")) npcSpawner.SpawnMicro(true);
		}
		
	}

	void ToggleLookAtPlayer()
	{
		HeadIK.lookAtPlayer = !HeadIK.lookAtPlayer;
	}
}

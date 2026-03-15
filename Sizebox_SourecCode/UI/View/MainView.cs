using UnityEngine;
using System.Collections;

public class MainView : MonoBehaviour {
	GameController controller;
	public enum Mode {Play, Edit}
	public Mode mode {get; private set;}
	GameObject mainCanvas;
	public EditView editMode {get; private set;}
	MainCameraController mainCamera;
	public PauseController pause;
	FPSDisplay fpsDisplay;
	static public bool fpsDisplayEnabled = true;
	EditPlacement placement;
	
	public void ShowFPS(bool show) {
		fpsDisplayEnabled = show;
		PreferencesCentral.fpsCount.value = show;
		if(!show) {
			fpsDisplay.enabled = false;
		}
		if(show && !controller.Paused) {
			fpsDisplay.enabled = true;
		}
	}
		

	// Use this for initialization
	void Start () {
		fpsDisplayEnabled = PreferencesCentral.fpsCount.value;

		if(FindObjectOfType<SmartConsole>() == null) Instantiate(Resources.Load<SmartConsole>("UI/Console"));

		controller = GetComponent<GameController>();
		mainCamera = GetComponent<MainCameraController>();

		gameObject.AddComponent<InterfaceControl>();

		mainCanvas = Instantiate(Resources.Load("UI/MainCanvas") as GameObject);
		fpsDisplay = mainCanvas.AddComponent<FPSDisplay>();
		if(!fpsDisplayEnabled) fpsDisplay.enabled = false;

		editMode = Instantiate(Resources.Load("UI/EditMode") as GameObject).AddComponent<EditView>();
		editMode.transform.SetParent(mainCanvas.transform, false);
		editMode.gameObject.SetActive(false);

		pause = Instantiate(Resources.Load<GameObject>("UI/PauseMenu")).AddComponent<PauseController>();
		pause.transform.SetParent(mainCanvas.transform, false);
		pause.SetGameController(controller);
		pause.main = this;
		pause.gameObject.SetActive(false);

		gameObject.AddComponent<MainInput>();

		mode = Mode.Play;

		// Initialize Cursor State
		// TODO: fix error on linux, there is still 1 pixel of error
		Cursor.lockState = CursorLockMode.Confined;
		Cursor.visible = true;

	}
	
	// Update is called once per frame
	void Update () {
		// Update Cursor State
		Cursor.visible = controller.Paused || (mode == Mode.Edit);
		
	}

	public void ChangeMode()
	{
		mainCamera.SwitchCameraMode();
		if(mode == Mode.Play) {
			mode = Mode.Edit;
			editMode.gameObject.SetActive(true);
		}
		else {			
			mode = Mode.Play;
			editMode.gameObject.SetActive(false);
		}
	}

	public void OpenPauseMenu() {
		controller.OnPauseClick();
		pause.gameObject.SetActive(controller.Paused);
		fpsDisplay.enabled = !controller.Paused && fpsDisplayEnabled;
	}

	public void TakeScreenshot() {		
		StartCoroutine(ScreenshotRoutine());
	}

	IEnumerator ScreenshotRoutine()
	{
		SetGizmo(false);
		mainCanvas.SetActive(false);
		controller.modelManager.SaveScreenshot();
		yield return null;
		mainCanvas.SetActive(true);
		SetGizmo(true);
	}

	void SetGizmo(bool value) {
		if(placement == null) placement = GetComponent<EditPlacement>();
		if(placement == null) return;
		value = value && mode == Mode.Edit;
		placement.enabled = value;
	}
}

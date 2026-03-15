using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;


public class PauseController : MonoBehaviour {
	public MainView main;
	private GameController gameController;
	GameObject settingsPrefab;
	GameObject gameSettings;
	GameObject videoSettings;
	GameObject audioSettings;

	// Settings Toggles
	public Toggle toggleCrush;
	public Toggle toggleAmbientOcclusion;
	public Toggle toggleDoF;
	public Toggle toggleLookAt;
	// Things needed for settings
	Button buttonPrefab;
	GridLayoutGroup buttonLayout;

	void Start() {
		buttonPrefab = Resources.Load<Button>("UI/Pause/PauseButton");
		settingsPrefab = Resources.Load<GameObject>("UI/Pause/SettingsMenu");

		gameSettings = Instantiate(settingsPrefab);
		gameSettings.transform.SetParent(transform, false);
		gameSettings.AddComponent<GameSettingsView>();
		gameSettings.SetActive(false);

		videoSettings = Instantiate(settingsPrefab);
		videoSettings.transform.SetParent(transform, false);
		videoSettings.AddComponent<VideoSettingsView>().main = main;
		videoSettings.SetActive(false);
		

		audioSettings = Instantiate(settingsPrefab);
		audioSettings.transform.SetParent(transform, false);
		audioSettings.AddComponent<AudioSettingsView>();
		audioSettings.SetActive(false);

		buttonLayout = gameObject.GetComponentInChildren<GridLayoutGroup>();
		AddButton("Resume").onClick.AddListener(() => OnResumeClick());
		AddButton("Restart").onClick.AddListener(() => OnResetClick());
		AddButton("Save").onClick.AddListener(() => OnSaveClick());
		AddButton("Settings").onClick.AddListener(() => OnSettingsClick());
		AddButton("Video").onClick.AddListener(() => OnVideoClick());
		AddButton("Audio").onClick.AddListener(() => OnAudioClick());
		AddButton("Main Menu").onClick.AddListener(() => OnMainMenuClick());
		AddButton("Quit Game").onClick.AddListener(() => OnQuitClick());
	}

	void OnEnable() {
		Debug.Log("Game paused");
		if(!Screen.fullScreen) Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	void OnDisabled() 
	{
		Debug.Log("Game resumed");
		Cursor.lockState = CursorLockMode.Confined;
	}

	Button AddButton(string label) {
		Button button = Instantiate(buttonPrefab);
		button.transform.SetParent(buttonLayout.transform, false);
		button.gameObject.GetComponent<Text>().text = label;
		return button;
	}

	public void SetGameController(GameController gc)
	{
		gameController = gc;
	}

	public void OnResumeClick()
	{
		main.OpenPauseMenu();
	}

	public void OnResetClick()
	{
		gameController.Paused = false;
		NetworkManager.singleton.StopHost();
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		//MainController.Reset();
	}

	public void OnSaveClick()
	{
		// It needs to be reimplemented
	}

	public void Save(string filename)
	{

		gameController.SaveScene(filename);
	}

	// Setting Options
	public void OnSettingsClick()
	{
		// UpdateSettingValues();
		gameSettings.SetActive(true);
	}

	public void OnAudioClick()
	{
		audioSettings.SetActive(true);
	}

	public void OnVideoClick() 
	{
		videoSettings.SetActive(true);
	}

	public void CloseSettings()
	{
		gameSettings.SetActive(false);
	}


	public void OnMainMenuClick() {
		gameController.Paused = false;
		NetworkManager.singleton.StopHost();
		MainController.SwitchScene("MainMenu");
	}

	// Quit Options
	public void OnQuitClick()
	{
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#endif
		NetworkManager.singleton.StopHost();
		Application.Quit();	
	}

}

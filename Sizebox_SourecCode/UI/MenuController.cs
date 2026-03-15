using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuController : MonoBehaviour {
	private static MenuController menuController;
	private List<GameObject> listChar;
	IOManager modelManager;
	int activeChar;
	int activeScene;

	[System.Serializable]
	public class StageData {
		public string scene;
		public Sprite thumbnail;

	}
	public List<StageData> listStages;
	public Canvas charUI;
	public Canvas loadUI;

	StateManager stateManager;

	void Awake() {
		stateManager = StateManager.instance;
	}
	
	// Use this for initialization
	protected void Start()
	{
		Cursor.visible = true;
		modelManager = IOManager.GetIOManager();
		menuController = this;
		listChar = modelManager.GetPlayerModels();
		activeChar = Random.Range(0,listChar.Count);
		activeScene = Random.Range(0,listStages.Count);
	}
	
	protected void OnDestroy()
	{
		if(menuController != null)
		{
			menuController = null;
		}
	}
	

	public void OnStartClick() {
		stateManager.gameSettings.multiplayer = false;
		charUI.gameObject.SetActive(true);
		
	}

	public void OnLANClick() {
		stateManager.gameSettings.multiplayer = true;
		charUI.gameObject.SetActive(true);
	}

	public void OnLoadClick() {
		loadUI.gameObject.SetActive(true);	
		
	}

	public void OnExitClick() {
		Application.Quit();
	}

	public void switchNextChar() {
		activeChar = (listChar.Count + (activeChar + 1)) % listChar.Count;
	}

	public void switchPreviousChar() {
		activeChar = (listChar.Count + (activeChar - 1)) % listChar.Count;
	}

	public GameObject getActiveChar() {
		return listChar[activeChar];
	}


	public void switchNextScene() {
		activeScene = (listStages.Count + (activeScene + 1)) % listStages.Count;
	}

	public void switchPreviousScene() {
		activeScene = (listStages.Count + (activeScene - 1)) % listStages.Count;
	}

	public StageData getActiveSceneData() {
		return listStages[activeScene];
	}

	
}

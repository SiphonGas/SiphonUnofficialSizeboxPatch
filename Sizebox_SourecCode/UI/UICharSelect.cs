using UnityEngine;
using System.Collections;

public class UICharSelect : MonoBehaviour {
	MenuController menu;
	GameObject actualChar;
	public Canvas uiStage;
	public string loadScene = "";

	void OnEnable()
	{
		menu = (MenuController)  Camera.main.GetComponent<MenuController>();
		showChar();
	}

	// Use this for initialization
	void Start () {
		menu = (MenuController)  Camera.main.GetComponent<MenuController>();
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void SwitchNextChar() {
		menu.switchNextChar();
		showChar();

	}

	public void SwitchPreviousChar() {
		menu.switchPreviousChar();
		showChar();

	}

	public void showChar() {
		if(actualChar != null) {
			Destroy(actualChar);
		}
		GameObject charac = menu.getActiveChar();

		StateManager.instance.myData.name = charac.name;

		actualChar = (GameObject) Instantiate(charac, new Vector3(0,0,-5), Quaternion.identity);
		actualChar.transform.eulerAngles = new Vector3(0,180,0);
		

	}

	public void BackButton() {
		Destroy(actualChar);
		loadScene = "";
		gameObject.SetActive(false);
	}

	public void OkButton() {
		if(loadScene.Length > 1) 
		{
			StateManager.instance.gameSettings.scene = loadScene;
			MainController.SwitchScene(loadScene);
		} else {
			BackButton();
			uiStage.gameObject.SetActive(true);
		}
		


	}
}

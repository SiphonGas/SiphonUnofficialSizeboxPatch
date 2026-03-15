using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIStageSelect : MonoBehaviour {

	MenuController menu;
	MenuController.StageData actualStage;
	public Canvas charUI;
	public Canvas loadingUI;

	public Image imagen;

	// Use this for initialization
	void Start () {
		menu = (MenuController)  Camera.main.GetComponent<MenuController>();
		showStage();
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	public void switchNextScene() {
		menu.switchNextScene();
		showStage();

	}

	public void switchPreviousScene() {
		menu.switchPreviousScene();
		showStage();

	}

	public void showStage() {
		actualStage = menu.getActiveSceneData();

		imagen.sprite = actualStage.thumbnail;
		//actualStage = (GameObject) Instantiate(charac, new Vector3(0,0,-5), Quaternion.identity);
		//actualStage.transform.eulerAngles = new Vector3(0,180,0);

	}

	public void BackButton() {
		transform.parent.gameObject.SetActive(false);
		charUI.gameObject.SetActive(true);
		
	}

	public void OkButton() {
		loadingUI.gameObject.SetActive(true);
		MainController.SwitchScene(actualStage.scene);


	}
}

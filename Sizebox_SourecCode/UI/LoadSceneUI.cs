using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadSceneUI : MonoBehaviour {
	public GameObject listCanvas;
	public Button buttonPrefab;
	string[] listSavedScenes;
	IOManager ioManager;
	public Canvas charUI;

	// Use this for initialization
	void Start () {
		ioManager = IOManager.GetIOManager();
		listSavedScenes = ioManager.GetListSavedFiles();
		for(int i = 0; i < listSavedScenes.Length; i++)
		{
			Button but = Instantiate(buttonPrefab);
			but.transform.SetParent(listCanvas.transform, false);
			but.transform.GetChild(0).GetComponent<Text>().text = listSavedScenes[i];
			string saveDataName = listSavedScenes[i];
			but.onClick.AddListener(() => OnClickLoadScene(saveDataName));
			

		}
	}

	void OnClickLoadScene(string scene) 
	{
		Debug.Log("Load Scene: " + scene);
		GameController.SaveData data = ioManager.LoadDataFile(scene);
		charUI.gameObject.SetActive(true);
		charUI.GetComponent<UICharSelect>().loadScene = data.scene;
		transform.parent.gameObject.SetActive(false);
	}

	public void BackButton() {
		transform.parent.gameObject.SetActive(false);
	}
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SaveWindow : MonoBehaviour {

	// Use this for initialization
	private InputField field;
	private PauseController pauseController;
	

	void Start()
	{
		field = transform.GetChild(0).GetComponent<InputField>();
		Debug.Assert(field, "There is no field in the save window ui");
		pauseController = transform.parent.GetComponent<PauseController>();
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			gameObject.SetActive(false);
		}
		else if (Input.GetKeyDown(KeyCode.Return))
		{
			Debug.Log("Enter key on");
			if(field.text.Length > 2) {
				pauseController.Save(field.text);
				field.text = "";
				gameObject.SetActive(false);
			}
		}
	}
}

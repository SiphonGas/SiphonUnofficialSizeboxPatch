using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsView : MonoBehaviour {
	protected bool initialized = false;
	Toggle toggle;
	GameObject slider;
	GridLayoutGroup gridGroup;
	public MainView main;

	void Awake() {
		gridGroup = GetComponentInChildren<GridLayoutGroup>();
		toggle = Resources.Load<Toggle>("UI/Pause/Toggle");
		slider = Resources.Load<GameObject>("UI/Pause/Slider");

		GetComponentInChildren<Button>().onClick.AddListener(() => ClosePanel());
	}

	void ClosePanel() {
		gameObject.SetActive(false);
	}
	
	protected Toggle AddToggle(string text) {
		Toggle newToggle = Instantiate(toggle);
		newToggle.GetComponentInChildren<Text>().text = text;
		newToggle.transform.SetParent(gridGroup.transform, false);
		return newToggle;
	}

	protected Slider AddSlider(string text, float min, float max) {
		GameObject newSlider = Instantiate(slider);
		newSlider.GetComponentInChildren<Text>().text = text;
		newSlider.transform.SetParent(gridGroup.transform, false);
		Slider sliderComponent = newSlider.GetComponentInChildren<Slider>();
		sliderComponent.minValue = min;
		sliderComponent.maxValue = max;
		return sliderComponent;
	}

	void OnEnable() {
		if(initialized)
			UpdateValues();
	}

	protected virtual void UpdateValues() {

	}

}

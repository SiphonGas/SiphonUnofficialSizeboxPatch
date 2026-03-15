using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using RuntimeGizmos;

public class TransformView : MonoBehaviour {
	

	Slider sliderVerticalOffset;
	Slider sliderRotX;
	Slider sliderRotY;
	Slider sliderRotZ;
	Slider sliderScale;
	Toggle toggleRotation;
	Toggle hideHandlesToggle;
	Text scaleLabel;
	string scaleString;

	InterfaceControl control;

	TransformGizmo gizmo;
	
	void Awake () {
		control = Camera.main.GetComponent<InterfaceControl>();
		gizmo = Camera.main.GetComponent<TransformGizmo>();
		scaleString = "Scale: ";
	
		Text[] sliderLabel = transform.GetComponentsInChildren<Text>();
		sliderLabel[1].text = "Vertical Offset";
		sliderLabel[2].text = "Rotation X";
		sliderLabel[3].text = "Rotation Y";
		sliderLabel[4].text = "Rotation Z";
		sliderLabel[5].text = "Scale";
		sliderLabel[6].text = "Lock Rotation";
		sliderLabel[7].text = "Show Handles";
		scaleLabel = sliderLabel[5];
		UpdateScaleTag(1f);

		Button[] buttons = transform.GetComponentsInChildren<Button>();
		buttons[0].onClick.AddListener(OnTranslateClick);
		buttons[1].onClick.AddListener(OnRotateClick);
		buttons[2].onClick.AddListener(OnSpaceClick);

		


		sliderVerticalOffset = GetSliderOfTag(sliderLabel[1]);
		sliderRotX = GetSliderOfTag(sliderLabel[2]);
		sliderRotY = GetSliderOfTag(sliderLabel[3]);
		sliderRotZ = GetSliderOfTag(sliderLabel[4]);
		sliderScale = GetSliderOfTag(sliderLabel[5]);
		Toggle[] toogles = GetComponentsInChildren<Toggle>();
		toggleRotation = toogles[0];
		hideHandlesToggle = toogles[1];

		sliderVerticalOffset.minValue = -200f;
		sliderVerticalOffset.maxValue = 200f;
		sliderVerticalOffset.onValueChanged.AddListener(control.SetYAxisOffset);

		sliderRotX.minValue = -200;
		sliderRotX.maxValue = 200;
		sliderRotX.onValueChanged.AddListener(control.RotateXAxis);


		sliderRotY.minValue = -200;
		sliderRotY.maxValue = 200;
		sliderRotY.onValueChanged.AddListener(control.RotateYAxis);

		sliderRotZ.minValue = -200;
		sliderRotZ.maxValue = 200;
		sliderRotZ.onValueChanged.AddListener(control.RotateZAxis);


		sliderScale.minValue = -300;
		sliderScale.maxValue = 300;
		sliderScale.onValueChanged.AddListener(control.SetScale);
		sliderScale.onValueChanged.AddListener(OnScaleChanged);

		control.OnSelected += OnChangedObject;
		
		toggleRotation.isOn = false;
		toggleRotation.onValueChanged.AddListener((val) => control.LockRotation(val));

		hideHandlesToggle.isOn = EditPlacement.showHandles;
		hideHandlesToggle.onValueChanged.AddListener((val) => ShowHandles(val));

		
		gameObject.SetActive(false);
		
	}

	void OnTranslateClick() {
		gizmo.SetMoveMode();
	}

	void OnRotateClick() {
		gizmo.SetRotateMode();
	}

	void OnSpaceClick() {
		gizmo.SetSpaceMode();
	}

	void OnScaleChanged(float scale) {
		if(control.selectedEntity == null) return;
		UpdateScaleTag(control.selectedEntity.Height);
	}

	void UpdateScaleTag(float scale) {
		scaleLabel.text = scaleString + GameController.ConvertScaleToHumanReadable(scale);
	}

	Slider GetSliderOfTag(Text tag)
	{
		Slider slider = tag.transform.parent.parent.GetComponent<Slider>();
		if(slider == null) Debug.LogError("Slider not found");
		return slider;
	}

	void OnChangedObject()
	{
		if(gameObject.activeSelf)
			ReloadValues();
	}

	void OnEnable()
	{
		ReloadValues();		
	}

	void ReloadValues()
	{
		if(control == null) return;
		if(control.selectedEntity == null) {
			return;
		}
		// position settings			
		sliderVerticalOffset.value = control.GetYAxisOffset();			

		// rotation settings
		sliderRotX.value = control.GetXRotation();
		sliderRotY.value = control.GetYRotation();
		sliderRotZ.value = control.GetZRotation();

		// scale settings
		sliderScale.value = control.GetScale();

		UpdateScaleTag(control.selectedEntity.Height);
		
	}

	void ShowHandles(bool show) {
		EditPlacement.showHandles = show;
	}
	
	
}

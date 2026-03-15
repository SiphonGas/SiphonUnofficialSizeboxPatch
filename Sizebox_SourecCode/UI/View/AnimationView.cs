using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AnimationView : MonoBehaviour {

	Slider speedSlider;
	GameObject placeholder;
	int elementsCount;
	GridLayoutGroup grid;
	InterfaceControl control;
	int page = 0;
	int pageCount = 0;
	Text[] elements;

	

	void Start()
	{		

		control = Camera.main.GetComponent<InterfaceControl>();

		Button[] buttons = GetComponentsInChildren<Button>();
		buttons[0].onClick.AddListener(() => OnPrevious());
		buttons[1].onClick.AddListener(() => OnNext());

		speedSlider = GetComponentInChildren<Slider>();
		speedSlider.minValue = 0f;
		speedSlider.maxValue = 3f;
		speedSlider.value = 1f;
		speedSlider.onValueChanged.AddListener((speed) => control.ChangeAnimationSpeed(speed));

		placeholder = Resources.Load("UI/Button/ListElement") as GameObject;
		grid = GetComponentInChildren<GridLayoutGroup>();

		RectTransform rt = grid.GetComponent<RectTransform>();
		int maxRows = (int) rt.rect.height / 30;

		elementsCount = maxRows;

		pageCount = (int) (control.animations.Length / elementsCount) + 1;

		elements = new Text[elementsCount];

		for(int i = 0; i < elementsCount; i++)
		{
			GameObject go = Instantiate(placeholder, grid.transform) as GameObject;
			go.name = "Element " + i;
			elements[i] = go.GetComponent<Text>();
			Button button = go.GetComponent<Button>();
			int number = i;
			button.onClick.AddListener(() => OnElementClick(number));
		}
		LoadPage(page);
		control.OnSelected += OnChangedCharacter;
		gameObject.SetActive(false);
	}

	void OnChangedCharacter()
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
		if(control.humanoid == null) {
			gameObject.SetActive(false);
			return;
		}
		speedSlider.value = control.GetAnimationSpeed();
		
	}


	void OnElementClick(int i)
	{
		control.SetAnimation(elements[i].text);	
	}

	void LoadPage(int pag)
	{
		int baseItem = pag * elementsCount;
		for(int i = 0; i < elementsCount; i++)
		{
			int elem = baseItem + i;
			if(elem < control.animations.Length) {
				elements[i].text = control.animations[elem];
				elements[i].gameObject.SetActive(true);
			} else {
				elements[i].gameObject.SetActive(false);
			}			
		}
	}

	void OnNext()
	{
		page++;
		if(page >= pageCount) page = 0;
		LoadPage(page);
	}

	void OnPrevious()
	{
		page--;
		if(page < 0) page = pageCount - 1;
		LoadPage(page);
	}



}

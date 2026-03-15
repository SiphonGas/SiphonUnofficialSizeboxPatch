using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MorphsView : MonoBehaviour {

	Giantess.MorphData[] morphs;
	GameObject placeholder;
	int elementsCount;
	GridLayoutGroup grid;
	InterfaceControl control;
	int page = 0;
	int pageCount = 0;
	Text[] text;
	Slider[] slider;


	void Start()
	{	
		control = Camera.main.GetComponent<InterfaceControl>();

		Button[] buttons = GetComponentsInChildren<Button>();
		buttons[0].onClick.AddListener(() => OnPrevious());
		buttons[1].onClick.AddListener(() => OnNext());
		buttons[2].onClick.AddListener(() => OnApply());

		placeholder = Resources.Load("UI/Button/FieldSlider") as GameObject;
		grid = GetComponentInChildren<GridLayoutGroup>();

		RectTransform rt = grid.GetComponent<RectTransform>();
		int maxRows = (int) rt.rect.height / 30;

		elementsCount = maxRows;


		text = new Text[elementsCount];
		slider = new Slider[elementsCount];

		for(int i = 0; i < elementsCount; i++)
		{
			GameObject go = Instantiate(placeholder, grid.transform) as GameObject;
			go.name = "Slider " + i;
			text[i] = go.GetComponentInChildren<Text>();
			slider[i] = go.GetComponentInChildren<Slider>();
			int number = i;
			slider[i].onValueChanged.AddListener((val) => OnValueChanged(number, val));
		}
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
		if(control.giantess == null) {
			gameObject.SetActive(false);
			return;
		}
		morphs = control.GetMorphList();
		if(morphs == null) return;
		page = 0;
		pageCount = (int) (morphs.Length / elementsCount) + 1;
		LoadPage(page);
	}

	void OnValueChanged(int i, float value)
	{
		int m = page * elementsCount + i;
		control.SetMorph(m, value);	
	}

	void LoadPage(int pag)
	{
		int baseItem = pag * elementsCount;
		for(int i = 0; i < elementsCount; i++)
		{
			int elem = baseItem + i;
			if(elem < morphs.Length) {
				text[i].text = morphs[elem].name;
				slider[i].value = morphs[elem].weight; 
				text[i].transform.parent.gameObject.SetActive(true);
			} else {
				text[i].transform.parent.gameObject.SetActive(false);
			}
			
		}
	}

	void OnNext()
	{
		page++;
		if(page >= pageCount) page = 0;
		LoadPage(page);
	}

	void OnDisable() {
		control.UpdateCollider();
	}

	void OnPrevious()
	{
		page--;
		if(page < 0) page = pageCount - 1;
		LoadPage(page);
	}

	void OnApply()
	{
		control.UpdateCollider();
	}



}


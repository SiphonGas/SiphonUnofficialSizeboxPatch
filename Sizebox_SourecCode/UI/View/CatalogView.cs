using UnityEngine;
using UnityEngine.UI;

public class CatalogView : MonoBehaviour {
	GameObject placeholder;
	int thumbCount;
	GridLayoutGroup grid;
	InterfaceControl control;
	Image[] thumbs;
	EditPlacement placement;
	int currentCatalog = 0;
	Color originalColor;
	int page = 0;
	int pageCount = 0;
	Toggle playAsGiantess;

	
	void Start () {

		control = Camera.main.GetComponent<InterfaceControl>();
		placement = control.GetComponent<EditPlacement>();

		playAsGiantess = GetComponentInChildren<Toggle>();
		playAsGiantess.isOn = false;

		Button[] buttons = GetComponentsInChildren<Button>();
		buttons[0].onClick.AddListener(() => OnPrevious());
		buttons[1].onClick.AddListener(() => OnNext());

		placeholder = Resources.Load("UI/Button/ThumbPlaceholder") as GameObject;
		grid = GetComponentInChildren<GridLayoutGroup>();

		RectTransform rt = grid.GetComponent<RectTransform>();
		int maxColumns = (int) rt.rect.width / 72;
		int maxRows = (int) rt.rect.height / 84;

		thumbCount = maxColumns * maxRows;
		thumbs = new Image[thumbCount];

		for(int i = 0; i < thumbCount; i++)
		{
			GameObject go = Instantiate(placeholder, grid.transform) as GameObject;
			go.name = "Thumbnail " + i;
			thumbs[i] = go.GetComponent<Image>();
			originalColor = thumbs[i].color;
			Button button = go.GetComponent<Button>();
			int number = i;
			button.onClick.AddListener(() => OnElementClick(number));
		}

		SetCategory(currentCatalog);
		gameObject.SetActive(false);

		
	}

	public void OnMenuClick(int category)
	{
		if(category == currentCatalog)
			gameObject.SetActive(!gameObject.activeSelf);
		else {
			SetCategory(category);
			gameObject.SetActive(true);
		}
		
	}

	public void SetCategory(int category)
	{
		currentCatalog = category;
		page = 0;
		pageCount = (int) (control.catalog[currentCatalog].Length / thumbCount) + 1;
		LoadPage(page);
		
	}

	void LoadPage(int pag)
	{
		int baseItem = pag * thumbCount;
		for(int i = 0; i < thumbCount; i++)
		{
			int elem = baseItem + i;
			if(elem < control.catalog[currentCatalog].Length) {
				thumbs[i].sprite = control.catalog[currentCatalog][elem]; 
				thumbs[i].color = Color.white;
				thumbs[i].GetComponentInChildren<Text>().text = thumbs[i].sprite.name.Split('.')[0];
			} else {
				thumbs[i].sprite = null; 
				thumbs[i].color = originalColor;
				thumbs[i].GetComponentInChildren<Text>().text = "";
			}
			
		}
	}

	void OnElementClick(int i)
	{
		if(thumbs[i].sprite){
			string elementName = thumbs[i].sprite.name;
			switch(currentCatalog)
			{
				case 0:
					placement.AddGameObject(elementName);	
					break;
				case 1:
					if(playAsGiantess.isOn) {
						control.PlayAsGiantess(elementName);
					} else {
						placement.AddGiantess(elementName);
					}
					break;
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

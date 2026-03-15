using UnityEngine;
using UnityEngine.UI;

public class PoseView : MonoBehaviour {
	public CommandView command;
	GameObject placeholder;
	int thumbCount;
	GridLayoutGroup grid;
	InterfaceControl control;
	Image[] thumbs;
	EditPlacement placement;
	Color originalColor;
	int page = 0;
	int pageCount = 0;
	int poseCatalog = 2;
	int handleCount = 14;
	PoseHandle poseTarget;

	PoseHandle[] poseHandle;
	Camera mainCamera;

	
	void Start () {
		mainCamera = Camera.main;
		control = mainCamera.GetComponent<InterfaceControl>();
		placement = control.GetComponent<EditPlacement>();

		Button[] buttons = GetComponentsInChildren<Button>();
		buttons[0].onClick.AddListener(() => OnPrevious());
		buttons[1].onClick.AddListener(() => OnNext());
		buttons[2].onClick.AddListener(() => OnCustomize());

		placeholder = Resources.Load("UI/Button/ThumbPlaceholder") as GameObject;
		grid = GetComponentInChildren<GridLayoutGroup>();

		poseTarget = Resources.Load<PoseHandle>("UI/Pose/Pose Target");

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

		page = 0;
		pageCount = (int) (control.catalog[poseCatalog].Length / thumbCount) + 1;
		LoadPage(page);

		control.OnSelected += OnChangedCharacter;

		poseHandle = new PoseHandle[handleCount];
		for(int i = 0; i < handleCount; i++)
		{
			poseHandle[i] = Instantiate<PoseHandle>(poseTarget);
			poseHandle[i].gameObject.layer = Layers.auxLayer;
			poseHandle[i].transform.GetChild(0).gameObject.layer = Layers.auxLayer;
			poseHandle[i].gameObject.SetActive(false);
		}

		gameObject.SetActive(false);

		
	}

	void OnElementClick(int i)
	{
		if(thumbs[i].sprite){
			string elementName = thumbs[i].sprite.name;
			control.SetPose(elementName);
			EnablePosingMode(false);	
		}		
	}

	void OnChangedCharacter()
	{
		UnparentHandles();
		if(!gameObject.activeSelf) return;
		if(!control.giantess) {
			gameObject.SetActive(false);
			return;
		}			
	}

	void UnparentHandles() {
		if(poseHandle == null) return;
		for(int i = 0; i < handleCount; i++)
		{
			poseHandle[i].transform.SetParent(null);
		}
	}

	void PrepareCharacter() {

		PoseIK ik = control.giantess.ik.poseIK;
		float scale = control.giantess.Scale;

		Transform bodyTransform = poseHandle[0].transform;		
		poseHandle[0].SetEffector(ik.body, control.giantess.transform, scale);

		poseHandle[1].SetEffector(ik.leftHand, bodyTransform, scale);
		poseHandle[2].SetEffector(ik.rightHand, bodyTransform, scale);

		poseHandle[3].SetEffector(ik.leftFoot, control.giantess.transform, scale);
		poseHandle[4].SetEffector(ik.rightFoot, control.giantess.transform, scale);

		poseHandle[5].SetEffector(ik.leftShoulder, bodyTransform, scale);
		poseHandle[6].SetEffector(ik.rightShoulder, bodyTransform, scale);

		poseHandle[7].SetEffector(ik.leftThight, bodyTransform, scale);
		poseHandle[8].SetEffector(ik.rightThight, bodyTransform, scale);

		poseHandle[9].SetBendGoal(ik.leftElbow, bodyTransform, scale);
		poseHandle[10].SetBendGoal(ik.rightElbow, bodyTransform, scale);

		poseHandle[11].SetBendGoal(ik.leftKnee, control.giantess.transform, scale);
		poseHandle[12].SetBendGoal(ik.rightKnee, control.giantess.transform, scale);

		poseHandle[13].SetLookAt(ik.head, control.giantess.transform, scale);
	}

	void OnEnable() {
		if(placement) {
			placement.updateHandles = false;
		}
	}

	void OnDisable() {
		placement.updateHandles = true;
		control.UpdateCollider();
		control.commandEnabled = true;
		DisableHandles();
		UnparentHandles();

	}

	void DisableHandles() {
		if(poseHandle == null) return;
		for(int i = 0; i < handleCount; i++)
		{
			poseHandle[i].gameObject.SetActive(false);
		}
	}

	void Update() {
		bool hitUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
		if(!hitUI && Input.GetMouseButtonDown(0)) {
			Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, Mathf.Infinity, Layers.auxMask)) {
				Debug.Log("hit");
				placement.gizmo.SetTarget(hit.collider.transform.parent);							
			}
		}
	}

	void LoadPage(int pag)
	{
		int baseItem = pag * thumbCount;
		for(int i = 0; i < thumbCount; i++)
		{
			int elem = baseItem + i;
			if(elem < control.catalog[poseCatalog].Length) {
				thumbs[i].sprite = control.catalog[poseCatalog][elem]; 
				thumbs[i].color = Color.white;
				thumbs[i].GetComponentInChildren<Text>().text = thumbs[i].sprite.name.Split('.')[0];
			} else {
				thumbs[i].sprite = null; 
				thumbs[i].color = originalColor;
				thumbs[i].GetComponentInChildren<Text>().text = "";
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

	void OnCustomize()
	{
		EnablePosingMode(true);
	}

	public void EnablePosingMode(bool enableIK) {
		if(!control.giantess || !control.giantess.poseMode) enableIK = false;

		control.EnablePoseIK(enableIK);
		control.commandEnabled = !enableIK;

		if(enableIK) {
			PrepareCharacter();
		} else {
			DisableHandles();
			UnparentHandles();
		}
		

	}
}

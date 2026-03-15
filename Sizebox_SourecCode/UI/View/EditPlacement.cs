using UnityEngine;
using UnityEngine.UI;
using RuntimeGizmos;

public class EditPlacement : MonoBehaviour {
	public static EditPlacement Instance;
	// configure the UI

	// everything else
	public bool updateHandles = true;
	public static bool showHandles = true;
	public TransformGizmo gizmo;
	
	Camera cam;
	public GameObject cursor {get; private set;}
	private Vector3 cursorPosition = Vector3.zero;
	private Quaternion cursorRotation = Quaternion.identity;
	private bool recreateMesh = false;

	public enum State { Select, Idle, Move };
	public State state = State.Idle;

	// to keep track of the stuff
	SimCam simCamControl;

	// for the action selector..
	public GameObject actionPanel;
	public Button actionButtonPrefab;
	public InterfaceControl control {get; private set; }
	MainView view;


	// TODO: fix selection of the element...

	// Use this for initialization
	void Awake () {
		Instance = this;
		control = GetComponent<InterfaceControl>();

		simCamControl = gameObject.AddComponent<SimCam>();
		
		cam = GetComponent<Camera>();
		gizmo = cam.gameObject.AddComponent<TransformGizmo>();
	}

	void Start()
	{
		view = GetComponent<MainView>();

		cursor = Instantiate(Resources.Load<GameObject>("UI/Edit/Cursor"));
		cursor.transform.SetParent(view.editMode.transform);
	}

	void OnEnable()
	{
		if(cursor != null)
			cursor.gameObject.SetActive(true);
		simCamControl.Enable(); 
		gizmo.enabled = true;
	}

	void OnDisable()
	{
		if(cursor != null)
			cursor.gameObject.SetActive(false);
		simCamControl.Disable();
		gizmo.enabled = false;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		bool showHandlesThisState = false;
		// obtain the mouse position in world and save the cursor position
		if(control == null) {
			control = GetComponent<InterfaceControl>();
		} 
		
		


		Vector3 mousePositionInScreen = Input.mousePosition;
		RaycastHit hit;
		Ray ray = cam.ScreenPointToRay(mousePositionInScreen);
		if(Physics.Raycast(ray, out hit, Mathf.Infinity, Layers.placementMask)) {
			cursor.transform.localScale = Vector3.one * cam.nearClipPlane;
			cursorPosition = hit.point;
			cursorRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);


			cursor.transform.position = cursorPosition;
			cursor.transform.rotation = cursorRotation;
	
		}		

		switch(state)
		{
			case State.Idle:
			ShowCursor(true);
			showHandlesThisState = true;
			
			break;
			
			case State.Select:
			// it's a new element and you want to place it somewhere
			if(control.selectedEntity != null)
			{
				control.SetSelectedObject(null);
			} 
			else 
			{
				// move the cursor
				ShowCursor(true);
				// you click on a giantess character
				if(Input.GetMouseButtonDown(0) && hit.collider != null)
				{
					if(hit.collider.gameObject.layer == Layers.gtsBodyLayer) {
						GiantessBone gtsBone = hit.collider.gameObject.GetComponent<GiantessBone>();
						if(gtsBone) {
							control.SetSelectedObject(gtsBone.giantess);
							state = State.Idle;
							break;
						} 		
													
					} 
					// you click on an accesory. ie cities, cars, etc.
					if (hit.collider.GetComponent<Collider>()) 
					{
						// get the root of the element
						GameObject item = hit.collider.gameObject;
						while(item.transform.parent != null && item.GetComponent<EntityBase>() == null)
						{
							item = item.transform.parent.gameObject;
							
						}
						if(item != null && item.GetComponent<EntityBase>() != null) {
							state = State.Idle;
							control.SetSelectedObject( item.GetComponent<EntityBase>() );
						}
						else control.SetSelectedObject(null);
					}

				}
					
			}
			break;

			case State.Move:
				// MOVE the selected item
				ShowCursor(false);
				if(control.selectedEntity)
				{
					if(control.selectedEntity.isPositioned) control.selectedEntity.isPositioned = false;

					control.selectedEntity.Move(cursorPosition);


					if(hit.transform == null || hit.transform.gameObject.layer == Layers.mapLayer 
					|| hit.transform.gameObject.layer == Layers.defaultLayer) {
						control.selectedEntity.transform.SetParent(null);
					} else {
						control.selectedEntity.transform.SetParent(hit.transform, true);
					}
					
					// control.selectedObject.transform.position = cursorPosition;
					// copied form climb mode -- look at that to understand or improve this function
					if(!control.lockRotation && control.selectedEntity.rotationEnabled)
						control.selectedEntity.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(control.selectedEntity.transform.forward, hit.normal), hit.normal);


					if(Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
					{
						control.selectedEntity.SetCollider(true);
						if(control.giantess != null && recreateMesh) {
							control.giantess.UpdateAllColliders();
						}
						control.selectedEntity.isPositioned = true;
						state = State.Idle;
					}
				} else {
					state = State.Idle;
				}
				if(state == State.Idle) {
					// inform that GO is placed
					control.selectedEntity.Place();
				}
			break;
		} 

		if(showHandlesThisState && showHandles && control.selectedEntity) {
			gizmo.enabled = true;
			if(updateHandles)
				gizmo.SetTarget(control.selectedEntity.transform);
		} else {
			gizmo.enabled = false;
		}		
	}

	public void AddGameObject(string accesoryId)
	{
		Debug.Log("Adding Object: " + accesoryId);
		ClientPlayer.Instance.CmdSpawnObject(accesoryId, cursorPosition, Quaternion.identity, control.lastMicroScale);
		
		
	}

	public void OnObjectSpawned(EntityBase entity) {
		control.SetSelectedObject(entity);
		entity.SetCollider(false);	
		state = State.Move;
	}

	void LoadAccesory(GameController.SaveData.Accessory accesory)
	{
		Vector3 position = new Vector3(accesory.position.x, accesory.position.y, accesory.position.z);
		Quaternion rotation = Quaternion.Euler(accesory.rotation.x, accesory.rotation.y, accesory.rotation.z);
		ClientPlayer.Instance.CmdSpawnObject(accesory.name, position, rotation, accesory.scale);
		
	}

	public void AddGiantess(string gtsid)
	{
		ClientPlayer.Instance.CmdSpawnGiantess(gtsid, cursorPosition, Quaternion.identity, control.lastMacroScale);
	}

	public void OnGiantessSpawned(Giantess gts) {
		control.SetSelectedObject(gts);
		GiantessControl gtsControl = gts.GetComponent<GiantessControl>();
		gts.ChangeScale(control.lastMacroScale);
		if(gtsControl == null || gtsControl.enabled == false) {
			gts.MovableCollidersEnable(false);
			gts.SetCollider(false);
			recreateMesh = true;
			state = State.Move;
		}
		
	}

	public void ShowCursor(bool show)
	{
		cursor.SetActive(false);

	}

	public void MoveCurrentGO()
	{
		if(control.selectedEntity)
		{
			control.selectedEntity.SetCollider(false);
			state = State.Move;
		}
	}

	public void SelectMode()
	{
		control.SetSelectedObject(null);
		state = State.Select;
	}

	public void Delete()
	{
		control.DeleteObject();
		state = State.Select;
	}

	public void LoadElementsFromSaveData(GameController.SaveData data)
	{

		for(int i = 0; i < data.accesories.Length; i++)
		{
			LoadAccesory(data.accesories[i]);
		}
		for(int i = 0; i < data.giantesses.Length; i++)
		{
			LoadGiantess(data.giantesses[i]);
		}
	}

	void LoadGiantess(GameController.SaveData.Giantess giantess)
	{
		Vector3 position = new Vector3(giantess.position.x, giantess.position.y, giantess.position.z);
		Quaternion rotation = Quaternion.Euler(giantess.rotation.x, giantess.rotation.y, giantess.rotation.z);

		// connect these two
		GameObject go = null;
		ClientPlayer.Instance.CmdSpawnGiantess(giantess.name, position, rotation, giantess.scale);
		if(go == null) return;

		Debug.Log("Loaded Character: " + giantess.name);
		control.SetSelectedObject( go.GetComponent<EntityBase>());
		
		if(giantess.isMoving) control.SetAnimation(giantess.animation);
		else control.SetPose(giantess.animation);
		control.SetSelectedObject(null);
	}

}

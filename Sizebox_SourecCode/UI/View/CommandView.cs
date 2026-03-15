using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CommandView : MonoBehaviour {
	Camera mainCamera;
	GameObject actionPanel;
	Button actionButtonPrefab;
	List<Behavior> listPossibleBehaviors;
	InterfaceControl control;
	Vector3 cursorPosition;
	EntityBase targetEntity;
	float maxTimeBetweenClick = 0.2f;
	float startClick = 0f;
	EditPlacement placement;

	// Use this for initialization
	void Start () {
		mainCamera = Camera.main;
		control = mainCamera.GetComponent<InterfaceControl>();
		placement = mainCamera.GetComponent<EditPlacement>();

		actionButtonPrefab = Resources.Load<Button>("UI/Button/Action Button");
		Debug.Assert(actionButtonPrefab, "Action button prefab not found");
		actionPanel = transform.GetChild(0).gameObject;
		

	}
	
	// Update is called once per frame
	void Update () {
		if(!control.commandEnabled) return;
		// do click
		if(Input.GetMouseButtonDown(0)) {
			if(placement.state == EditPlacement.State.Idle)
				startClick = Time.time;
		}

		if(Input.GetMouseButtonUp(0) && Time.time - startClick < maxTimeBetweenClick)
		{
			// HideBehaviors();
			bool hitUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
			if(!hitUI)
			{
				if(control.selectedEntity != null && !control.selectedEntity.isPositioned) return;
				HideBehaviors();
				// check if is smartobject
				RaycastHit hitPoint;
				Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
				if(Physics.Raycast(ray, out hitPoint, Mathf.Infinity, Layers.actionSelectionMask)) {
					if(hitPoint.collider.gameObject.layer == Layers.uiLayer) return;
					cursorPosition = hitPoint.point;
					actionPanel.transform.position = Input.mousePosition;
					// get list possible BehaviorSelector
					// check if is a game object and check if has some actions to do
					targetEntity = FindEntity(hitPoint.transform);

					listPossibleBehaviors = GetListPossibleBehaviors(hitPoint, targetEntity);
					ShowBehaviorsInPanel(listPossibleBehaviors, targetEntity);					
				}
				
			}
				
		}
		
	}

	List<Behavior> GetListPossibleBehaviors(RaycastHit hit, EntityBase targetEntity)
	{
		List<Behavior> myBehaviours = new List<Behavior>();

		if(control.selectedEntity == null || control.selectedEntity.ai == null || BehaviorLists.Instance == null) return myBehaviours;

		List<Behavior> allBehaviors = new List<Behavior>();		

		if(targetEntity != null) {
			if(control.selectedEntity == targetEntity) allBehaviors.AddRange(BehaviorLists.GetBehaviors(EntityType.Oneself));
			else allBehaviors.AddRange(targetEntity.GetListBehaviors());
		}
		
		allBehaviors.AddRange(BehaviorLists.GetBehaviors(EntityType.None));
		// Debug.Log("Looking for type"); 
		List<EntityType> type = control.selectedEntity.GetTypesEntity();
		if(control.selectedEntity == targetEntity) {
			type.Add(EntityType.Oneself);
		}

		foreach (Behavior behavior in allBehaviors) {
			if(!behavior.hidden && type.Contains(behavior.agent)) {
				myBehaviours.Add(behavior);
			}
		}

		return myBehaviours;
	}


	EntityBase FindEntity(Transform hitElement) {
		if(hitElement.gameObject.layer == Layers.gtsBodyLayer) {
			GiantessBone gtsBone = hitElement.gameObject.GetComponent<GiantessBone>();
			if(gtsBone) return gtsBone.giantess;
		}
		EntityBase entity = null;
		Transform currentTransform = hitElement;
		while(entity == null && currentTransform != null)
		{
			entity = currentTransform.GetComponent<EntityBase>();
			currentTransform = currentTransform.parent;
		}
		return entity;
	}

	void ShowBehaviorsInPanel(List<Behavior> listBehaviors, EntityBase targetEntity)
	{
		// Debug.Log(listBehaviors.Count + " actions found.");
		int extraBehaviors = 1;
		if(targetEntity != null && targetEntity != control.selectedEntity) extraBehaviors++;
		// Debug.Log(extraBehaviors + " extra actions found");

		if(listBehaviors.Count == 0 && (targetEntity == null || extraBehaviors == 1)) 
		{
			return;
		}

		int totalButtons = listBehaviors.Count + extraBehaviors;
		// Debug.Log(totalButtons + " total buttons");
		 // add the cancel action and others
		for(int i = 0; i < totalButtons; i++)
		{
			Button actButton;
			actButton = Instantiate(actionButtonPrefab, actionPanel.transform, false) as Button;

			Text textComponent = actButton.transform.GetChild(0).GetComponent<Text>();

			if(i == listBehaviors.Count)
			{
				textComponent.text = "Cancel";
				actButton.onClick.AddListener(() => HideBehaviors());
				continue;
			} 

			if (i == listBehaviors.Count + 1)
			{
				if(targetEntity != null && targetEntity != control.selectedEntity) {
					textComponent.text = "Select";
					actButton.onClick.AddListener(() => OnSelectObject(targetEntity));
				}
				continue;
			}

			textComponent.text = listBehaviors[i].text;
			int number = i;
			actButton.onClick.AddListener(() => OnBehaviorClicked(number));
		}
	}

	void OnSelectObject(EntityBase entity)
	{
		control.SetSelectedObject(entity);
		HideBehaviors();
	}

	void OnBehaviorClicked(int number)
	{
		// Debug.Log("Clicked the option " + number);
		// take action and process it
		TellToDoBehavior(number);
		// hide all the buttons
		HideBehaviors();

	}

	void HideBehaviors()
	{
		listPossibleBehaviors = null;
		int cantButtons = actionPanel.transform.childCount;
		for (int i = 0; i < cantButtons; i++)
		{
			Button button = actionPanel.transform.GetChild(i).gameObject.GetComponent<Button>();
			button.onClick.RemoveAllListeners();
			// button.gameObject.SetActive(false);
			Destroy(button.gameObject);
		}
	}

	void TellToDoBehavior(int number)
	{
		EntityBase character = control.selectedEntity;
		if(character.ai != null && character.actionManager != null)
		{
			// create the action
			Behavior behavior = listPossibleBehaviors[number]; 
			// character.actionManager.DoInmediateBehavior(action);
			character.ai.DisableAI();
			character.ai.InmediateCommand(behavior, targetEntity, cursorPosition);

			// execute the manager
			// character.actionManager.Execute();			
		} else {
			Debug.Log("No character selected");
		}

	}

	public void BehaviorPanelEnabled(bool enabled)
	{
		actionPanel.SetActive(enabled);
	}
}



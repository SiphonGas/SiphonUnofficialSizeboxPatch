using UnityEngine;
using UnityEngine.UI;

public class EditView : MonoBehaviour {
	GameController controller;
	MainView mainView;
	GameObject button;
	Transform panel;
	Button playButton;
	public EditPlacement placement {get; private set;}
	GameObject transformView;
	CatalogView catalogView;
	AnimationView animationView;
	MorphsView morphsView;
	PoseView poseView;
	InterfaceControl control;

	CommandView actionSelector;

	// Use this for initialization
	void Start () {
		controller = Camera.main.GetComponent<GameController>();
		mainView = controller.view;
		placement = controller.GetComponent<EditPlacement>();
		control = controller.GetComponent<InterfaceControl>();

		transformView = Instantiate(Resources.Load("UI/Transform") as GameObject);
		transformView.transform.SetParent(transform, false);
		transformView.AddComponent<TransformView>();
		transformView.SetActive(false);

		// this object will auto disable himself after their Start function.
		catalogView = Instantiate(Resources.Load("UI/Catalog") as GameObject).AddComponent<CatalogView>();
		catalogView.transform.SetParent(transform, false);

		poseView = Instantiate(Resources.Load("UI/Pose/Pose") as GameObject).AddComponent<PoseView>();
		poseView.transform.SetParent(transform, false);

		animationView = Instantiate(Resources.Load("UI/Animation") as GameObject).AddComponent<AnimationView>();
		animationView.transform.SetParent(transform, false);

		morphsView = Instantiate(Resources.Load("UI/Morphs") as GameObject).AddComponent<MorphsView>();
		morphsView.transform.SetParent(transform, false);

		actionSelector = Instantiate(Resources.Load("UI/Command") as GameObject).AddComponent<CommandView>();
		actionSelector.transform.SetParent(transform, false);

		

		button = Resources.Load("UI/Button/EditButton") as GameObject;
		panel = transform.GetChild(0);

		// Play Button
		playButton = AddButton("Play");
		playButton.onClick.AddListener(() => mainView.ChangeMode());

		playButton = AddButton("Select");
		playButton.onClick.AddListener(() => placement.SelectMode());

		playButton = AddButton("Giantess");
		playButton.onClick.AddListener(() => OnCatalogClick(1));

		playButton = AddButton("Objects");
		playButton.onClick.AddListener(() => OnCatalogClick(0));

		playButton = AddButton("Move");
		playButton.onClick.AddListener(() => placement.MoveCurrentGO());

		playButton = AddButton("Transform");
		playButton.onClick.AddListener(() => OnTransformClick());

		playButton = AddButton("Animation");
		playButton.onClick.AddListener(() => OnAnimationClick());

		playButton = AddButton("Pose");
		playButton.onClick.AddListener(() => OnPoseClick());

		playButton = AddButton("Morphs");
		playButton.onClick.AddListener(() => OnMorphsClick());

		playButton = AddButton("Delete");
		playButton.onClick.AddListener(() => placement.Delete());

		playButton = AddButton("Menu");
		playButton.onClick.AddListener(() => controller.view.OpenPauseMenu());

	}

	public void OnTransformClick()
	{
		transformView.SetActive(!transformView.activeSelf);
	}

	public void OnCatalogClick(int category)
	{
		if(category == 2 && (control.selectedEntity == null || !control.selectedEntity.isGiantess)) return;

		animationView.gameObject.SetActive(false);
		morphsView.gameObject.SetActive(false);
		poseView.gameObject.SetActive(false);

		catalogView.OnMenuClick(category);
		transformView.SetActive(true);
	}

	public void OnAnimationClick()
	{
		if(control.humanoid == null) return;

		catalogView.gameObject.SetActive(false);		
		morphsView.gameObject.SetActive(false);
		poseView.gameObject.SetActive(false);

		animationView.gameObject.SetActive(!animationView.gameObject.activeSelf);
		transformView.SetActive(true);
	}

	public void OnPoseClick()
	{
		if(control.humanoid == null) return;

		catalogView.gameObject.SetActive(false);		
		morphsView.gameObject.SetActive(false);
		animationView.gameObject.SetActive(false);

		poseView.gameObject.SetActive(!poseView.gameObject.activeSelf);
		transformView.SetActive(true);
	}

	public void OnMorphsClick()
	{
		if(control.selectedEntity == null || !control.selectedEntity.isGiantess) return;
		
		animationView.gameObject.SetActive(false);
		catalogView.gameObject.SetActive(false);
		poseView.gameObject.SetActive(false);

		morphsView.gameObject.SetActive(!morphsView.gameObject.activeSelf);
	}
	

	public Button AddButton(string label)
	{
		GameObject buttonGameObject = Instantiate(button, panel) as GameObject;
		buttonGameObject.name = label;
		buttonGameObject.GetComponent<Text>().text = label;
		return buttonGameObject.GetComponent<Button>();
	}

}

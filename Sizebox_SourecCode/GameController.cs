using UnityEngine;
using System;
using UnityEngine.Networking;

public class GameController : MonoBehaviour {
	// layers
	// Mutiplayer: Add Chat and UserNames
	public static GameController Instance;
	public NetworkManager networkManager;
	public Pathfinder pathfinder;

	public static float globalSpeed = 1f;
	public static float startingSize = 1f;
	public static float referenceScale = 1000f;
	public static bool IsMacroMap = false;
	public static bool inputEnabled = false;
	public EventManager eventManager;
	public SharedKnowledge sharedKnowledge;
	public static Player playerInstance {get; private set;}
	public ClientPlayer spawner {get; private set;}

	bool paused = false;
	public bool Paused {
		get {
			return paused;
		}
		set {
			paused = value;
			if(paused) Time.timeScale = 0f;
			else Time.timeScale = 1f;
		}
	}


	[Serializable]
	public class SaveData {
		[Serializable]
		public class Vector{
			public float x;
			public float y;
			public float z;
		}
		[Serializable]
		public class Accessory {
			public string name;
			public Vector position;
			public Vector rotation;
			public float scale;
		}

		[Serializable]
		public class Giantess {
			public string name;
			public Vector position;
			public Vector rotation;
			public float scale;
			public bool isMoving;
			public string animation;
		}



		public int version;
		public string scene;
		public Accessory[] accesories;
		public Giantess[] giantesses;

	}
	EditPlacement editCamera;
	public IOManager modelManager {get; private set;}
	public MainView view { get; private set;}
	LuaManager luaManager;

	// Use this for initialization
	void Awake () {
		Instance = this;
		modelManager = IOManager.GetIOManager();
		if(NetworkManager.singleton) networkManager = NetworkManager.singleton;
		else networkManager = Instantiate(networkManager);
		pathfinder = new Pathfinder();
		eventManager = new EventManager();
		sharedKnowledge = new SharedKnowledge();
		PreferencesCentral.Initialize();
		
		Giantess.ignorePlayer = PreferencesCentral.ignorePlayer.value;
		MicroNPC.crushEnabled = PreferencesCentral.crushEnabled.value;

		gameObject.AddComponent<SoundManager>();
		// define all the annoying layers
		Paused = false;
		Layers.Initialize();

		editCamera = GetComponent<EditPlacement>();

		// new ui starts here
		view = gameObject.AddComponent<MainView>();		

		// new ui ends here

		// set normal gravity
		Physics.gravity = new Vector3(0,-9.8f,0);
		
	}

	public void RegisterPlayer(ClientPlayer player) {
		spawner = player;
		string modelName = StateManager.instance.myData.name;
		if(modelName == null || modelName.Length == 0) modelName = modelManager.GetRandomPlayerModelName();
		spawner.CmdSpawnPlayer(modelName);
		StateManager.instance.myData.name = modelName;
		inputEnabled = true;
	}

	public void SetPlayerInstance(Player player) {
		playerInstance = player;
	}

	void Start()
	{
		// load things after everything is awake
		if(modelManager.isLoadingData)
		{
			Debug.Log("Loading the data to the scene");
			SaveData loadedData = modelManager.LoadCachedDataFile();
			editCamera.LoadElementsFromSaveData(loadedData);
		}

		if(StateManager.instance.gameSettings.multiplayer == false) {
			NetworkManager.singleton.StartHost();
		} else {
			GetComponent<CenterOrigin>().enabled = false;
			NetworkManagerHUD hud = networkManager.GetComponent<NetworkManagerHUD>();
			hud.showGUI = true;
		}
	}

	public void OnPlayerStart() {
		if(luaManager == null) {
			luaManager = gameObject.AddComponent<LuaManager>();
		}
	}

	public void OnPauseClick()
	{
		Paused = !Paused;
		GameController.inputEnabled = !Paused;
	}

	public static void IncreaseSpeed()
	{
		globalSpeed /= 1.2f;
		ObjectManager.Instance.UpdateAllSpeeds();
	}

	public static void DecreaseSpeed()
	{
		globalSpeed *= 1.2f;
		ObjectManager.Instance.UpdateAllSpeeds();
	}

	public static void ChangeSpeed(float newSpeed) {
		globalSpeed = newSpeed;
		ObjectManager.Instance.UpdateAllSpeeds();
	}

	public static void SetSlowDown(bool value) {
		AnimationManager.slowdownWithSize = value;
		ObjectManager.Instance.UpdateAllSpeeds();
	}

	public void SaveScene(string filename)
	{
		/*
		SaveData savedata = new SaveData();
		savedata.version = 1;
		savedata.scene = SceneManager.GetActiveScene().name;
		if(editCamera != null) {
			int cantAccesories = editCamera.rootAccesories.childCount;
			int cantGiantess = editCamera.rootGiantess.childCount;

			Debug.Log("There are " + cantAccesories + " accesories");
			Debug.Log("There are " + cantAccesories + " giantesses");
			
			savedata.accesories = new SaveData.Accessory[cantAccesories];
			for (int i = 0; i < cantAccesories; i++) {
				SaveData.Accessory accesory = new SaveData.Accessory();
				Transform child = editCamera.rootAccesories.GetChild(i);

				accesory.name = child.name.Replace("(Clone)","");

				accesory.position = new SaveData.Vector();
				accesory.position.x = child.position.x;
				accesory.position.y = child.position.y;
				accesory.position.z = child.position.z;

				accesory.rotation = new SaveData.Vector();
				accesory.rotation.x = child.eulerAngles.x;
				accesory.rotation.y = child.eulerAngles.y;
				accesory.rotation.z = child.eulerAngles.z;

				accesory.scale = child.localScale.x;

				savedata.accesories[i] = accesory;

			}

			savedata.giantesses = new SaveData.Giantess[cantGiantess];
			for (int i = 0; i < cantGiantess; i++) {
				SaveData.Giantess giantess = new SaveData.Giantess();
				Transform child = editCamera.rootGiantess.GetChild(i);

				giantess.name = child.name.Replace("(Clone)","");

				giantess.position = new SaveData.Vector();
				giantess.position.x = child.position.x;
				giantess.position.y = child.position.y;
				giantess.position.z = child.position.z;

				giantess.rotation = new SaveData.Vector();
				giantess.rotation.x = child.eulerAngles.x;
				giantess.rotation.y = child.eulerAngles.y;
				giantess.rotation.z = child.eulerAngles.z;

				giantess.scale = child.localScale.x;
				
				GiantessController morphConfig = child.GetComponent<GiantessController>();
				giantess.isMoving = !morphConfig.poseMode;
				giantess.animation = morphConfig.currentAnimation;

				savedata.giantesses[i] = giantess;

			}

		}
		string json = JsonUtility.ToJson(savedata);
		modelManager.SaveDataFile(filename, json);
		*/

	}


	public static string ConvertScaleToHumanReadable(float scale) {
		scale = scale / referenceScale;
		string unit;
		if(scale >= 1000) {
			unit = " km";
			scale /= 1000;
		} else if(scale >= 1) {
			unit = " m";
		} else if(scale >= 0.01f) {
			unit = " cm";
			scale *= 100;
		} else {
			unit = " mm";
			scale *= 1000;
		}
		return scale.ToString("0.00") + unit;

	}

}

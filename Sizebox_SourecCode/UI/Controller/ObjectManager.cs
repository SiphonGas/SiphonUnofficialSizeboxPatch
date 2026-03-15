using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class ObjectManager : NetworkBehaviour {
	// Instantiate player in multiplayer games
	public static ObjectManager Instance;
	PlayerCamera playerCamera;
	MicroManager microManager;
	
	public Vector3 spawnPoint;
	
	public List<MicroNPC> maleMicroModels;
	public List<MicroNPC> femaleMicroModels;
	static IOManager modelManager;
	RuntimeAnimatorController microAnimatorController;
	static Dictionary<string, GameObject> cachedObjects;
	public List<VehicleEnterExit> vehicles;

	public Dictionary<int, Giantess> giantessList;
	int gtsIndex = 1;

	public delegate void ObjectEvent(int id);
    public event ObjectEvent OnGiantessAdd;
    public event ObjectEvent OnGiantessRemove;
	public static NetworkHash128 maleMicroAssetId;
	public static NetworkHash128 femaleMicroAssetId;

	enum MicroGender {Male, Female}

	public delegate GameObject SpawnDelegate(Vector3 position, NetworkHash128 assetId);
    public delegate void UnSpawnDelegate(GameObject spawned);

	// Use this for initialization
	void Awake () {
		Instance = this;
		playerCamera = Camera.main.GetComponent<PlayerCamera>();
		spawnPoint = playerCamera.transform.position;
		modelManager = IOManager.GetIOManager();

		microAnimatorController = (RuntimeAnimatorController) Resources.Load("TinyController", typeof(RuntimeAnimatorController));
		// first thing to do
		// load all the models in memory for future instanciation
		
		maleMicroModels = LoadMicroList(MicroGender.Male);
		femaleMicroModels = LoadMicroList(MicroGender.Female);

		cachedObjects = new Dictionary<string, GameObject>();
		vehicles = new List<VehicleEnterExit>();

		giantessList = new Dictionary<int, Giantess>();
		giantessList.Clear();
		gtsIndex = 1;

		
		// Player Spawner
		maleMicroAssetId = NetworkHash128.Parse("223234342");
		femaleMicroAssetId = NetworkHash128.Parse("675756754");
		ClientScene.RegisterSpawnHandler(maleMicroAssetId, SpawnMaleMicro, UnSpawnMicro);
		ClientScene.RegisterSpawnHandler(femaleMicroAssetId, SpawnFemaleMicro, UnSpawnMicro);

		foreach(KeyValuePair<NetworkHash128,string> pair in modelManager.hashPlayer) {
			ClientScene.RegisterSpawnHandler(pair.Key, SpawnPlayer, UnSpawnPlayer);
		}

		// Object Spawner
		foreach(KeyValuePair<NetworkHash128,string> pair in modelManager.hashObjects) {
			ClientScene.RegisterSpawnHandler(pair.Key, SpawnObject, UnSpawnObject);
		}

		// Giantess Spawner
		foreach(KeyValuePair<NetworkHash128,string> pair in modelManager.hashGiantess) {
			ClientScene.RegisterSpawnHandler(pair.Key, SpawnGiantess, UnSpawnGiantess);
		}

		foreach(KeyValuePair<NetworkHash128,string> pair in modelManager.hashPlayableGiantess) {
			ClientScene.RegisterSpawnHandler(pair.Key, SpawnPlayableGiantess, UnSpawnGiantess);
		}

		microManager = new MicroManager();

	}

	void Update() {
		microManager.Update();
	}

	public static GameObject LoadPlayer(string name) {
		
		GameObject player = modelManager.LoadPlayerModel(name);
		if(player == null) {
			Debug.Log("Player " + name + " not found, instancing random skin");
			player = modelManager.LoadRandomPlayerModel();
		}

		player.AddComponent<Player>();
		player.AddComponent<Destructor>();

		player.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
		NetworkTransform nt = player.AddComponent<NetworkTransform>();
		nt.sendInterval = 0.02f;
		nt.transformSyncMode = NetworkTransform.TransformSyncMode.SyncTransform;

		Animator anim = player.GetComponent<Animator>();
		NetworkAnimator nAnim = player.AddComponent<NetworkAnimator>();
		nAnim.animator = anim;

		int parameterCount = 10;
		for (int i = 0; i < parameterCount; i++)
 			 nAnim.SetParameterAutoSend(i, true);

		return player;

	}

	public GameObject SpawnPlayer(Vector3 position, NetworkHash128 assetId) {
		Debug.Log("Spawning Player " + assetId);
		string name;
		if(!modelManager.hashPlayer.TryGetValue(assetId, out name)) {
			Debug.Log("The player doesn't have the model trying to spawn");
		}

		return Instantiate<GameObject>(LoadPlayer(name), position, Quaternion.identity);
	}

    public void UnSpawnPlayer(GameObject spawned) {
		 Destroy(spawned);
	}

	public GameObject SpawnObject(Vector3 position, NetworkHash128 assetId) {
		Debug.Log("Spawning " + assetId.ToString());
		string id;
		if(!modelManager.hashObjects.TryGetValue(assetId, out id)) {
			Debug.Log("Spawned and object not avaivable for this user.");
			return null;
		}
		return Instantiate<GameObject>(LoadObject(id), position, Quaternion.identity);
	}

    public void UnSpawnObject(GameObject spawned) {
		spawned.GetComponent<EntityBase>()._DestroyObject(true);
	}


	static GameObject LoadGiantess(string id) {
		GameObject model;
		if(!cachedObjects.TryGetValue(id, out model)) {
			model = modelManager.LoadGiantessModel(id);
			if(model == null) {
				Debug.LogError(id + " not found.");
				return null;
			}
			
			model.AddComponent<GiantessIK>();
			model.AddComponent<Giantess>();
			model.AddComponent<GiantessControl>().enabled = false;
			model.AddComponent<ResizeCharacter>().enabled = false;

			model.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

			NetworkTransform nt = model.AddComponent<NetworkTransform>();
			nt.sendInterval = 0.01f;
			nt.transformSyncMode = NetworkTransform.TransformSyncMode.SyncTransform;

			NetworkAnimator nAnim = model.AddComponent<NetworkAnimator>();
			nAnim.animator = model.GetComponent<Animator>();

			int parameterCount = 3;
			for (int i = 0; i < parameterCount; i++)
				nAnim.SetParameterAutoSend(i, true);

			cachedObjects.Add(id, model);
		}
		return model;
	}

	public GameObject InstantiateGiantess(string id, Vector3 position, Quaternion rotation, float scale = 1.0f)
	{

		GameObject model = LoadGiantess(id);
		if(model == null)
		{
			Debug.Log("Can't find " + id);
			return null;
		}
		
		GameObject go = Instantiate(model, position, rotation) as GameObject;
		GiantessPostLoad(go);
		go.transform.localScale = Vector3.one * scale;			
		return go;
	}

	public GameObject InstantiatePlayableGiantess(string id, Vector3 position, float scale) {
		GameObject giantess = InstantiateGiantess(id, position, Quaternion.identity, scale);
		return MakeGTSPlayable(giantess);
	} 

	public GameObject MakeGTSPlayable(GameObject giantess) 
	{
		giantess.GetComponent<GiantessControl>().enabled = true;
		ResizeCharacter resizer = giantess.GetComponent<ResizeCharacter>();
		resizer.enabled = true;
		resizer.sizeChangeRate = 0.2f;		
		Giantess gts = giantess.GetComponent<Giantess>();
		gts.ai.DisableAI();
		return giantess;
	}

	void GiantessPostLoad(GameObject go) {
		Giantess gts = go.GetComponent<Giantess>();
		gts.id = gtsIndex;
		giantessList[gtsIndex] = gts;
		if(OnGiantessAdd != null) {
			OnGiantessAdd(gtsIndex);
		}
		gtsIndex++;
	}

	public GameObject SpawnGiantess(Vector3 position, NetworkHash128 assetId) {
		Debug.Log("Spawning GTS " + assetId.ToString());
		string id;
		if(!modelManager.hashGiantess.TryGetValue(assetId, out id)) {
			Debug.Log("Spawned giantess not avaivable for this user.");
			return null;
		}
		GameObject go = Instantiate<GameObject>(LoadGiantess(id), position, Quaternion.identity);
		GiantessPostLoad(go);
		return go;
	}

	public GameObject SpawnPlayableGiantess(Vector3 position, NetworkHash128 assetId) {
		Debug.Log("Spawning GTS " + assetId.ToString());
		string id;
		if(!modelManager.hashPlayableGiantess.TryGetValue(assetId, out id)) {
			Debug.Log("Spawned giantess not avaivable for this user.");
			return null;
		}
		GameObject go = Instantiate<GameObject>(LoadGiantess(id), position, Quaternion.identity);
		GiantessPostLoad(go);
		return MakeGTSPlayable(go);
	}

    public void UnSpawnGiantess(GameObject spawned) {
		spawned.GetComponent<Giantess>()._DestroyObject(true);
	}


	// ========================= MICRO ===========================// 
	List<MicroNPC> LoadMicroList(MicroGender gender)
	{
		// 1. get list of gameobjects
		List<GameObject> modelList;
		if(gender == MicroGender.Male) {
			modelList = modelManager.GetMaleMicroModels();
		} else {
			modelList = modelManager.GetFemaleMicroModels(); 
		}
		if(modelList != null) {
			List<MicroNPC> microList = new List<MicroNPC>();
			// 2. iterate on the game objects, put the scripts, and save it in the main list
			for(int i = 0; i < modelList.Count; i++)
			{
				GameObject model = modelList[i];

				model.layer = Layers.microLayer;

				Animator anim = model.GetComponent<Animator>();
				// anim.cullingMode = AnimatorCullingMode.CullCompletely;
				anim.runtimeAnimatorController = microAnimatorController;

				SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
				foreach(SkinnedMeshRenderer renderer in renderers) {
					renderer.quality = SkinQuality.Bone1;
					renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
					renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
					renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
					renderer.skinnedMotionVectors = false;
				}

				CapsuleCollider capsuleCollider = model.AddComponent<CapsuleCollider>();
				capsuleCollider.center = new Vector3(0,0.8f,0);
				capsuleCollider.radius = 0.3f;
				capsuleCollider.height = 1.6f;

				MicroNPC microController = model.AddComponent<MicroNPC>();
				microController.rbody = model.AddComponent<Rigidbody>();
				microController.rbody.freezeRotation = true;
                microController.rbody.mass = 0.5f;

				model.AddComponent<Gravity>();

				microList.Add(microController);
			}
			return microList;
		}
		return null;
	}

	float microSpawnScale = 1;

	public GameObject InstantiateMicro(MicroNPC tinyAi, Vector3 position, Quaternion rotation, float scale = 1.0f)
	{
		Micro micro = Instantiate<Micro>(tinyAi, position, rotation);
		if(micro == null) return null;
		microSpawnScale = scale;
		microManager.AddMicro(micro);
		return micro.gameObject;
	} 

	public GameObject SpawnMaleMicro(Vector3 position, NetworkHash128 assetId) {
		Micro model = maleMicroModels[Random.Range(0, maleMicroModels.Count)];
		return SpawnMicro(position, model);
	}

	public GameObject SpawnFemaleMicro(Vector3 position, NetworkHash128 assetId) {
		Micro model = femaleMicroModels[Random.Range(0, femaleMicroModels.Count)];
		return SpawnMicro(position, model);
	}

	public GameObject SpawnMicro(Vector3 position, Micro model) {
		Micro micro = Instantiate<Micro>(model, position, Quaternion.identity);
		microManager.AddMicro(micro);
		return micro.gameObject;
	}

    public void UnSpawnMicro(GameObject spawned) {
		microManager.RemoveMicro(spawned.GetComponent<Micro>().id);
		Destroy(spawned);
	}

	public void OnMicroNPCSpawned(MicroNPC micro) {
		micro.ChangeScale(microSpawnScale);
	}


// ============================ OBJECTS ================================== // 



	static GameObject LoadObject(string id) {
		GameObject model;
		if(!cachedObjects.TryGetValue(id, out model)) {
			model = modelManager.LoadObject(id);
			if(model == null) {
				Debug.LogError(id + " not found.");
				return null;
			}
			
			model.layer = Layers.objectLayer;
			if(model.GetComponent<EntityBase>() == null) {
				model.AddComponent<EntityBase>();
			}
			model.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
			cachedObjects.Add(id, model);
		}
		return model;
	}

	public GameObject InstantiateObject(string id, Vector3 position, Quaternion rotation, float scale = 1.0f) {
		// load the model
		GameObject model = LoadObject(id);

		if(model == null) {
			Debug.LogError("Can't instante " + id + "(object could not be loaded)");
			return null;
		}

		GameObject go = Instantiate(model, position, rotation) as GameObject;
		go.transform.localScale = Vector3.one * scale;		
		return go;
	}

	public void OnObjectSpawned(EntityBase entity) {
		VehicleEnterExit vehicle = entity.GetComponent<VehicleEnterExit>();
		if(vehicle) vehicles.Add(vehicle);
	}

	// =================================== END OBJECTS ==================================== //

	public static NetworkHash128 GetHash(string name) {
		if(name == null) Debug.LogError("GetHash: Empty String");
		int hashCode = name.GetHashCode();
		string stringHash = hashCode.ToString("x");
		return NetworkHash128.Parse(stringHash);
	}

	public void UpdateAllSpeeds()
	{
		foreach(KeyValuePair<int,Giantess> giantess in giantessList) {
			if(giantess.Value != null) {
				giantess.Value.movement.anim.UpdateAnimationSpeed();
			} else {
				Debug.LogError("Null giantess in list");
			}
		}
	}

	public void RemoveGiantess(int id) {
		if(!giantessList.ContainsKey(id)) return;

		giantessList.Remove(id);
		if(OnGiantessRemove != null) {
			OnGiantessRemove(id);
		}
	}
	
}

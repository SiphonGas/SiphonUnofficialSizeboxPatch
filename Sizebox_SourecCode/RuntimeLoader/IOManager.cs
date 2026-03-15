using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public class IOManager : MonoBehaviour {
	// shader names
	string standard = "Standard";
	string mmd = "MMD";
	string _mode = "_Mode";

	AudioLoader audioLoader;

	private static IOManager iomanager;
	List<string> playerModelList;
	List<string> gtsModelList;
	List<string> maleMicroList;
	List<string> femaleMicroList;
	List<string> objectList;
	List<Sprite> giantessThumbnailList;
	List<Sprite> objectThumbList;
	public Dictionary<string, RuntimeAnimatorController> animationControllers;
	string[] animationList;

	string folderGTSModels;
	string folderPlayerModels;
	string folderSaves;
	string folderMaleNPC;
	string folderFemaleNPC;
	string folderObjects;
	string folderScreenshots;
	string folderSounds;
	public Dictionary<NetworkHash128, string> hashPlayer;
	public Dictionary<NetworkHash128, string> hashObjects;
	public Dictionary<NetworkHash128, string> hashGiantess;
	public Dictionary<NetworkHash128, string> hashPlayableGiantess;
	GameController.SaveData saveData;
	public bool isLoadingData = false;

	static RuntimeAnimatorController playerRuntimeControl;

	public static Shader standardShader;
	public static Shader standardCullOffShader;

	public enum BlendMode { Opaque, Cutout, Fade, Transparent}
	

	public static IOManager GetIOManager()
	{
		if(iomanager != null)
			return iomanager;
		else
		{
			Layers.Initialize();
			iomanager = new GameObject("IOManager").AddComponent<IOManager>();
			Object.DontDestroyOnLoad(iomanager.gameObject);
			if(playerRuntimeControl == null) {
				playerRuntimeControl = (RuntimeAnimatorController) Resources.Load("CharacterController", typeof(RuntimeAnimatorController));
				standardShader = Shader.Find("Standard");
				standardCullOffShader = Shader.Find("StandardCullOff");
			}
			return iomanager;
		}
	}


	void Awake()
	{
		hashObjects = new Dictionary<NetworkHash128, string>();
		hashGiantess = new Dictionary<NetworkHash128, string>();
		hashPlayableGiantess = new Dictionary<NetworkHash128, string>();
		hashPlayer = new Dictionary<NetworkHash128, string>();

		audioLoader = new AudioLoader();
		


		// get and create all the folders
		folderGTSModels = GetDirectory("Models/Giantess");
		folderMaleNPC = GetDirectory("Models/MaleNPC");
		folderFemaleNPC = GetDirectory("Models/FemaleNPC");
		folderSaves = GetDirectory("SavedScenes");
		folderPlayerModels = GetDirectory("Models/Player");
		folderObjects = GetDirectory("Models/Objects");
		folderScreenshots = GetDirectory("Screenshots");
		folderSounds = GetDirectory("Sounds");

		// get files in models folder
		string[] filesModels = GetFileList(folderGTSModels);
		string[] filesPlayer = GetFileList(folderPlayerModels);
		string[] filesMaleMicro = GetFileList(folderMaleNPC);
		string[] filesFemaleMicro = GetFileList(folderFemaleNPC);
		string[] filesObjects = GetFileList(folderObjects);

		animationControllers = new Dictionary<string, RuntimeAnimatorController>();
		List<string> defaultAnimations = Giantess.GetAnimationList();
		foreach(string clip in defaultAnimations) {
			if(clip.StartsWith("_")) continue;
			animationControllers[clip] = Giantess.giantessAnimatorController;
		}
		// animationControllers["Walk"] = Giantess.giantessAnimatorController;

		// create the list of micro files
		playerModelList = GetListMicroModels(filesPlayer);
		maleMicroList = GetListMicroModels(filesMaleMicro);
		femaleMicroList = GetListMicroModels(filesFemaleMicro);

		// save the player hashes
		foreach(string playerModel in playerModelList) {			
			hashPlayer[ObjectManager.GetHash(playerModel)] = playerModel;
		}

		// create the list of giantess sprites
		gtsModelList = GetGiantessModelList(filesModels);
		objectList = GetObjectsModelList(filesObjects);

		// register all prefabs
		GameObject[] objectPrefabs = Resources.LoadAll<GameObject>("Stuff");
		foreach(GameObject go in objectPrefabs) {
			ClientScene.RegisterPrefab(go);
		}

		StartCoroutine(LoadSoundFiles());
	}

	public static string[] GetFileList(string folder) {
		string[] directories = System.IO.Directory.GetDirectories(folder);
		List<string> files = new List<string>();
		foreach(string subdirectory in directories) {
			string[] fileList = GetFileList(subdirectory);
			foreach(string file in fileList) {
				files.Add(file);
			}
		}
		string[] thisFolderFiles = System.IO.Directory.GetFiles(folder);
		foreach(string file in thisFolderFiles) {
			files.Add(file);
		}
		return files.ToArray();
	}
	

	public void SaveScreenshot() {
		string date = System.DateTime.Now.ToString();
		date = date.Replace("/", "-");
		date = date.Replace(":", ".");
		string filename = "Screenshot " + date + ".png";
		Debug.Log("Taken Screenshot: " + filename);
		Application.CaptureScreenshot(folderScreenshots + filename);
	}

	

	

	public List<GameObject> GetMaleMicroModels() {
		return SearchMicroModels(folderMaleNPC, maleMicroList);
	}

	public List<GameObject> GetFemaleMicroModels() {
		return SearchMicroModels(folderFemaleNPC, femaleMicroList);
	}

	public List<GameObject> SearchMicroModels(string folder, List<string> microList) {
		List<GameObject> microModels = new List<GameObject>();
		for(int i = 0; i < microList.Count; i++)
		{
			string[] modelBundleNames = microList[i].Split('/');
			string bundleName = modelBundleNames[0];
			string modelName = modelBundleNames[1];
			
			AssetBundle bundle = AssetBundle.LoadFromFile(folder + bundleName);
			if(bundle != null)
			{
				GameObject model = bundle.LoadAsset(modelName) as GameObject;
				LayerMask layer = Layers.microLayer;

				StringHolder shaderList = bundle.LoadAsset(modelName + "_shaders") as StringHolder;
				if(shaderList != null) {
					LoadModelShaders(model, shaderList.content, layer);
				} else {
					shaderList = bundle.LoadAsset("shaders") as StringHolder;
					if(shaderList != null) {
						LoadModelShaders(model, shaderList.content, layer);
					} else {
						ApplyShader(model, standardShader, layer);
					}					
				}

				microModels.Add(model);
				bundle.Unload(false);				
			}
		
		}
		return microModels;
	}




	

	

	public void SaveDataFile(string filename, string content)
	{
		string path = folderSaves + filename + ".save";
		TextWriter tw = new StreamWriter(path);
		tw.Write(content);
		tw.Close();
	}

	public GameController.SaveData LoadDataFile(string filename)
	{
		string path = folderSaves + filename + ".save";
		string text = System.IO.File.ReadAllText(path);
		saveData = JsonUtility.FromJson<GameController.SaveData>(text);
		isLoadingData = true;
		return saveData;
	}

	public GameController.SaveData LoadCachedDataFile()
	{
		isLoadingData = false;
		return saveData;
	}

	public string[] GetListSavedFiles() {
		string[] files = System.IO.Directory.GetFileSystemEntries(folderSaves);
		for(int i = 0; i < files.Length; i++)
		{
			string[] subfolders = files[i].Split('/');
			files[i] = subfolders[subfolders.Length-1].Replace(".save","");
		}
		return files;
	}

	List<string> GetListMicroModels(string[] files)
	{
		List<string> microModelList = new List<string>();
		for(int i = 0; i < files.Length; i++) {
			if(files[i].EndsWith(".micro")) {
				AssetBundle bundle = AssetBundle.LoadFromFile(files[i]);
				if(bundle != null) {
					string[] folders = files[i].Split('/');
					string bundleName = folders[folders.Length - 1];
					StringHolder modelsInside = bundle.LoadAsset("modellist") as StringHolder;
					if(modelsInside != null) {
						for(int j = 0; j < modelsInside.content.Length; j++) {
							string modelName = modelsInside.content[j];
							microModelList.Add(bundleName + "/" + modelName);
						}
					}
					bundle.Unload(false);
				}
			}
		}
		return microModelList;
	}

	// PLAYER

	public List<GameObject> GetPlayerModels()
	{
		List<GameObject> playerModels = new List<GameObject>();
		for(int i = 0; i < playerModelList.Count; i++)
		{
			playerModels.Add(LoadPlayerModel(playerModelList[i]));		
		}
		return playerModels;
	}

	public GameObject LoadRandomPlayerModel()
	{
		string modelName = GetRandomPlayerModelName();
		return LoadPlayerModel(modelName);
	}

	public string GetRandomPlayerModelName()
	{
		int i = Random.Range(0, playerModelList.Count);
		return playerModelList[i];
	}

	public GameObject LoadPlayerModel(string name) {
		if(name == null || name.Length == 0) return null;
		Debug.Log("Loading Player: " + name);
		if(!playerModelList.Contains(name)) {
			Debug.LogWarning(name + " model is not available in this computer.");
			return null;
		}
		string[] modelBundleNames = name.Split('/');
		string bundleName = modelBundleNames[0];
		string modelName = modelBundleNames[1];

		AssetBundle bundle = AssetBundle.LoadFromFile(folderPlayerModels + bundleName);
		if(bundle != null)
		{
			GameObject model = bundle.LoadAsset(modelName) as GameObject;
			model.name = name;
			LayerMask layer = Layers.playerLayer;
			StringHolder shaderList = bundle.LoadAsset(modelName + "_shaders") as StringHolder;
			if(shaderList != null) {
				LoadModelShaders(model, shaderList.content, layer);
			} else {
				shaderList = bundle.LoadAsset("shaders") as StringHolder;
				if(shaderList != null) {
					LoadModelShaders(model, shaderList.content, layer);
				} else {
					ApplyShader(model, standardShader, layer);
				}					
			}

			Animator anim = model.GetComponent<Animator>();
			anim.runtimeAnimatorController = playerRuntimeControl;
			bundle.Unload(false);

			return model;				
		}
		return null;
	}


	// GIANTESS
	List<string> GetGiantessModelList(string[] files)
	{
		List<string> modelList = new List<string>();

		giantessThumbnailList = GetThumbnails(files, ".gts");
		
		foreach(Sprite sp in giantessThumbnailList) {
			modelList.Add(sp.name);
			hashGiantess[ObjectManager.GetHash(sp.name)] = sp.name;
			hashPlayableGiantess[ObjectManager.GetHash(sp.name + "_")] = sp.name;
		}

		return modelList;
	}

	public GameObject LoadGiantessModel(string name)
	{
		if(gtsModelList.Contains(name)) 
		{
			GameObject model = null;
			string[] modelBundleNames = name.Split('/');
			string bundleName = modelBundleNames[0];
			string modelName = modelBundleNames[1];
			AssetBundle bundle = AssetBundle.LoadFromFile(folderGTSModels + bundleName);
			if(bundle != null)
			{
				model = bundle.LoadAsset(modelName + "Prefab") as GameObject;
				LayerMask layer = Layers.objectLayer;

				StringHolder shaderList = bundle.LoadAsset(modelName + "_shaders") as StringHolder;
				if(shaderList != null) {
					LoadModelShaders(model, shaderList.content, layer);
				} else {
					shaderList = bundle.LoadAsset("shaders") as StringHolder;
					if(shaderList != null) {
						LoadModelShaders(model, shaderList.content, layer);
					} else {
						ApplyShader(model, standardCullOffShader, layer);
					}					
				}			

				Object modelBytes = bundle.LoadAsset(modelName + ".model.bytes");
				Object indexBytes = bundle.LoadAsset(modelName + ".index.bytes");
				Object vertexBytes = bundle.LoadAsset(modelName + ".index.bytes");
				if(modelBytes != null || indexBytes != null || vertexBytes != null) {
					MMD4MecanimModel mmd4mecanim = model.AddComponent<MMD4MecanimModel>();				
					mmd4mecanim.indexFile = indexBytes as TextAsset;
					mmd4mecanim.modelFile = modelBytes as TextAsset;
					mmd4mecanim.vertexFile = vertexBytes as TextAsset;
				}				
				model.name = name;
				bundle.Unload(false);
			}
			return model;
		}
		else
		{
			Debug.Log("There is not a model called " + name);
			return null;
		}
	}

	public Sprite[] GetGtsThumbnails()
	{
		return giantessThumbnailList.ToArray();

	} 

	// OBJECTS

	List<string> GetObjectsModelList(string[] files)
	{
		List<string> modelList = new List<string>();

		objectThumbList = GetThumbnails(files, ".object");
		
		foreach(Sprite sp in objectThumbList) {
			modelList.Add(sp.name);
			hashObjects[ObjectManager.GetHash(sp.name)] = sp.name;
		}

		return modelList;
	}

	GameObject LoadObjectModel(string id)
	{
		GameObject model = null;

		string[] modelBundleNames = id.Split('/');
		string bundleName = modelBundleNames[0];
		string modelName = modelBundleNames[1];

		AssetBundle bundle = AssetBundle.LoadFromFile(folderObjects + bundleName);
		if(bundle != null)
		{
			model = bundle.LoadAsset(modelName) as GameObject;
			if(model == null) {
				Debug.LogError("Model " + modelName + " in " + id + " can't be loaded");
				return null;
			}
			
			// load the shaders
			StringHolder shaderList = bundle.LoadAsset(modelName + "_shaders") as StringHolder;
			if(shaderList != null) {
				Debug.Log("Model Shaders");
				LoadObjectShaders(model, shaderList.content);
			} else {
				shaderList = bundle.LoadAsset("shaders") as StringHolder;
				if(shaderList != null) {
					Debug.Log("Bundle Shaders");
					LoadObjectShaders(model, shaderList.content);
				} 					
			}

			// add mesh colliders
			MeshFilter[] meshes = model.GetComponentsInChildren<MeshFilter>();
			for(int i = 0; i < meshes.Length; i++) {
				meshes[i].gameObject.layer = Layers.objectLayer;
				if(meshes[i].gameObject.GetComponent<MeshCollider>() == null) {
					meshes[i].gameObject.AddComponent<MeshCollider>();
				}
			}
			bundle.Unload(false);				
		}
		return model;
	}

	public GameObject LoadObject(string id)
	{
		if(objectList.Contains(id)) {
			return LoadObjectModel(id);
		}
		return Resources.Load<GameObject>("Stuff/" + id);
	}

	void LoadObjectShaders(GameObject model, string[] shaderList)
	{
		if(model == null) {
			Debug.LogError("Model is null");
			return;
		}
		MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
		int i = 0;
		foreach(MeshRenderer renderer in renderers) {
			foreach(Material material in renderer.sharedMaterials) {
				material.shader = GetShader(shaderList[i]);
				if(shaderList[i].Contains("Standard")) {
					BlendMode mode = (BlendMode) (int) material.GetFloat("_Mode");
					SetupMaterialWithBlendMode(material, mode);
				}
				i++;
			}
		}
	}

	public Sprite[] GetObjectsThumbnails()
	{
		Sprite[] resources = Resources.LoadAll<Sprite>("StuffThumbs");
		Sprite[] allSprites = new Sprite[resources.Length + objectList.Count];
		for(int i = 0; i < allSprites.Length; i++) {
			if(i < resources.Length) {
				allSprites[i] = resources[i];
			} else {
				allSprites[i] = objectThumbList[i - resources.Length];
			}
		}

		return allSprites;

	} 

	
	// COMMON
	void LoadModelShaders(GameObject model, string[] shaderList, LayerMask layer)
	{
		Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
		int i = 0;
		foreach(Renderer renderer in renderers) {
			// Debug.Log("Skinned Mesh: " + renderer.name);
			renderer.gameObject.layer = layer;
			foreach(Material material in renderer.sharedMaterials) {
				if(i > shaderList.Length) i--;
				material.shader = GetShader(shaderList[i]);
				if(shaderList[i].Contains(standard)) {
					BlendMode mode = (BlendMode) (int) material.GetFloat(_mode);
					SetupMaterialWithBlendMode(material, mode);
				} else if(shaderList[i].Contains(mmd)) {
					Color color = material.GetColor("_Ambient");
					color *= 1.5f;
					material.SetColor("_Ambient", color);
					// material.SetFloat(_ambientRate, 0.8f);
				}
				i++;
			}
		}
	}

	Shader GetShader(string name) {
		Shader shader = Shader.Find(name);
		if(shader != null) {
			return shader;
		} else {
			return standardCullOffShader;
		}
	}

	List<Sprite> GetThumbnails(string[] files, string extension)
	{
		List<Sprite> thumbnailList = new List<Sprite>();
		for(int i = 0; i < files.Length; i++) {
			if(files[i].EndsWith(extension)) {
				AssetBundle bundle = AssetBundle.LoadFromFile(files[i]);
				if(bundle != null) {
					try {
						string[] folders = files[i].Split('/');
						string bundleName = folders[folders.Length - 1];
						StringHolder modelsInside = bundle.LoadAsset("modellist") as StringHolder;
						if(modelsInside != null) {
							for(int j = 0; j < modelsInside.content.Length; j++) {
								string modelName = modelsInside.content[j];
								string modelIdentifier = bundleName + "/" + modelName;

								Texture2D texture = bundle.LoadAsset(modelName + "Thumb") as Texture2D;
								if(texture != null)
								{
									Sprite sprite = Sprite.Create(texture, new Rect(0,0,texture.width, texture.height), Vector2.zero);
									sprite.name = modelIdentifier;
									thumbnailList.Add(sprite);
								} else {
									Debug.LogError("Error loading the texture of the model " + modelName);
								}

								
							}
						}

						RuntimeAnimatorController runtimeAnim = bundle.LoadAsset<RuntimeAnimatorController>("animationController");
						if(runtimeAnim) {
							Debug.Log("Animator controller found in " + bundleName);
							AnimationClip[] animationClips = runtimeAnim.animationClips;
							foreach(AnimationClip clip in animationClips) {
								animationControllers[clip.name] = runtimeAnim;
							}
							
						}
						bundle.Unload(false);

					} catch(System.Exception e) {
						Debug.LogException(e, this);
						Debug.LogError("Error loading model: " + files[i]);
					}
					
				}
			}
		}
		return thumbnailList;
	}

	string GetDirectory(string path)
	{
		string newPath = Application.dataPath + "/../" + path + "/";
		if(!Directory.Exists(path)) {
			Directory.CreateDirectory(path);
		}
		return newPath;
	}

	public class AnimationData {
		public string showName;
		public string internalName;

		public AnimationData(string s, string i) 
		{
			showName = s;
			internalName = i;
		}
	}

	public string[] GetAnimationList()
	{
		if(animationList == null)
		{
			
			List<string> animData = new List<string>();

			foreach(KeyValuePair<string, RuntimeAnimatorController> anim in animationControllers) {
				animData.Add(anim.Key);
			}

			animData.Sort();
			animationList = animData.ToArray();
		}
		return animationList;
	}

	// SHADER THINGS
	public static void ApplyShader(GameObject go, Shader shader, LayerMask layer)
	{
		SkinnedMeshRenderer[] renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
		Dictionary<int, bool> transparent = new Dictionary<int, bool>();

		foreach(SkinnedMeshRenderer renderer in renderers) 
		{
			renderer.gameObject.layer = layer;
			Material[] material = renderer.sharedMaterials;

			foreach(Material mat in material) {
				string materialName = mat.name;
				
				// Debug.Log(materialName);

				mat.shader = shader;

				int mode = (int) mat.GetFloat("_Mode");
				if (materialName.Contains("eyes") && !materialName.Contains("_")){
					SetupMaterialWithBlendMode(mat, BlendMode.Opaque);
				}

				else if( materialName.Contains("eye") || materialName.Contains("matuge") || materialName.Contains("hitomi")  
				|| materialName.Contains("matsuge") | materialName.Contains("shadow") || materialName.Contains("shadou") 
				|| materialName.Contains("scar") || materialName.Contains("kage") || materialName.Contains("hairaito") 
				|| materialName.Contains("kizu") || materialName.Contains("mune2") ){
					// Debug.Log("Set as Fade: " + materialName);
					SetupMaterialWithBlendMode(mat, BlendMode.Fade);
				}

				else if (materialName.Contains("Lens") || materialName.Contains("renzu")) {
					// Debug.Log("Set as Transparent: " + materialName);
					SetupMaterialWithBlendMode(mat, BlendMode.Transparent);
				}
				else if (materialName.Contains("Face")) {
					// Debug.Log("Set as Transparent: " + materialName);
					SetupMaterialWithBlendMode(mat, BlendMode.Cutout);
				}					
				else {

					BlendMode blendmode = (BlendMode) mode;
					bool isTransparent = false;

					if(blendmode == BlendMode.Opaque && mat.mainTexture != null) {
						int instanceID = mat.mainTexture.GetInstanceID();
						if(!transparent.TryGetValue(instanceID, out isTransparent)) {
							isTransparent = IsTransparent(mat.mainTexture);
							transparent.Add(instanceID, isTransparent);
						}
					}

					if(isTransparent) {
						SetupMaterialWithBlendMode(mat, BlendMode.Cutout);
						
					} else {
						SetupMaterialWithBlendMode(mat, blendmode);							
					}
					
				}
			}
		}
	}

	static string sMode = "_Mode";
	static string sSrcBlend = "_SrcBlend";
	static string sDstBlend = "_DstBlend";
	static string sAlphaTestOn = "_ALPHATEST_ON";
	static string sAlphaBlendOn = "_ALPHABLEND_ON";
	static string sAlphaPremultipyOn = "_ALPHAPREMULTIPLY_ON";
	static string sZWrite = "_ZWrite";
	public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
	{
		switch (blendMode)
		{
			case BlendMode.Opaque:
				material.SetFloat(sMode, 0);
				material.SetInt(sSrcBlend, (int) UnityEngine.Rendering.BlendMode.One);
				material.SetInt(sDstBlend, (int) UnityEngine.Rendering.BlendMode.Zero);
				material.DisableKeyword(sAlphaTestOn);
				material.DisableKeyword(sAlphaBlendOn);
				material.DisableKeyword(sAlphaPremultipyOn);
				material.SetInt(sZWrite, 1);
				
				material.renderQueue = -1;
				break;

			case BlendMode.Cutout:
				material.SetFloat(sMode, 1);
				material.SetInt(sSrcBlend, (int) UnityEngine.Rendering.BlendMode.One);
				material.SetInt(sDstBlend, (int) UnityEngine.Rendering.BlendMode.Zero);
				material.EnableKeyword(sAlphaTestOn);
				material.DisableKeyword(sAlphaBlendOn);
				material.DisableKeyword(sAlphaPremultipyOn);
				material.SetInt(sZWrite, 1);
				material.renderQueue = 2450;
				break;

			case BlendMode.Fade:
				material.SetFloat(sMode, 2);
				material.SetInt(sSrcBlend, (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt(sDstBlend, (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.DisableKeyword(sAlphaTestOn);
				material.EnableKeyword(sAlphaBlendOn);
				material.DisableKeyword(sAlphaPremultipyOn);
				material.SetInt(sZWrite, 0);
				material.renderQueue = 3000;
				break;

			case BlendMode.Transparent:
				material.SetFloat(sMode, 3);
				material.SetInt(sSrcBlend, (int) UnityEngine.Rendering.BlendMode.One);
				material.SetInt(sDstBlend, (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.DisableKeyword(sAlphaTestOn);
				material.DisableKeyword(sAlphaBlendOn);
				material.EnableKeyword(sAlphaPremultipyOn);
				material.SetInt(sZWrite, 0);
				material.renderQueue = 3000;
				break;
		}
	}

	public static bool IsTransparent(Texture texture) {	
		Texture2D texture2d = (Texture2D) texture;
		if(texture2d == null || texture2d.width == 0 || texture2d.height == 0) return false;
		FilterMode originalFilterMode = texture2d.filterMode;
		texture2d.filterMode = FilterMode.Point;
		RenderTexture rt = RenderTexture.GetTemporary(texture2d.width, texture2d.height);
		rt.filterMode = FilterMode.Point;
		RenderTexture.active = rt;
		Graphics.Blit(texture2d, rt);
		int downscale = 4;
		Texture2D img2;
		try {			
			img2 = new Texture2D(texture2d.width / downscale, texture2d.height / downscale);
			img2.ReadPixels(new Rect(0, 0, texture2d.width / downscale, texture2d.height / downscale), 0, 0, false);
		} catch {
			img2 = new Texture2D(texture2d.width, texture2d.height);
			img2.ReadPixels(new Rect(0, 0, texture2d.width, texture2d.height), 0, 0, false);
		}
		
		img2.Apply();
		RenderTexture.active = null;

		texture2d.filterMode = originalFilterMode;
		texture2d = img2;
		Color32[] aColors = texture2d.GetPixels32();
		

		for(int i = 0; i < aColors.Length; i = i + 4)
             if (aColors[i].a < 1f)
                 return true;
         return false;
	}

	IEnumerator LoadSoundFiles() {
		audioLoader.SearchAndLoadClips(folderSounds);
		yield return null;
		/*
		exampleClip = audioLoader.LoadAudioClip("Helicopter crash.wav");
		while(!exampleClip.isReadyToPlay) yield return null;

		AudioSource source = gameObject.AddComponent<AudioSource>();
		source.loop = true;
		source.spatialBlend = 0f;
		source.clip = exampleClip;

		source.Play();
		*/

	}

	public AudioClip LoadAudioClip(string clipName) 
	{
		return audioLoader.LoadAudioClip(clipName);
	}

	
}

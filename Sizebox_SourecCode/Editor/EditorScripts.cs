using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class EditorScripts : MonoBehaviour {
	static int sizeTexure = 248;


	// [MenuItem ("MY SCRIPTS/Build AssetBundles")]
    static void BuildAllAssetBundles ()
    {
		if(!Directory.Exists("BundlesWindows")) Directory.CreateDirectory("BundlesWindows");
        BuildPipeline.BuildAssetBundles ("BundlesWindows", BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows);

		if(!Directory.Exists("BundlesLinux")) Directory.CreateDirectory("BundlesLinux");
        BuildPipeline.BuildAssetBundles ("BundlesLinux", BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneLinuxUniversal);
    }

	[MenuItem("MY SCRIPTS/SimpleTest")]
	static void Test()
	{
		// fbx imported, it haves the mechanim all of that stuff
		// REDO: stuff that is in the editor helper
		// create sa mesh collider and prefabs

		// obtain all the assets bundles, check their folder and find the fbx files

		//alternatively, do that to the current selected asset bundles
		string gtsPath = "Assets/Models/GTS/";
		string microPath = "Assets/Models/Micro/";
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		bool isGTS = path.Contains(gtsPath);
		bool isMicro = path.Contains(microPath);
		Camera cam = new GameObject("ScreenshotCamera").AddComponent<Camera>();
		if(isGTS || isMicro)
		{
			string parentPath = "";
			// create specials files with parameters about the models
			List<string> parameters = new List<string>();
			if(isGTS) {
				parameters.Add("gts");
				parentPath = gtsPath;
			} 
			if(isMicro) {
				parameters.Add("micro");
				parentPath = microPath;
			}
			List<string> modelList = new List<string>();
			List<string> fileList = new List<string>();
			// create the virtual camera			
			// put the character outside of the scene..
			Vector3 offsetPosition = Vector3.one * 10000;
			// set up the camera
			cam.transform.position = new Vector3(0,1200,1800) + offsetPosition;
			cam.transform.rotation = Quaternion.Euler(12,180,0);
			cam.farClipPlane = 10000;
			// search in the parent folder
			string modelFolder = path.Replace(parentPath, "").Split('/')[0];
			Debug.Log("Model Folder: " + modelFolder);
			path = parentPath + modelFolder;
			Debug.Log("Folder to search assets: " + path);
			string[] fileEntries = Directory.GetFiles(path);
			Debug.Log("Contents of the folder:");
			// here are all the files that are inside the gts folder
			foreach(string file in fileEntries)
			{
				// locate the fbx files
				if(file.EndsWith(".fbx"))
				{
					Debug.Log("Model Found: " + file);
					GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(file);
					model = Instantiate(model, offsetPosition, Quaternion.identity) as GameObject;
					model.name = model.name.Replace("(Clone)","");
					modelList.Add(model.name);
					if(isGTS)
					{
						SABoneColliderBuilder colliderBuilder = model.AddComponent<SABoneColliderBuilder>();
						SABoneColliderBuilderInspector.Process(colliderBuilder);				
						string thumbnailPath = file.Replace(".fbx", "Thumb.png");
						TakeScreenshot(thumbnailPath, cam);
						string prefabPath = file.Replace(".fbx", "Prefab.prefab").Replace('\\','/');
						Debug.Log("New prefab: " + prefabPath);
						PrefabUtility.CreatePrefab(prefabPath, model, ReplacePrefabOptions.Default);
					}					
					DestroyImmediate(model);					
				}			
			}
			StringHolder holder = ScriptableObject.CreateInstance<StringHolder>();
			holder.content = modelList.ToArray();
			AssetDatabase.CreateAsset(holder, parentPath + modelFolder + "/modellist.asset");

			holder = ScriptableObject.CreateInstance<StringHolder>();
			holder.content = parameters.ToArray();
			AssetDatabase.CreateAsset(holder, parentPath + modelFolder + "/parameters.asset");

			// update filelist
			fileEntries = Directory.GetFiles(path);
			foreach(string file in fileEntries)
			{
				if(!file.EndsWith(".meta"))
				{					
					if(!file.EndsWith(".pmx") && !file.EndsWith(".pmd") && !file.EndsWith(".spa") && !file.EndsWith(".sph")) {
						fileList.Add(file.Replace(parentPath + modelFolder + "\\", ""));
					}
				}
			}

			holder = ScriptableObject.CreateInstance<StringHolder>();
			holder.content = fileList.ToArray();
			AssetDatabase.CreateAsset(holder, parentPath + modelFolder + "/filelist.asset");

			DestroyImmediate(cam.gameObject);
			string bundleName = modelFolder.ToLower();
			if(isGTS) bundleName += ".gts";
			if(isMicro) bundleName += ".micro";
			Debug.Log("Asset Bundle Name: " + bundleName);
			AssetImporter assetImporter = AssetImporter.GetAtPath(parentPath + modelFolder);
			assetImporter.assetBundleName = bundleName;
			BuildAllAssetBundles();
			AssetDatabase.RemoveAssetBundleName(bundleName, true);
		}
	}


	static void TakeScreenshot (string filename, Camera virtualCamera)
	{

		RenderTexture renderTexture = new RenderTexture(sizeTexure, sizeTexure, 24);
		virtualCamera.targetTexture = renderTexture;
		virtualCamera.Render();
		RenderTexture.active = renderTexture;
		Texture2D screenshot = new Texture2D(sizeTexure, sizeTexure, TextureFormat.RGB24, true);
		screenshot.ReadPixels(new Rect(0, 0, sizeTexure, sizeTexure), 0, 0);
		screenshot.Apply();
		RenderTexture.active = null;
		virtualCamera.targetTexture = null;			
		byte[] bytes = screenshot.EncodeToPNG();
		File.WriteAllBytes(filename, bytes);
		AssetDatabase.ImportAsset(filename, ImportAssetOptions.ForceUpdate);

	}

	[MenuItem("MY SCRIPTS/Update Animation List")]
	static void UpdateAnimationsData()
	{
		AnimationData data = ScriptableObject.CreateInstance<AnimationData>();
		

		RuntimeAnimatorController runtimeAnimator = Resources.Load<RuntimeAnimatorController>("Animator/Controller/GTSAnimator");
		AnimationClip[] clips = runtimeAnimator.animationClips;
		List<string> animationList = new List<string>();
		data.animations = new string[clips.Length];

		for(int i = 0; i < clips.Length; i++) {
			string clipName = clips[i].name;
			if(!clipName.Contains("a_")) {
				animationList.Add(clipName);
			}				
		}
		animationList.Sort();
		data.animations = animationList.ToArray();	
		
		// Array.Sort(data.animations);

		AssetDatabase.CreateAsset(data, "Assets/Resources/Data/Animations.asset");
	}

	// [MenuItem("MY SCRIPTS/JapaneseAsset")]
	static void CreateJapaneseData()
	{
		JapaneseData japData = ScriptableObject.CreateInstance<JapaneseData>();
		AssetDatabase.CreateAsset(japData, "Assets/Resources/japanese.asset");
	} 

	

}

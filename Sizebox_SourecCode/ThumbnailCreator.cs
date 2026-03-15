#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.Animations;
using System.IO;

public class ThumbnailCreator : MonoBehaviour {
	public Texture2D textura;
	public bool characters;
	public bool poses;
	public bool OnScreenStuff;
	public string stuffName;
	public GameObject model;
	private Animator anim;
	public int h = 128;
	public int w = 128;
	private RenderTexture renderTexture;
	public Camera virtualCamera;
	

	// Use this for initialization
	void Start () {
		renderTexture = new RenderTexture(w,h,24);
		virtualCamera.targetTexture = renderTexture;
		virtualCamera.Render();
		RenderTexture.active = renderTexture;
		if(characters)
		{
			GameObject[] gts = (GameObject[]) Resources.LoadAll<GameObject>("Giantesses");
			for(int i = 0; i < gts.Length; i++) {
				Debug.Log(gts[i].name);
				textura = AssetPreview.GetAssetPreview(gts[i]);
				if(textura != null) {
					byte[] bytes = textura.EncodeToPNG();
					File.WriteAllBytes(Application.dataPath + "/Resources/GiantessThumb/" + gts[i].name + ".png", bytes);
				}
			}
		}
		if(poses)
		{
			anim = (Animator) model.GetComponent<Animator>();
			StartCoroutine(CreatePosesThumbnails("/Resources/PosesThumb/"));
		}
		if(OnScreenStuff)
		{
			model.SetActive(false);
			TakeScreenshot(Application.dataPath + "/Resources/StuffThumbs/" + stuffName + ".png");
		}
			
	}
	
	// Update is called once per frame
	void Update () {
		
	
	}

	IEnumerator CreatePosesThumbnails(string folder)
	{
		foreach(AnimationClip clip in AnimationUtility.GetAnimationClips(model))
		{
			anim.Play(clip.name);
			yield return new WaitForSeconds(0.1f);
			TakeScreenshot(Application.dataPath + folder + clip.name + ".png");
		}

	}

	void TakeScreenshot (string filename)
	{
		virtualCamera.targetTexture = renderTexture;
		virtualCamera.Render();
		RenderTexture.active = renderTexture;
		Texture2D screenshot = new Texture2D(w, h, TextureFormat.RGB24, true);
		screenshot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
		screenshot.Apply();
		RenderTexture.active = null;
		virtualCamera.targetTexture = null;			
		byte[] bytes = screenshot.EncodeToPNG();
		File.WriteAllBytes(filename, bytes);

	}
}
#endif

#if UNITY_EDITOR
using UnityEngine;
using System.IO;

public class ScreenshotTaker : MonoBehaviour {
	string stuffName;
	int squareSize = 128;
	RenderTexture renderTexture;
	Camera virtualCamera;
	public GameObject target;
	

	// Use this for initialization
	void Start () {
		target.SetActive(true);
		virtualCamera = Camera.main;
		renderTexture = new RenderTexture(squareSize, squareSize, 24);
		virtualCamera.targetTexture = renderTexture;
		virtualCamera.Render();
		stuffName = target.name;

		Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
		float maxSize = 0;
		Renderer biggerRenderer = null;

		foreach(Renderer renderer in renderers)
		{
			float sizeX = renderer.bounds.size.x;
			float sizeY = renderer.bounds.size.y;
			float sizeZ = renderer.bounds.size.z;
			float newMax = Mathf.Max(sizeX, sizeY, sizeZ);
			if(newMax > maxSize) 
			{
				maxSize = newMax;
				biggerRenderer = renderer;
			}
		}

		virtualCamera.orthographic = true;
		virtualCamera.orthographicSize = maxSize / 2;
		virtualCamera.backgroundColor = Color.white;

		virtualCamera.transform.position = target.transform.position + Vector3.one * maxSize / 2;

		Vector3 direction = biggerRenderer.bounds.center - virtualCamera.transform.position;
		virtualCamera.transform.rotation = Quaternion.FromToRotation(Vector3.forward, direction);
		Vector3 euler = virtualCamera.transform.rotation.eulerAngles;
		virtualCamera.transform.rotation = Quaternion.Euler(euler.x, euler.y, 0);



		RenderTexture.active = renderTexture;
		TakeScreenshot(Application.dataPath + "/Resources/StuffThumbs/" + stuffName + ".png");
			
	}

	void TakeScreenshot (string filename)
	{
		virtualCamera.targetTexture = renderTexture;
		virtualCamera.Render();
		RenderTexture.active = renderTexture;
		Texture2D screenshot = new Texture2D(squareSize, squareSize, TextureFormat.RGB24, true);
		screenshot.ReadPixels(new Rect(0, 0, squareSize, squareSize), 0, 0);
		screenshot.Apply();
		RenderTexture.active = null;
		virtualCamera.targetTexture = null;			
		byte[] bytes = screenshot.EncodeToPNG();
		File.WriteAllBytes(filename, bytes);

	}
}
#endif
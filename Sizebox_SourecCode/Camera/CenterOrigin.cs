using UnityEngine;

public class CenterOrigin : MonoBehaviour {
	static Vector3 virtualOrigin;
	float threshold = 5000f;
	PlayerCamera playerCamera;
	float timeDelay = 10f;
	float lastUpdate = 0f;
	float currentTime;
	GameObject[] rootObjects;
	Transform[] rootTransforms;
	static Vector3 aux;

	// Use this for initialization
	void Start () {
		virtualOrigin = Vector3.zero;
		playerCamera = GetComponent<PlayerCamera>();
		lastUpdate = -100f;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		currentTime = Time.time;
		if(currentTime > lastUpdate + timeDelay) UpdateOrigin();
	}

	void UpdateOrigin() {
		if(playerCamera.target == null) return;
		Vector3 currentPosition = playerCamera.target.position;
		if(currentPosition.magnitude > threshold * playerCamera.targetScale) {
			lastUpdate = currentTime;
			RpcMoveOrigin(currentPosition);
		}
	}

	void RpcMoveOrigin(Vector3 offset) {
		rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
		rootTransforms = null;

		if(rootTransforms == null) {
			rootTransforms = new Transform[rootObjects.Length];
			for(int i = 0; i < rootTransforms.Length; i++) {
				rootTransforms[i] = rootObjects[i].transform;
			}
		}

		for(int i = 0; i < rootTransforms.Length; i++) {
			rootTransforms[i].localPosition -= offset;
		}

		virtualOrigin -= offset;
	}

	

	public static Vector3 WorldToVirtual(Vector3 worldPosition) {
		worldPosition.x -= virtualOrigin.x;
		worldPosition.y -= virtualOrigin.y;
		worldPosition.z -= virtualOrigin.z;
		return worldPosition;
	}

	public static Vector3 WorldToVirtual(float x, float y, float z) {
		aux.x = x - virtualOrigin.x;
		aux.y = y - virtualOrigin.y;
		aux.z = z - virtualOrigin.z;
		return aux;
	}

	public static Vector3 VirtualToWorld(Vector3 virtualPosition) {
		virtualPosition.x += virtualOrigin.x;
		virtualPosition.y += virtualOrigin.y;
		virtualPosition.z += virtualOrigin.z;
		return virtualPosition;
	}

	public static Vector3 VirtualToWorld(float x, float y, float z) {
		aux.x = x + virtualOrigin.x;
		aux.y = y + virtualOrigin.y;
		aux.z = z + virtualOrigin.z;
		return aux;
	}
}

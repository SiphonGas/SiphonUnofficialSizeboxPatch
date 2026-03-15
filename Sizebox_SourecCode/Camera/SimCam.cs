using UnityEngine;
using System.Collections;

public class SimCam : MonoBehaviour {
	bool enable;
	Camera mainCamera;

	public float zoomSpeed = 0.4f;
	float zoomPower = 0f;
	public float zoomSmooth = 4f;
	public float rotationSmooth = 16f;
	public float zoomShift = 1.2f;

	// Camera Movements Control
	float shiftMultiplier = 5f;
	public float movementSpeed = 1f;
	float currentSpeed;
	float baseSpeed;
	Vector3 movementDirection;

	Transform cameraTransform;

	enum Mode { Basic, Advanced };
	Mode mode = Mode.Basic;
	float mouseSensibility;
	public float fpsSensibility = 1.5f;
	float cameraHeight = 1f;

	// Input
	public float speedChange = 2.5f;
	bool shift;
	bool decreaseSpeed;
	bool increaseSpeed;	
	float zoomInput;
	float forwardMovement;
	float righMovement;
	float upMovement;
	Vector3 mousePositionInScreen;
	Vector3 centerPoint;
	bool rigthClick;
	float mouseH;
	float mouseV;
	int borderThickness = 3;
	Quaternion myRotation;
	Quaternion targetRotation;


	// todo: fix movement speed
	// move by hovering borders
	// right click move
	// right click switchs back between look at and not look at.

	// Use this for initialization
	public void Enable() {
		enable = true;
		zoomPower = 0f;
		movementDirection = Vector3.zero;
		GetCameraHeight();
		baseSpeed = movementSpeed * cameraHeight;
	}

	public void Disable() {
		enable = false;
	}

	void Start () {
		if(PreferencesCentral.cameraRTSMode.value == true) mode = Mode.Basic;
		else mode = Mode.Advanced;

		Disable();
		mainCamera = Camera.main;
		cameraTransform = mainCamera.transform.parent;
		mouseSensibility = PreferencesCentral.mouseSensibility.value;
		baseSpeed = movementSpeed;
	
	}
	
	// Update is called once per frame
	void Update () {
		if(!enable) return;
		GetInput();
		UpdateCenterPoint();
		GetCameraHeight();
	}

	void LateUpdate() {
		if(!enable) return;
		if(mode == Mode.Basic) BasicMode();
		else AdvancedMode();
	}

	void BasicMode() {
		if(!OrbitPoint(centerPoint)) {
			MoveBasic();
		}
		Zoom();
	}

	void AdvancedMode() {
		MoveAdvanced();
		Zoom();
		MouseLook();
	}

	void GetInput() 
	{
		mousePositionInScreen = Input.mousePosition;

		rigthClick = Input.GetMouseButton(1);
		mouseH = Input.GetAxis("Mouse X") * mouseSensibility;
		mouseV = Input.GetAxis("Mouse Y") * mouseSensibility;
		
		shift = Input.GetKey(KeyCode.LeftShift);
		decreaseSpeed = Input.GetKey(KeyCode.Z);
		increaseSpeed = Input.GetKey(KeyCode.X);	
		zoomInput = Input.GetAxisRaw("Mouse ScrollWheel");

		forwardMovement = Input.GetAxis("Vertical");
		righMovement = Input.GetAxis("Horizontal");

		if(!rigthClick && mode == Mode.Basic && Screen.fullScreen) {
			if(forwardMovement == 0f) {
				if(mousePositionInScreen.y < borderThickness) forwardMovement = -1f;
				if(mousePositionInScreen.y > Screen.height - borderThickness) forwardMovement = 1f;
			}			
			
			if(righMovement == 0f) {
				if(mousePositionInScreen.x < borderThickness) righMovement = -1f;
				if(mousePositionInScreen.x > Screen.width - borderThickness) righMovement = 1f;
			}
		}

		
		


		upMovement = 0;
		if(Input.GetKey(KeyCode.E)) upMovement += 1;
		if(Input.GetKey(KeyCode.Q)) upMovement -= 1;

		

		if(Input.GetKeyDown(KeyCode.V)) {
			if(mode == Mode.Basic) mode = Mode.Advanced;
			else mode = Mode.Basic;
			PreferencesCentral.cameraRTSMode.value = (mode == Mode.Basic);
		}
	}

	void MoveCamera(Vector3 forward, Vector3 up, Vector3 right) {
		if(forwardMovement != 0 || righMovement != 0 || upMovement != 0)
		{
			movementDirection = forwardMovement * forward + righMovement * right + upMovement * up;
			movementDirection.Normalize();
			movementDirection *= currentSpeed;
		} else {
			return;
		}

		Vector3 movement = movementDirection * Time.deltaTime;
		cameraTransform.position += movement;
	}

	bool OrbitPoint(Vector3 point) 
	{
		Vector3 direction = cameraTransform.position - point;
		Vector3 rotation = Quaternion.LookRotation(direction).eulerAngles;
		if(!rigthClick) {
			myRotation = Quaternion.Euler(rotation.x, rotation.y, 0);
			targetRotation = myRotation;
			return false;
		}

		float x = targetRotation.eulerAngles.x + mouseV;
		float y = targetRotation.eulerAngles.y + mouseH;
		x = ClampAngle(x);

		targetRotation = Quaternion.Euler(x,y,0);
		myRotation = Quaternion.Slerp(myRotation, targetRotation, rotationSmooth * Time.deltaTime);
		

		direction = myRotation * (Vector3.forward * direction.magnitude);
		cameraTransform.position = direction + point;
		LookAt(point);
		return true;

	}

	void LookAt(Vector3 point) {
		Vector3 directionToLook = point - cameraTransform.position;
		cameraTransform.rotation = Quaternion.LookRotation(directionToLook);
	}

	void MoveBasic() {
		Vector3 forward = cameraTransform.forward;
		forward.y = 0;
		forward.Normalize();

		Vector3 right = cameraTransform.right;
		right.y = 0;
		right.Normalize();

		baseSpeed = movementSpeed * cameraHeight;
		currentSpeed = baseSpeed;		
		if(shift) currentSpeed *= shiftMultiplier;

		MoveCamera(forward, Vector3.up, right);
	}

	void MoveAdvanced() {
		if(increaseSpeed) baseSpeed *= 1 + speedChange * Time.deltaTime;
		if(decreaseSpeed) baseSpeed /= 1 + speedChange * Time.deltaTime;
		currentSpeed = baseSpeed;
		if(shift) currentSpeed *= shiftMultiplier;
		MoveCamera(cameraTransform.forward, cameraTransform.up, cameraTransform.right);
	}

	void UpdateCenterPoint() {
		if(rigthClick) return;
		RaycastHit hitPoint;
		Vector3 centerOfCamera = new Vector3(Screen.width / 2, Screen.height / 2, 0);
		Ray ray = mainCamera.ScreenPointToRay(centerOfCamera);
		if(Physics.Raycast(ray, out hitPoint, Mathf.Infinity)) {
			centerPoint = hitPoint.point;
		}
	}

	void GetCameraHeight() {
		if(cameraTransform == null) {
			cameraHeight = 1f;
			return;
		}
		RaycastHit hitPoint;
		Vector3 cameraPosition = cameraTransform.position;
		if(Physics.Raycast(cameraPosition, Vector3.down, out hitPoint, 10000)) {
			cameraHeight = hitPoint.distance;
		}
	}

	void Zoom() {
		if(zoomInput != 0) {
			zoomPower = zoomInput;
			if(shift) zoomPower *= zoomShift;
		}

		if(zoomPower == 0) return;
		Vector3 direction = centerPoint - cameraTransform.position;
		float distance = direction.magnitude;

		// obtain direction vector to center point
		float movementAmount = zoomPower * zoomSpeed * distance;
		zoomPower = Mathf.Lerp(zoomPower, 0f, zoomSmooth * Time.deltaTime);

		if(distance - movementAmount > 0)
		{
			direction = direction.normalized * movementAmount;
			if(shift) direction *= shiftMultiplier;
			cameraTransform.position += direction;
		}
	}

	void MouseLook() 
	{
		//rotate the world cam
		if(rigthClick) {
			float rotationX = cameraTransform.localEulerAngles.x - mouseV * fpsSensibility;
			rotationX = ClampAngle(rotationX);
			float rotationY = cameraTransform.localEulerAngles.y + mouseH * fpsSensibility;
			cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, Quaternion.Euler(rotationX, rotationY, 0), rotationSmooth * Time.deltaTime);
		}
	}

	float ClampAngle(float x) {
		if(x > 180f) x -= 360f;
		x = Mathf.Clamp(x, -89f, 89f);
		return x;
	}
}

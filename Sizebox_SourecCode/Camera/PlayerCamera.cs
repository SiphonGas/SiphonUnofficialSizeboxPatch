using UnityEngine;

public class PlayerCamera : MonoBehaviour, IListener {
	// Things to initialize at the Start
	public float smoothRotation = 10f;
	public static PlayerCamera instance;
	Camera farCamera;
	public Transform target;
	public EntityBase entity;
	public float forwardView = 0.14f;
	
	[System.Serializable]
	public class PositionSettings
	{
		public Vector3 up = Vector3.up;
		public Vector3 forward = Vector3.forward;
		public float verticalOffset = 1.5f;
		public float targetZoom = -2.5f;
		public float distanceFromTarget = -2.5f;
		public float zoomSmooth = 0.1f;
		public float minZoom = -2f;
		public float maxZoom = -200f;
		public float smooth = 2f;
		[HideInInspector]
		public float adjustementDistance = 9999f;
	}
	
	[System.Serializable]
	public class OrbitSettings
	{
		public float xRotation = -20f;
		public float yRotation = -180f;
		public float maxXRotation = 85f;
		public float minXRotation = -85f;
		public float mouseSensibility = 8f;
	}
	
	bool colliding = false;
	
	public PositionSettings position = new PositionSettings();
	public OrbitSettings orbit = new OrbitSettings();
	
	Vector3 targetPos = Vector3.zero;
	Vector3 destination = Vector3.zero;
	Vector3 adjustedDestination = Vector3.zero;
	Player player;
	float hOrbitInput, vOrbitInput, zoomInput, hOrbitSnapInput;
	public bool firstPersonMode = false;
	bool wasClimbing = false;
	public float rawInputH = 0f;
	public float targetScale = 1f;
	public static float defaultFOV = 65f;
	public float lookDownFOV = 15f;
	public float sprintFOV = 25f;
	float targetFOV;
	Camera mainCamera;
	public float shakeStrength;
	float durationShake = 1f;
	public ResizeCharacter resizeChar;
	public LayerMask collisionLayer;
	Transform parentTransform;
	VRCamera vr;
	public Vector3 eyePosition;
	
	// Use this for initialization
	public void OnNotify(IEvent e) {
		StepEvent se = (StepEvent) e;
		// calculate earthquake magnitude
		float relativeSize = se.gts.Height / parentTransform.localScale.y;
		float magnitude = Mathf.Log10(relativeSize);
		float shakeDistance = se.gts.Height * magnitude;

		if(magnitude < 0f) return;

		// calculate distance atenuation
		float distanceToPlayer = (se.position - transform.position).magnitude;
		distanceToPlayer = 1 - (distanceToPlayer / shakeDistance);
		distanceToPlayer = Mathf.Clamp01(distanceToPlayer);


		Shake(magnitude * distanceToPlayer);
	}

	void Awake()
	{
		parentTransform = transform.parent;
		defaultFOV = PreferencesCentral.fov.value;
		collisionLayer = Layers.cameraCollisionMask;
		instance = this;
		mainCamera = GetComponent<Camera>();
		farCamera = transform.GetChild(0).GetComponent<Camera>();
		vr = GetComponent<VRCamera>();

		orbit.mouseSensibility = PreferencesCentral.mouseSensibility.value;
	} 


	void Start () {
		targetScale = GameController.startingSize;
		SetCameraTarget(target);
		MoveToTarget();

		position.distanceFromTarget = position.distanceFromTarget * targetScale;
		position.adjustementDistance = - position.distanceFromTarget;

		GameController.Instance.eventManager.RegisterListener(this, Interest.OnStep);
	}
	
	public void SetCameraTarget(Transform t)
	{
		target = t;
		if(target != null)
		{
			entity = target.GetComponent<EntityBase>();
			if(entity == null) return;

			if(entity.isGiantess) {
				collisionLayer = Layers.gtsWalkableMask;
				return;
			}

			if(target.GetComponent<VehicleEnterExit>()) {
                collisionLayer = Layers.vehicleCameraCollisionMask;
				return;
			} 

			collisionLayer = Layers.cameraCollisionMask;

			Player targetPlayer = target.GetComponent<Player>();
			if(targetPlayer)
			{
				player = targetPlayer;
				resizeChar = target.GetComponent<ResizeCharacter>();
			}
			
		}
	}
	
	void Update() {
		GetInput();	
		if(firstPersonMode && entity != null && entity.isGiantess) eyePosition = entity.GetEyesPosition();
	}

	void LateUpdate()
	{
		if(player == null) return;
		AdjustToPlayerSize();
		UpdateFOV();				
		OrbitTarget();
		MoveToTarget();
		LookAtTarget();			
		ZoomInOnTarget();	
		CollisionDetection();				
		ShakeEffect();
	}


	void AdjustToPlayerSize() {	
		if(entity != null) {
			targetScale = entity.AccurateScale;				
			parentTransform.localScale = new Vector3(targetScale, targetScale, targetScale);	
		}
	}

		
		

	void CollisionDetection()
	{
		RaycastHit hit;
		Vector3 direction = destination - targetPos;
		float maxDistance = -position.distanceFromTarget;

		Debug.DrawLine(targetPos, targetPos + direction, Color.green);
		colliding = Physics.Raycast(targetPos, direction, out hit, direction.magnitude * 1.5f, collisionLayer);
		float collisionDistance = hit.distance * 0.9f;

		float whiskerAngle = 15;
		float a = WhiskerCollisionCheck(direction, -whiskerAngle, 0);
		float b = WhiskerCollisionCheck(direction, whiskerAngle, 0);
		float c = WhiskerCollisionCheck(direction, 0, -whiskerAngle);
		float d = WhiskerCollisionCheck(direction, 0, whiskerAngle);
		float minDistance = Mathf.Min(a,b,c,d, maxDistance);

		// smoothly move the camera in collisions
		float targetDistance = minDistance;
		float speed = 0.5f;
		if(colliding) {			
			if(collisionDistance < targetDistance) {
				targetDistance = collisionDistance;
				speed = 16f;
			}						
		} 

		if(minDistance < position.adjustementDistance) {
			speed = 8f;
		}

		targetDistance *= 0.9f;
		position.adjustementDistance = Mathf.Lerp(position.adjustementDistance, targetDistance, speed * Time.deltaTime);
				
	}

	float WhiskerCollisionCheck(Vector3 direction, float x_rotation, float y_rotation) {
		// Calculated the rotation from the origianl position
		Quaternion rotation = Quaternion.LookRotation(direction);
		rotation =  rotation * Quaternion.Euler(x_rotation, y_rotation, 0);

		// Reconstruct the direction
		direction = rotation * Vector3.forward * direction.magnitude;
		

		// Raycast to find collision
		RaycastHit hit;
		if(Physics.Raycast(targetPos, direction, out hit, direction.magnitude * 1.5f, collisionLayer)) {
			Debug.DrawLine(targetPos, targetPos + direction, Color.red);
			return hit.distance;
		}
		Debug.DrawLine(targetPos, targetPos + direction, Color.green);
		return - position.distanceFromTarget;
	}
	
	void GetInput()
	{
		float timeScale = Time.timeScale;
		if (Input.GetButtonDown("CameraSwith")) {
			firstPersonMode = !firstPersonMode;
		}
		rawInputH = Input.GetAxis("Mouse X");
		hOrbitInput = rawInputH * orbit.mouseSensibility * timeScale;
		vOrbitInput = Input.GetAxis("Mouse Y") * orbit.mouseSensibility * timeScale; // * orbit.horizontalAimingSpeed * Time.deltaTime;
		zoomInput = Input.GetAxisRaw("Mouse ScrollWheel");
		if ( Input.GetButton("ZoomIn")) {
			zoomInput += 0.1f;
		} else if (Input.GetButton("ZoomOut")) 
		{
			zoomInput -= 0.1f;
		}
	}
	
	void MoveToTarget()
	{
		if(player == null) return;
		Quaternion refeferenceRotation;

		if(player.gameObject.activeSelf) refeferenceRotation = Quaternion.LookRotation(player.referenceTransform.forward, player.referenceTransform.up);
		else refeferenceRotation = Quaternion.Euler(0,0,0);

		float newVerticalOffset = 1.5f;
		Vector3 up = Vector3.up;
		if(vr.IsInVR()) newVerticalOffset = 1f;
		if(entity != null && entity.isGiantess) newVerticalOffset = 0.9f;
		if(firstPersonMode) newVerticalOffset = 1.6f;

		if(player.isClimbing() && player.gameObject.activeSelf) {
			newVerticalOffset = 0.8f;
			up = player.myTransform.up;
		}

		
		position.up = Vector3.Lerp(position.up, up, Time.deltaTime * 4f);
		position.verticalOffset = Mathf.Lerp(position.verticalOffset, newVerticalOffset, Time.deltaTime * 4f);

		targetPos = target.position + position.up * position.verticalOffset * targetScale;

		if(firstPersonMode)
		{
			if(entity != null && entity.isGiantess) {
				Vector3 forward = transform.forward;
				forward.y = 0f;
				forward.Normalize();
				targetPos = entity.GetEyesPosition() + forward * (forwardView * targetScale);
			} 
		} 

		if(!player.isClimbing() && player.gameObject.activeSelf)
		{
			Quaternion antAngulo;
			if(wasClimbing)
			{
				// Posicionar correctamente la rotacion Y entre coordenadas del target y coodernadas globales
				if(firstPersonMode) antAngulo = player.transform.rotation * Quaternion.Euler(orbit.xRotation, 180f, 0);
				else antAngulo = player.transform.rotation * Quaternion.Euler(orbit.xRotation, orbit.yRotation, 0);	

				orbit.yRotation = antAngulo.eulerAngles.y;
				wasClimbing = false;
			}
			
		}

		// Calculate the rotation
		Quaternion rotationYZ = refeferenceRotation;
		Quaternion rotationXY = Quaternion.Euler(orbit.xRotation, orbit.yRotation, 0);
		
		// Put the camera in the correct distance
		float distanceFromTarget = - position.distanceFromTarget;

		if(firstPersonMode)
		{
			distanceFromTarget = 0.02f * targetScale;
		}

		destination = rotationYZ * (rotationXY * Vector3.forward) * distanceFromTarget;
		destination += targetPos;
		parentTransform.position = destination;

		float collisionDistance = position.adjustementDistance;

		if(!firstPersonMode && collisionDistance < distanceFromTarget)
		{
			distanceFromTarget = collisionDistance;

			adjustedDestination = rotationYZ * (rotationXY * Vector3.forward) * distanceFromTarget;
			adjustedDestination += targetPos;
			parentTransform.position = adjustedDestination;	
		}
		
	}
	
	void LookAtTarget()
	{
		Quaternion targetRotation;
		if(player.gameObject.activeSelf)
		{
			targetRotation = Quaternion.LookRotation(targetPos - parentTransform.position, player.referenceTransform.up); 
		}
		else
		{
			targetRotation = Quaternion.LookRotation(targetPos - parentTransform.position);
		}
		
		//parentTransform.rotation = Quaternion.Slerp(parentTransform.rotation, targetRotation, 5 * Time.deltaTime);
		parentTransform.rotation = targetRotation;
	}
	
	void OrbitTarget()
	{
		if(firstPersonMode && vr.IsInVR()) {
			orbit.xRotation = 0f;
		} else {
			orbit.xRotation += vOrbitInput;
		}
		orbit.yRotation += hOrbitInput;
		orbit.xRotation = Mathf.Clamp(orbit.xRotation, orbit.minXRotation, orbit.maxXRotation);
	}
	
	void ZoomInOnTarget()
	{
		// adjust the target zoom of the character, makin it sure that is inside of the boundaries
		position.targetZoom *= (1 - zoomInput * position.zoomSmooth * Time.deltaTime);
		if(position.targetZoom < position.maxZoom)
		{
			position.targetZoom = position.maxZoom;
		}
		else if(position.targetZoom > position.minZoom)
		{
			position.targetZoom = position.minZoom;
		}
		float oldDistance = position.distanceFromTarget;

		position.distanceFromTarget = Mathf.Lerp(oldDistance, position.targetZoom * targetScale, position.smooth * Time.deltaTime);
		position.adjustementDistance *= position.distanceFromTarget / oldDistance;
		
		
	}

	void ShakeEffect()
	{
		if(vr.IsInVR()) return;
		if(shakeStrength > 0)
		{
			Vector3 shake = Random.onUnitSphere * shakeStrength;
			shake.z = 0;
			shake *= targetScale;
			parentTransform.position = Vector3.Lerp(parentTransform.position, parentTransform.position + shake, 0.5f * Time.deltaTime);
			shakeStrength -= (1f / durationShake) * Time.deltaTime;
		}
	}

	public void Shake(float intensity)
	{
		if(vr.IsInVR()) return;
		float newStrenght = intensity;
		if(newStrenght > shakeStrength)
			shakeStrength = newStrenght;
	}
	
	void UpdateFOV()
	{
		if(vr.IsInVR()) return;
		targetFOV = defaultFOV;
		float speed = 2f;
		if(!firstPersonMode) {
			if(player.isSprinting()) targetFOV += sprintFOV;
		
			else if(orbit.xRotation > 0f) {
				float ratio = orbit.xRotation / orbit.maxXRotation;
				speed = ratio * 20f;
				if(speed < 2f) speed = 2f;
				targetFOV += (orbit.xRotation / orbit.maxXRotation) * lookDownFOV;
			}
		}

		if(targetFOV > 110f) targetFOV = 110f;
		float fov = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * speed);	
		
		mainCamera.fieldOfView = fov;
		farCamera.fieldOfView = fov;		
		
	}

	public void SetMouseSensibility(float sensibility) {
		orbit.mouseSensibility = sensibility;
		PreferencesCentral.mouseSensibility.value = sensibility;
	}

	

}

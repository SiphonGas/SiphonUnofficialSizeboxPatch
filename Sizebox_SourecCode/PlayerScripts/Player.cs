using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class Player : Micro
{
	public static bool vrMode = false;

	public float walkSpeed = 0.2f;
	public float runSpeed = 1.0f;
	public float sprintSpeed = 3.0f;
	public float flySpeed = 8.0f;

	public float turnSmoothing = 4.0f;
	public float speedDampTime = 0.4f;
	
	public float climbTurnSpeed = 1.5f;
	public float climbRotationSmooth = 2f;
	public float climbSpeed = 70f;
	
	public float jumpHeight = 7.0f;
	private float jumpCooldown = 1.0f;
		
	private CapsuleCollider capsuleCollider;
	private float height;

	private float timeToNextJump = 0;
	private float speed;

	private Animator anim;
	private int speedFloat;
	private int crushedBool;
	private int jumpBool;
	private int hFloat;
	private int vFloat;
	private int flyBool;
	private int groundedBool;
	private int climbBool;
	private Transform cameraTransform;
    private ResizeCharacter resizer;
	PlayerCamera playerCamera;

	private float horizontalInput;
	private float verticalInput;

	private bool aim;

    private bool run;
	private bool sprint;
	private bool jump;

	public bool isMoving;
	
	private bool climbing = false;

	// fly
	private bool fly = false;
	private float distToGround;
	private float sprintFactor;
	private bool walk = false;
	Vector3 targetDir;
	public bool autowalk = false;
	// to parent to gts
	Vector3 camForwardDirection;
	AudioSource audioSource;
	AudioSource flySource;

	// Super Speed for Travelling Long Distances
	public float superSpeed = 1000f;
	bool superFlySpeed = false;
	float lastShiftTime = 0f;
	float timeBetweenShifts = 0.2f;
	Gravity gravity;

	public Vector3 planeUp = Vector3.up;
	public Vector3 planeForward = Vector3.forward;
	bool firstPersonMode = false;


	delegate void State();
	State CurrentState;

	public Transform referenceTransform; // to be used by the camera
	


	protected override void Awake()
	{
		isPlayer = true;
		base.Awake();
		myTransform = transform;
		CurrentState = DefaultState;
		baseScale = 1.6f;
		gameObject.name = "Player Character";
		// Set up the player and add the necessary components
		capsuleCollider = GetComponent<CapsuleCollider>();
		capsuleCollider.center = new Vector3(0,0.8f,0);
		capsuleCollider.radius = 0.3f;
		capsuleCollider.height = 1.6f;

		rbody = GetComponent<Rigidbody>();
		rbody.freezeRotation = true;
		rbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		rbody.useGravity = false;
		rbody.interpolation = RigidbodyInterpolation.Interpolate;

		gameObject.layer = Layers.playerLayer;

		height = capsuleCollider.height;

		anim = GetComponent<Animator>();
		Debug.Assert(anim, "The animator is not loaded");

        resizer = gameObject.AddComponent<ResizeCharacter>();
		cameraTransform = Camera.main.transform;

		speedFloat = Animator.StringToHash("Speed");
		jumpBool = Animator.StringToHash("Jump");
		hFloat = Animator.StringToHash("H");
		vFloat = Animator.StringToHash("V");
		climbBool = Animator.StringToHash("Climbing");
		crushedBool = Animator.StringToHash("Crushed");
		// fly
		flyBool = Animator.StringToHash ("Fly");
		groundedBool = Animator.StringToHash("Grounded");
		distToGround = GetComponent<Collider>().bounds.extents.y;
		sprintFactor = sprintSpeed / runSpeed;

		audioSource = gameObject.AddComponent<AudioSource>();

		// Fly Sound Settings
		flySource = gameObject.AddComponent<AudioSource>();
		flySource.loop = true;			
		flySource.spatialBlend = 0f;
		flySource.dopplerLevel = 0f;
		flySource.volume = 0f;
		SoundManager.SetSoundClip(flySource, SoundManager.This.flySound);

		actionManager = gameObject.AddComponent<ActionManager>();

		gameObject.AddComponent<PlayerIK>();
		gravity = gameObject.AddComponent<Gravity>();

		referenceTransform = new GameObject("Reference Transform", typeof(CameraReference)).transform;
		
	}

	void Start() {
		playerCamera = PlayerCamera.instance;
	}

	public override void OnStartAuthority() {
		GameController.Instance.SetPlayerInstance(this);  
		PlayerCamera mainCamera = Camera.main.GetComponent<PlayerCamera>();
		mainCamera.SetCameraTarget(transform);		
		resizer.ChangeScale(GameController.startingSize);
		GameController.Instance.OnPlayerStart();
	}

	bool IsGrounded() {
		if(isInFloor) return true;
		if(transform.parent != null && transform.parent.gameObject.layer == Layers.objectLayer) return true;
		
		Vector3 origin = transform.position + Vector3.up * 0.1f * Scale;
		Vector3 direction = -Vector3.up;
		float distance = (distToGround + 0.2f) * Scale;
		bool grounded = Physics.Raycast(origin, direction, distance);
		return grounded;
	}



	protected override void CheckGround()
	{
		if(fly) return;
		base.CheckGround();
	}

	void UpdateReference() {
		if(referenceTransform.parent != transform.parent) {
			referenceTransform.SetParent(transform.parent);
		}
		if(climbing) UpdateRelativeReference();
		else RepositionReference();
	}

	void UpdateRelativeReference() {
		planeUp = transform.up;
		planeForward = Vector3.ProjectOnPlane(referenceTransform.forward, planeUp);
		Quaternion targetRotation = Quaternion.LookRotation (planeForward, planeUp);
		referenceTransform.rotation = Quaternion.Slerp(referenceTransform.rotation, targetRotation, Time.deltaTime * 4f);
	}

	private void RepositionReference()
	{
		Vector3 repositioning = referenceTransform.forward;
		repositioning.y = 0;
		Quaternion targetRotation = Quaternion.LookRotation (repositioning, Vector3.up);
		referenceTransform.rotation = Quaternion.Slerp(referenceTransform.rotation, targetRotation, Time.deltaTime * 2f);
	}

	void LateUpdate() {
		if(!hasAuthority) return;	
		firstPersonMode = PlayerCamera.instance.firstPersonMode;
		
	}

	void Update()
	{
		// fly
		if(!hasAuthority) return;
		if(GameController.inputEnabled) 
		{
			Render(!firstPersonMode);
			horizontalInput = Input.GetAxis("Horizontal");
			verticalInput = Input.GetAxis("Vertical");

			if(Input.GetButtonDown ("Fly"))
			{
				fly = !fly;
				setClimb(false);
				anim.SetFloat(speedFloat, 1f);
				if(fly && transform.parent != null) {
					Invoke("UnparentWhileFlyng", 1f);
				}
			}

			if(Input.GetButtonDown ("Climb") && !fly)
				setClimb(!climbing);
				
			aim = Input.GetButtonDown("Aim");
			run = Input.GetButton ("Run");
			sprint = Input.GetButton ("Sprint");
			if (Input.GetButtonDown("Jump")) jump = true;
			// horizontal input for the first person camera

			// autowalk
			if(Input.GetKeyDown(KeyCode.RightShift)) autowalk = !autowalk;

			if(autowalk) verticalInput = 1f;
			
			// super fly speed
			if(IsFlying() && !vrMode) {
				if(Input.GetKeyDown(KeyCode.LeftShift)) 
				{
					if(Time.time < lastShiftTime + timeBetweenShifts) superFlySpeed = true;
					else lastShiftTime = Time.time;
				}

			} else superFlySpeed = false;

			if(aim) walk = !walk;

			isMoving = Mathf.Abs(horizontalInput) > 0.1 || Mathf.Abs(verticalInput) > 0.1;
			

			camForwardDirection = cameraTransform.TransformDirection(Vector3.forward);

			if(Input.GetKeyDown(KeyCode.Return) && ObjectManager.Instance.vehicles.Count > 0) {
				VehicleEnterExit closestVehicle = null;
				float closestDistance = 10000;
				foreach(VehicleEnterExit vehicle in ObjectManager.Instance.vehicles) {
					if(vehicle == null) continue;
					float distance = (transform.position - vehicle.transform.position).sqrMagnitude;
					if(!closestVehicle || distance < closestDistance) {
						closestVehicle = vehicle;
						closestDistance = distance;
					}
				}
				if(closestVehicle) {
					closestVehicle.EnterVehicle();
				}
			}
		}
		
		if(rbody.interpolation == RigidbodyInterpolation.Interpolate && myTransform.parent != null) {
			rbody.interpolation = RigidbodyInterpolation.None;
		} else if(rbody.interpolation == RigidbodyInterpolation.None && myTransform.parent == null) {
			rbody.interpolation = RigidbodyInterpolation.Interpolate;
		}
		
	}

	void UnparentWhileFlyng()
	{
		if(fly && transform.parent != null) {
			transform.SetParent(null);
		}
	}
	
	void setClimb(bool val)
	{
		climbing = val;

		float heighModifier = 1f;
		if(climbing) heighModifier = 0.3f;

		capsuleCollider.height = height * heighModifier;
		capsuleCollider.center = Vector3.up * height * heighModifier * 0.5f;
	}

    void FixedUpdate()
    {
		if(!hasAuthority) return;
		
		CurrentState();
		UpdateReference();
	}

	void DefaultState()
	{
		// CheckGround();
        anim.SetFloat(hFloat, horizontalInput);
        anim.SetFloat(vFloat, verticalInput);
		anim.SetBool(flyBool, fly);			
		anim.SetBool(climbBool, climbing);
		// set collider conf

		// Fly					
		gravity.useGravity = !fly;
		anim.SetBool (groundedBool, IsGrounded());
		FlySound();
		
		if(fly)
		{
			planeUp = Vector3.up;
			planeForward = Vector3.forward;
			FlyManagement(horizontalInput,verticalInput);
			jump = false;
		}
		else
		{			
			if (climbing)
			{
				ClimbManagement(horizontalInput, verticalInput, run, sprint);
				jump = false;
			}
			else
			{
				planeUp = Vector3.up;
				planeForward = Vector3.forward;
				MovementManagement (horizontalInput, verticalInput, run, sprint);
				JumpManagement ();
			}
		}
		
		TurnFPSCamera();
	}
	
	void ClimbManagement(float horizontal, float vertical, bool running, bool sprinting)
	{		
		float gravityForce = Physics.gravity.y * Scale;
		
		RaycastHit hit;
		bool hasHit = Physics.Raycast(transform.position + transform.up * 0.1f * Scale, transform.forward, out hit, 1f * Scale);
		if(!hasHit)
		{
			hasHit = Physics.Raycast(transform.position + transform.up * 0.1f * Scale, -transform.up, out hit, 2f * Scale);
		}		
		gravity.useGravity = !hasHit;
		if(hasHit) targetDir = hit.normal;
		else targetDir = Vector3.up;
		
		Vector3 climbDirection;
		if(isMoving && firstPersonMode) climbDirection = GetCameraForward().normalized;
		else climbDirection = myTransform.forward;
		
		Quaternion newRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(climbDirection, targetDir), targetDir);
		newRotation = Quaternion.Slerp(transform.rotation, newRotation, climbRotationSmooth * Time.fixedDeltaTime);
		newRotation *= Quaternion.Euler(0,horizontal*climbTurnSpeed,0);
		rbody.MoveRotation(newRotation);

		rbody.AddForce(targetDir * gravityForce);
		if(isMoving)
		{
			climbSpeed = 150f;
			if(vrMode) climbSpeed = 50f;
			else if(sprint) climbSpeed *= 2;

			anim.SetFloat(speedFloat, vertical * climbSpeed / 80f);
			rbody.AddForce(climbDirection * vertical * climbSpeed * Scale);
		}
		else
		{
			anim.SetFloat(speedFloat, 0f);
		}
	}

	// fly
	void FlyManagement(float horizontal, float vertical)
	{
		float speed = flySpeed;
		if(superFlySpeed) speed = superSpeed;
		float sprintMultiplier = 1;
		if(sprint) {
			sprintMultiplier *= sprintFactor * 4;
		}

		Vector3 direction;

		// if(fps) direction = (Camera.main.transform.forward * vertical + Camera.main.transform.right * horizontal).normalized;
		direction = Rotating(horizontal, vertical);

		if(isMoving) {

			rbody.AddForce(direction * speed * Scale * 100 * sprintMultiplier);

		}

	}

	void JumpManagement()
	{
		if (rbody.velocity.y < 10) // already jumped
		{
			anim.SetBool (jumpBool, false);
			if(timeToNextJump > 0)
				timeToNextJump -= Time.fixedDeltaTime;
		}
		if(jump)
		{			
			jump = false;
			if(timeToNextJump <= 0 && !aim)
			{
				anim.SetBool(jumpBool, true);
				rbody.velocity = new Vector3(0, jumpHeight * Scale, 0);
				timeToNextJump = jumpCooldown;
			} 
		}
	}


	void MovementManagement(float horizontal, float vertical, bool running, bool sprinting)
	{
		if(isMoving)
		{
			if(sprinting && !vrMode)
			{
				speed = sprintSpeed;
			}
			else if (!walk && !vrMode || sprint && vrMode)
			{
				speed = runSpeed;
			}
			else
			{
				speed = walkSpeed;
			}
			anim.SetFloat(speedFloat, speed, speedDampTime, Time.fixedDeltaTime);
		}
		else
		{
			speed = 0f;
			anim.SetFloat(speedFloat, 0f);
		}
		
		
		if(firstPersonMode && isMoving) {
			Vector3 direction = (GetCameraForward() * vertical + playerCamera.transform.parent.right * horizontal);
			rbody.MoveRotation(Quaternion.LookRotation(direction));
		} 
		else Rotating(horizontal, vertical);
		GetComponent<Rigidbody>().AddForce(myTransform.forward * speed * AccurateScale);
				
    }

	void TurnFPSCamera() {
		// When you are in first person mode, you move the horizontal axis moving the character
		// So with the mouse you control the horizontal axis of the character
		if(climbing || true) return;
		// Fly and Walk use the same code

		Vector3 cameraDirection = GetCameraForward();
		rbody.rotation = Quaternion.LookRotation(cameraDirection, transform.up);
		
	}

	Vector3 GetCameraForward() {
		Vector3 cameraDirection = transform.InverseTransformDirection(playerCamera.transform.forward);
		cameraDirection.y = 0f;
		return transform.TransformDirection(cameraDirection);
	}

	Vector3 GetCameraRight() {
		Vector3 cameraDirection = transform.InverseTransformDirection(playerCamera.transform.forward);
		cameraDirection.y = 0f;
		return transform.TransformDirection(cameraDirection);
	}

	Vector3 Rotating(float horizontal, float vertical)
	{
		if (!fly)
			camForwardDirection.y = 0.0f;
		camForwardDirection = camForwardDirection.normalized;

		Vector3 right = new Vector3(camForwardDirection.z, 0, -camForwardDirection.x);

		Vector3 targetDirection;

		float finalTurnSmoothing;
		
		targetDirection = camForwardDirection * vertical + right * horizontal;
		finalTurnSmoothing = turnSmoothing;		

		if((isMoving && targetDirection != Vector3.zero))
		{
			Quaternion targetRotation = Quaternion.LookRotation (targetDirection, Vector3.up);
			// fly
			if (fly)
				targetRotation *= Quaternion.Euler (90, 0, 0);

			Quaternion newRotation = Quaternion.Slerp(rbody.rotation, targetRotation, finalTurnSmoothing * Time.fixedDeltaTime);
			rbody.MoveRotation (newRotation);
		}
		//idle - fly or grounded
		if(!(Mathf.Abs(horizontalInput) > 0.9 || Mathf.Abs(verticalInput) > 0.9))
		{
			VerticalFix();
			
		}

		return targetDirection;
	}	

	void VerticalFix() {
		if(locked || isDead) return;
		Vector3 forward = myTransform.forward;
		if(forward.y == 0) return;

		forward.y = 0;
		Quaternion targetRotation = Quaternion.LookRotation(forward);
		Quaternion rotation = rbody.rotation;
		if(rotation == targetRotation) return;

		Quaternion newRotation = Quaternion.Slerp(rotation, targetRotation, turnSmoothing * Time.deltaTime);
		rbody.MoveRotation(newRotation); 
	}

	public bool IsFlying()
	{
		return fly;
	}

	public bool IsAiming()
	{
		return walk && !fly;
	}

	public bool isSprinting()
	{
		return sprint && !aim && (isMoving);
	}
	
	public bool isClimbing()
	{
		return climbing;
	}


	// Override of parent functions
	public override void DestroyObject(bool recursive = true)
	{
		if(!gameObject.activeSelf) {
			SetActive(true);
			if(hasAuthority) {
				playerCamera.SetCameraTarget(transform);
			}			
		}
		gameObject.transform.SetParent(null);
		return;
	}

	public override void Crushed(EntityBase gts)
	{
		SoundManager.This.PlayCrushed(transform.position, Scale);
		climbing = false;
		anim.SetBool(climbBool, false);
		if(CurrentState != CrushedState)
		{
			isDead = true;
			CurrentState = CrushedState;
			anim.SetBool(crushedBool, true);
			rbody.isKinematic = true;
			
		}	
	}

	void LockedState() {
		if(jump || fly || climbing)
		{
			jump = false;
			fly = false;
			climbing = false;

			rbody.isKinematic = false;
			gravity.useGravity = true;
			isDead = false;
			locked = false;

			CurrentState = DefaultState;
		}
	}

	void CrushedState()
	{
		if(jump || fly || climbing)
		{
			anim.SetBool(crushedBool, false);
			jump = false;
			fly = false;
			climbing = false;
			rbody.isKinematic = false;
			CurrentState = DefaultState;
			isDead = false;
		}
	}

	public override void OnStep()
	{
		//audioSource.pitch = 1.2f;
		audioSource.pitch = 0.9f;
		audioSource.volume = 1f;
		audioSource.spatialBlend = 1f;
		audioSource.dopplerLevel = 0f;
		audioSource.minDistance = 0.5f * Scale;
		audioSource.maxDistance = 10f * Scale;
		audioSource.PlayOneShot(SoundManager.This.GetPlayerFootstepSound());
	}

	public override void Lock() {
		base.Lock();
		if(CurrentState != LockedState) {
			CurrentState = LockedState;
		}
	}

	public void FlySound()
	{
		float targetVolume = 0.2f;
		float targetPitch = 1f;
		if(!fly) targetVolume = 0f;
		else if(!isMoving) targetVolume = 0.05f;
		else if(superFlySpeed && sprint) {
			targetVolume = 1f;
			targetPitch = 3f;
		} 
		else if(sprint || (superFlySpeed && !sprint)) {
			targetVolume = 0.5f;
			targetPitch = 1.5f;
		}
		flySource.volume = Mathf.Lerp(flySource.volume, targetVolume, 2 * Time.deltaTime);
		flySource.pitch = Mathf.Lerp(flySource.pitch, targetPitch, Time.deltaTime);		
		
	}

	protected override void HandleGiantessContact()
	{
		// to do when entering in contact with a giantess
		if(fly && transform.parent == null) {
			fly = false;
		}
	}

	public override List<Behavior> GetListBehaviors()
	{
		List<Behavior> behaviors = BehaviorLists.GetBehaviors(EntityType.Player);
		behaviors.AddRange(base.GetListBehaviors());
		return behaviors;
	}

	public override List<EntityType> GetTypesEntity() {
		List<EntityType> types = base.GetTypesEntity();
		types.Add(EntityType.Humanoid);
		return types;
	}
}

[MoonSharpUserDataAttribute]
public class PlayerEntity : Lua.Entity {
	Player player;
	ResizeCharacter resizeCharater;

	public float walkSpeed {
		get { return player.walkSpeed;}
		set { player.walkSpeed = value;}
	}

	public float runSpeed {
		get { return player.runSpeed;}
		set { player.runSpeed = value;}
	}

	public float sprintSpeed {
		get { return player.sprintSpeed;}
		set { player.sprintSpeed = value;}
	}

	public float flySpeed {
		get { return player.flySpeed;}
		set { player.flySpeed = value;}
	}

	public float superFlySpeed {
		get { return player.superSpeed;}
		set { player.superSpeed = value;}
	}

	public float climbSpeed {
		get { return player.climbSpeed;}
		set { player.climbSpeed = value;}
	}

	public float jumpPower {
		get { return player.jumpHeight;}
		set { player.jumpHeight = value;}
	}

	public bool autowalk {
		get { return player.autowalk;}
		set { player.autowalk = value;}
	}

	public float sizeChangeSpeed {
		get { return resizeCharater.sizeChangeRate;}
		set { resizeCharater.sizeChangeRate = value;}
	}

	public override float minSize {
		get { return player.minSize;}
		set { player.minSize = value; ResizeCharacter.minSize = value; }
	}

	public override float maxSize {
		get { return player.maxSize;}
		set { player.maxSize = value; ResizeCharacter.maxSize = value; }
	}

	public override float scale {
		get { return player.Scale;}
		set { resizeCharater.ChangeScale(value); }
	}

	public void Crush() {
		player.Crushed(null);
	}

	[MoonSharpHiddenAttribute]
	public PlayerEntity(Player player) : base(player) {
		this.player = player;
		resizeCharater = player.GetComponent<ResizeCharacter>();
	}
}

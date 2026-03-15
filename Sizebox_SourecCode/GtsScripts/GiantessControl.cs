using UnityEngine;
using UnityEngine.Networking;

public class GiantessControl : NetworkBehaviour {
	static RuntimeAnimatorController gtsPlayerControl;
	Giantess giantess;
	bool initialized = false;
	
	public float walkSpeed = 0.015f;
	public float sprintMultiplier = 4.5f;
	public float turnSpeed = 10f;
	public float speedChange = 5f;

	string horizontalAxis = "Horizontal";
	string verticalAxis = "Vertical";
	string sprintButton = "Sprint";

	float horizontalInput;
	float verticalInput;
	bool sprint;
	float currentInput = 0f;


	PlayerCamera cam;

	int verticalHash;

	Animator anim;
	Rigidbody rbody;

	AnimationManager animManager;

	/*
	TODO

	- Different walk styles
	- Crawl
	- Jump with impact
	- Targeted Stomp
	- Froze camera in position
	
	*/

	void Start() {
		if(!initialized) {
			Initialize();
		}
	}

	void Initialize() {
		giantess = GetComponent<Giantess>();
		giantess.isPlayer = true;
		cam = Camera.main.GetComponent<PlayerCamera>();
		rbody = giantess.movement.GetComponent<Rigidbody>();
		animManager = giantess.animationManager;

		// Prepare Animator
		anim = GetComponent<Animator>();
		if(!gtsPlayerControl) gtsPlayerControl = Resources.Load<RuntimeAnimatorController>("Animator/Controller/GTSPlayer");

		anim.runtimeAnimatorController = gtsPlayerControl;
		verticalHash = Animator.StringToHash("verticalInput");
		initialized = true;
	}

	public override void OnStartAuthority() {
		if(!enabled) return;
		
		if(!initialized) {
			Initialize();
		}

		giantess.ik.head.DisableLookAt();
		giantess.gtsMovement.onlyMoveWithPhysics = true;

		Player player = GameController.playerInstance;
		transform.position = player.myTransform.position;
		transform.rotation = player.myTransform.rotation;
		transform.localScale = Vector3.one * (player.Scale * 0.001f);
		giantess.ChangeScale(player.Scale * 0.001f);
		

		player.myTransform.SetParent(giantess.transform);
		player.SetActive(false);

		if(cam.entity != null && cam.entity.isGiantess) cam.entity.DestroyObject(true);
		cam.SetCameraTarget(giantess.transform);
	}
	
	void Update () {
		if(!hasAuthority) return;
		ReadInput();
		
	}

	void LookAround() 
	{
		Vector3 direction = cam.transform.forward;
		Vector3 lookPoint = giantess.GetEyesPosition() + direction * (giantess.AccurateScale * 100f);
		giantess.ik.LookAtPoint(lookPoint);
	}

	void FixedUpdate() {
		if(!hasAuthority) return;
		Move();
	}

	void ReadInput() {
		if(!GameController.inputEnabled) return;
		horizontalInput = Input.GetAxis(horizontalAxis);
		verticalInput = Input.GetAxis(verticalAxis);
		sprint = Input.GetButton(sprintButton);
	}

	void Move() {
		float currentSpeed = animManager.GetCurrentSpeed();
		float input = Mathf.Max(Mathf.Abs(horizontalInput), Mathf.Abs(verticalInput));
		if(sprint) input *= sprintMultiplier;

		animManager.UpdateAnimationSpeed();
		currentInput = Mathf.Lerp(currentInput, input, speedChange * Time.deltaTime);
		anim.SetFloat(verticalHash, currentInput);

		// if no input return
		if (horizontalInput * horizontalInput < 0.01f && verticalInput * verticalInput < 0.01f) {
			return;
		} 

		MakeSureUsesMyAnimator();

		// convert input to camera direction		
		Vector3 direction = GetCameraForward() * verticalInput + GetCameraRigth() * horizontalInput;
		direction.Normalize();

		float speed = walkSpeed * currentSpeed;
		if(sprint) speed *= sprintMultiplier;

		direction = Vector3.Slerp(transform.forward, direction, turnSpeed * Time.deltaTime * currentSpeed);

		rbody.position += direction * giantess.AccurateScale * speed;
		rbody.rotation = Quaternion.LookRotation(direction);
		
	}

	Vector3 GetCameraForward() {
		Vector3 forward = cam.transform.forward;
		forward.y = 0;
		return forward.normalized;
	}

	Vector3 GetCameraRigth() {
		Vector3 right = cam.transform.right;
		right.y = 0;
		return right.normalized;
	}

	void MakeSureUsesMyAnimator() {
		if(anim.runtimeAnimatorController == gtsPlayerControl) return;
		anim.runtimeAnimatorController = gtsPlayerControl;
		giantess.movement.Stop();
	}
}
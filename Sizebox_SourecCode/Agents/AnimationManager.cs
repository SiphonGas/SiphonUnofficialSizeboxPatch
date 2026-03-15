using UnityEngine;

public class AnimationManager : MonoBehaviour {
	public static bool slowdownWithSize = true;
	public static RuntimeAnimatorController defaultAnimator;

	public float transitionDuration = 0.08f;
	public Animator unityAnimator;
	Humanoid agent;
	int animationSpeedHash;
	int forwardHash;
	int rightHash;
	public float minSpeed = 0.1f;
	public float maxSpeed = 1f;
	float lastChange = 0f;
	public string nameAnimation {get; private set;}
	// This is chosen by the player in the UI
	public float speedMultiplier = 1f;
	static IOManager iomanager;
	Giantess giantess;

	

	// Use this for initialization
	void Awake () {
		agent = GetComponent<Humanoid>();

		unityAnimator = gameObject.GetComponent<Animator>();

		animationSpeedHash = Animator.StringToHash("animationSpeed");
		forwardHash = Animator.StringToHash("forward");
		rightHash = Animator.StringToHash("right");

		iomanager = IOManager.GetIOManager();
		giantess = GetComponent<Giantess>();
	}

	public static bool AnimationExists(string animation) {
		return iomanager.animationControllers.ContainsKey(animation);
	}
	
	public void PlayAnimation(string nameAnimation, bool pose = false, bool inmediate = false)
	{
		RuntimeAnimatorController animController = null;
		if(nameAnimation == null) {
			Debug.LogError("No animation name has been given");
			return;
		}

		if(!pose) {
			// error here related with animation
			if(!iomanager.animationControllers.TryGetValue(nameAnimation, out animController)) {
				Debug.LogError("Animation \"" + nameAnimation + "\" not found.");
			return;
			}
		}
		// Look for the animator controller of the animationControllers
		

		if(pose != agent.poseMode) {
			agent.SetPoseMode(pose);
		}

		this.nameAnimation = nameAnimation;
		// Debug.Log("Animation: " + nameAnimation);	
		if(pose) {
			unityAnimator.Play(nameAnimation);
			// update the static collider
			if(giantess) {
				giantess.UpdateStaticCollider();
				giantess.ik.poseIK.ResetEffectors();
			} 
		} 
		else {
			if(unityAnimator.runtimeAnimatorController != animController) {
				Vector3 originalPosition = transform.position;
				unityAnimator.runtimeAnimatorController = animController;
				transform.position = originalPosition;				
			}
			UpdateAnimationSpeed();
			if(inmediate) unityAnimator.Play(nameAnimation);		
			else {
				if(Time.time < lastChange + transitionDuration) Invoke("CrossFade", transitionDuration);
				else CrossFade();		
			}
		}
		
		
			
	}

	public bool IsInPose()
	{
		return agent.poseMode;
	}

	public bool AnimationHasFinished()
	{
		 return unityAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !unityAnimator.IsInTransition(0);
	}

	public bool TransitionEnded()
	{
		return !unityAnimator.IsInTransition(0);
	}

	void CrossFade()
	{	
		unityAnimator.CrossFade(nameAnimation, transitionDuration);
		lastChange = Time.time;
	}

	public void SetWalkSpeed(Vector3 direction, float speed) 
	{
		// vector must come normalized and multiplied by factor
		if(agent.isGiantess) { 
			speed *= GetCurrentSpeed();
			direction = direction.normalized;
		}
		ChangeInternalSpeed(speed);// reescaled to giantess speed
	}

	public void SetNewWalkSpeed(Vector3 direction) {
		float forward = direction.z;
		float right = direction.x;
		if(forward < 0) {
			forward = 0;
			right = Mathf.Sign(right);
		}
		// Debug.Log(forward);
		unityAnimator.SetFloat(forwardHash, forward);
		unityAnimator.SetFloat(rightHash, right);
	}


	public void UpdateAnimationSpeed()
	{
		if(!agent.isGiantess) {
			ChangeInternalSpeed(1f);
			return;
		}
		float newSpeed = GetCurrentSpeed();
		ChangeInternalSpeed(newSpeed);
		agent.movement.speedModifier = newSpeed;
	}

	void ChangeInternalSpeed(float speed) {
		unityAnimator.SetFloat(animationSpeedHash, speed);
	}

	public float GetCurrentSpeed()
	{
		float newSpeed = 1f;
		if(slowdownWithSize) {
			float magnitude = Mathf.Log10(agent.Height);
			newSpeed = 1f - magnitude / 5f;
			// newSpeed = (4f - Mathf.Log10(agent.Scale * 1000f)) * 0.2f;
			newSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);
		}
		return newSpeed * speedMultiplier * GameController.globalSpeed;
	}

	public void ChangeSpeed(float newSpeed)
	{
		speedMultiplier = newSpeed;
		UpdateAnimationSpeed();

	}

}

using UnityEngine;
using AI;

public class MicroNPC : Micro {
	public static bool crushEnabled = true;
	private Animator anim;
	
	
	public override void OnStartAuthority() {
		ObjectManager.Instance.OnMicroNPCSpawned(this);
		// do nothing
	}

	// Use this for initialization
	protected override void Awake()
	{
		base.Awake();
		baseScale = 1.6f;
		anim = GetComponent<Animator>();
		gameObject.AddComponent<MicroObstacleDetector>();
		movement = gameObject.AddComponent<MovementCharacter>();
		movement.entity = this;
		movement.anim = gameObject.GetComponent<AnimationManager>();
		movement.tileWidth = 0.6f;
		movement.angle = 1.2f;
		ai.EnableAI();

	}

	void FixedUpdate() {
		// fix the vertical position 
		VerticalFix();

	}

	void VerticalFix() {
		if(locked || isDead) return;
		Vector3 forward = myTransform.forward;
		if(forward.y == 0) return;

		forward.y = 0;
		Quaternion targetRotation = Quaternion.LookRotation(forward);
		Quaternion rotation = rbody.rotation;
		if(rotation == targetRotation) return;

		Quaternion newRotation = Quaternion.Slerp(rotation, targetRotation, Time.deltaTime);
		rbody.MoveRotation(newRotation); 
	}


	public override void Crushed(EntityBase gts)
	{
		if(crushEnabled) {
			isDead = true;
			GameController.Instance.eventManager.SendEvent(new CrushEvent(transform, gts));
			SoundManager.This.PlayCrushed(transform.position, Scale);
			anim.Play("Fall", 0);
			Destroy(GetComponent<CapsuleCollider>());
			Destroy(GetComponent<MovementCharacter>());
			Destroy(GetComponent<Gravity>());
			Destroy(GetComponent<Rigidbody>());
			Destroy(GetComponent<AIController>());
			Invoke("StopAnimation", 4f);
			Destroy(gameObject, 120f);

			GameObject blood = (GameObject) Instantiate(NPCSpawner.Instance.blood, transform.position + Vector3.up * 0.001f, Quaternion.identity);
			blood.transform.parent = this.transform;
			blood.transform.localScale = blood.transform.localScale * transform.lossyScale.y;
		}
	}

	void StopAnimation() {
		anim.speed = 0f;
	}
}

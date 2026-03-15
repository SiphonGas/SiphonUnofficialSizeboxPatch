using UnityEngine;
using System.Collections.Generic;
using SteeringBehaviors;

public class MovementCharacter : MonoBehaviour {
	EntityBase _entity;
	public EntityBase entity {
		get 
		{ 
			if(_entity == null) {
				_entity = GetComponent<EntityBase>();
				if(_entity == null) return null;
			} 			
			return _entity;
		}
		set 
		{ 
			_entity = value;
		}

	}
	public float tileWidth = 0.1f;
	public float angle = 1f;
	public float speedModifier = 1f;
	public float maxSpeedBase = 0.8f;
	public float MaxSpeed 
	{
		get { return maxSpeedBase * scale * speedModifier; }
	}
	public float maxAccel = 0.5f;
	public float MaxAccel 
	{
		get { return maxAccel * scale * speedModifier; }
	}
	public float maxRotation = 100f;
	public float MaxRotation
	{
		get { return maxRotation * speedModifier; }
	}
	public float maxAngularAccel = 1000f;
	public float MaxAngularAccel
	{
		get { return maxAngularAccel * speedModifier; }
	}
	public float orientation = 0f;
	public float rotation = 0f;
	public Vector3 velocity;
	protected SteeringOutput steering;
	public bool move = false;
	List<SteerBehavior> behaviors;
	public float scale { get { return entity.AccurateScale;} }
	ObstacleDetector obstacleDetector;
	public AnimationManager anim;
	public bool stop = false;
	Transform myTransform;
	int behaviorCount = 0;


	// Character dependent behaviors
	SteerBehavior look;
	SteerBehavior avoidWall;
	SteerBehavior wander;

	Vector3 forward;
	Vector3 direction;
	Rigidbody rbody;
	Vector3 upVector;

	// Walk 
	public WalkController walkController;

	void Awake()
	{
		walkController = gameObject.AddComponent<WalkController>();
		velocity = Vector3.zero;
		steering = new SteeringOutput();
		behaviors = new List<SteerBehavior>();
		obstacleDetector = GetComponent<ObstacleDetector>();
		myTransform = transform;
		upVector = Vector3.up;
	}

	// Use this for initialization
	void Start () {
		rbody = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if(!move || behaviorCount == 0) return;

		float deltaTime = Time.deltaTime;
		float magnitude = direction.z * deltaTime;

		if(magnitude != 0) {
			Vector3 displacement = forward;

			displacement.x *= magnitude;
			displacement.y *= magnitude;
			displacement.z *= magnitude;

			Vector3 position = rbody.position;
			displacement.x += position.x;
			displacement.y += position.y;
			displacement.z += position.z;

			#if UNITY_EDITOR
			Debug.DrawRay(position, forward * direction.z, Color.yellow);
			#endif

			rbody.position = displacement;
		}			

		if(rotation != 0) {
			orientation += rotation * deltaTime;
			if(orientation < 0) orientation += 360;
			else if(orientation > 360) orientation -= 360;

			rbody.rotation = Quaternion.AngleAxis(orientation, upVector);
		}

		if(stop) {
			Stop();
		}

	}

	void LateUpdate()
	{
		behaviorCount = behaviors.Count;
		if(!move || behaviorCount == 0) return;
		float deltaTime = Time.deltaTime;

		forward = myTransform.forward;
		
		// update the speed	
		direction =  myTransform.InverseTransformDirection(velocity);

		float currentMaxSpeed = MaxSpeed;

		// beware with the animator, don't forget to assing it
		if(anim != null) 
		{
			if(currentMaxSpeed > 0) 
				anim.SetWalkSpeed(direction, direction.z / currentMaxSpeed);
			else
				anim.SetWalkSpeed(direction, 0f);
		}
		

		steering.linear.x = 0;
		steering.linear.y = 0;
		steering.linear.z = 0;

		steering.angular = 0;

		for(int i = 0; i < behaviorCount; i++)
		{
			SteerBehavior behavior = behaviors[i];

			SteeringOutput behaviorSteer = behavior.GetSteering();
			float weight = behavior.weight;

			steering.linear.x += behaviorSteer.linear.x * weight;
			steering.linear.z += behaviorSteer.linear.z * weight;

			steering.angular += behaviorSteer.angular * weight;
		}
		
		velocity.x += steering.linear.x * deltaTime;
		velocity.z += steering.linear.z * deltaTime;

		rotation += steering.angular * deltaTime;

		
		if(velocity.x * velocity.x + velocity.z * velocity.z > currentMaxSpeed * currentMaxSpeed)
		{
			velocity.Normalize();
			velocity.x = velocity.x * currentMaxSpeed;
			velocity.z = velocity.z * currentMaxSpeed;
		}

		if(steering.angular == 0)
		{
			rotation = 0;
		}

		if(steering.linear.x == 0 && steering.linear.z == 0)
		{
			velocity.x = 0;
			velocity.y = 0;
			velocity.z = 0;
		}

		#if UNITY_EDITOR
		Vector3 myPosition = myTransform.position;
		Debug.DrawLine(myPosition, myPosition + velocity, Color.green);
		#endif
	}


	public void SetSteering(SteeringOutput steering)
	{
		this.steering = steering;
	}

	public void StartSeekBehavior(IKinematic target)
	{
		SteerBehavior seek = new Seek(this, target);
		behaviors.Add(seek);

		AddSharedBehaviors();
		move = true;
	}
	

	public void StartFace(IKinematic target) {
		SteerBehavior face = new Face(this, target);
		behaviors.Add(face);
		move = true;
	}

	public void StartFlee(IKinematic target) {
		SteerBehavior flee = new Flee(this, target);
		behaviors.Add(flee);

		if(wander == null) wander = new Wander(this);
		wander.weight = 0.6f;
		behaviors.Add(wander);

		AddSharedBehaviors();
		move = true;
	}

	public void Stop()
	{
		if(anim != null) anim.UpdateAnimationSpeed();
		behaviors = new List<SteerBehavior>();
		move = false;
		stop = false;

	}

	public void StartPursueBehavior(IKinematic target)
	{
		SteerBehavior pursue = new Pursue(this, target);
		behaviors.Add(pursue); 		

		AddSharedBehaviors();
		move = true;
	}

	public void StartWanderBehavior()
	{
		if(wander == null) wander = new Wander(this);
		wander.weight = 1f;
		behaviors.Add(wander);
		
		AddSharedBehaviors();
		move = true;
	}

	public void StartArriveBehavior(IKinematic target) {
		SteerBehavior arrive = new WaypointArrive(this, target);
		behaviors.Add(arrive);
		
		AddSharedBehaviors();
		move = true;
	}

	void AddSharedBehaviors() {
		if(look == null) look = new LookWhereYouAreGoing(this);
		behaviors.Add(look);

		if(avoidWall == null) avoidWall = new AvoidWall(this, obstacleDetector);
		behaviors.Add(avoidWall);
	}

	public void StartCustomBehavior() {
		walkController.Initialize(this);
		behaviors.Add(walkController.customSteer);

		if(look == null) look = new LookWhereYouAreGoing(this);
		behaviors.Add(look);

		move = true;
	}
	


}



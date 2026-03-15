using UnityEngine;
using System.Collections.Generic;

namespace SteeringBehaviors {
	public class SteeringOutput
	{
		public float angular;
		public Vector3 linear;
		public SteeringOutput ()
		{
			angular = 0.0f;
			linear = new Vector3();
		}
	}

	public class IKinematic {
		public Vector3 position {
			get { return Position(); } set { SetPosition(value); } 
		}
		public float orientation;
		public Vector3 velocity {
			get { return Velocity(); }
		}
		public float rotation;

		public virtual Vector3 Position() {
			return new Vector3();
		}

		public virtual void SetPosition(Vector3 pos) {
			return;
		}

		public virtual Vector3 Velocity() {
			return Vector3.zero;
		}
	}

	public class Kinematic : IKinematic {
		Vector3 virtualPosition;

		public Kinematic(Vector3 position) {
			virtualPosition = CenterOrigin.WorldToVirtual(position);
		}

		public override Vector3 Position() {
			return CenterOrigin.VirtualToWorld(virtualPosition);
		}

		public override void SetPosition(Vector3 position) {
			virtualPosition = CenterOrigin.WorldToVirtual(position);
		} 

		
		
	}

	public class MovementKinematic : IKinematic {
		MovementCharacter character;
		public MovementKinematic(MovementCharacter character) {
			this.character = character;
		}

		public override Vector3 Position() {
			return character.transform.position;
		}

		public override Vector3 Velocity() {
			// check if is normalized please and adjust to the player size
			return character.velocity;
		}
	}

	public class TransformKinematic : IKinematic {
		Transform transform;
		Vector3 targetPosition;
		public TransformKinematic(Transform transform) {
			this.transform = transform;
		}

		public override Vector3 Position() {
			if(transform != null) targetPosition = transform.position;
			return targetPosition;			
		}
	}

	public class SteerBehavior
	{
		public IKinematic target;
		public MovementCharacter agent;
		public float weight = 1f;
		protected SteeringOutput steering;

		public SteerBehavior(MovementCharacter agent, IKinematic target)
		{
			this.agent = agent;
			this.target = target;
			steering = new SteeringOutput();
		}

		public virtual SteeringOutput GetSteering()
		{
			return steering;
		}

		public float MapToRange(float rotation)
		{
			// helps in finding the actual direction of rotation
			// after two orientation values are substracted
			rotation %= 360f;

			if(rotation > 180f) {
				rotation -= 360f;
			} else if(rotation < -180f) {
				rotation += 360f;
			}

			return rotation;
		}

		public Vector3 GetOriAsVec (float orientation) {
			// converts orientation value to a vector
			Vector3 vector = new Vector3();
			vector.x = Mathf.Sin(orientation * Mathf.Deg2Rad);
			vector.z = Mathf.Cos(orientation * Mathf.Deg2Rad);
			return vector;
		}

		public float RandomBinomial()
		{
			return Random.Range(0f,1f) - Random.Range(0f,1f);
		}
	}

	// ========================== Seek =================================================

	public class Seek : SteerBehavior {
		public Seek(MovementCharacter agent, IKinematic target) : base(agent, target) {
			weight = 1.5f;
		}
		public override SteeringOutput GetSteering()
		{
			steering.linear.x = target.position.x - agent.transform.position.x;
			steering.linear.y = 0;
			steering.linear.z = target.position.z - agent.transform.position.z;

			steering.linear.Normalize();

			float maxAcceleration = agent.MaxAccel;
			steering.linear.x *= maxAcceleration;
			steering.linear.y *= maxAcceleration;
			steering.linear.z *= maxAcceleration;

			return steering;
		}
	}

	// ================== Flee ============================== //

	public class Flee : SteerBehavior {
		public Flee(MovementCharacter agent, IKinematic target) : base(agent, target) {}
		public override SteeringOutput GetSteering()
		{
			steering.linear = agent.transform.position - target.position;
			steering.linear.y = 0;
			steering.linear.Normalize();
			steering.linear = steering.linear * agent.MaxAccel;
			return steering;
		}

	}

	// ========================== WaypointArrive ==================================0

	public class WaypointArrive : SteerBehavior {
		Queue<IKinematic> waypointsQueue;
		IKinematic currentPoint;
		Arrive goalArrive;
		Seek seekWaypoint;
		float lastPathfind = -99f;
		float timeTolerance = 1f;
		float width = 0.2f;
		bool ignorePathfinding  = false;
		int maxTry = 10;
		int tries;
		float scale = 1.6f;
		float seekWeight = 2f;
		float arriveWeight = 0.5f;
		public WaypointArrive(MovementCharacter agent, IKinematic target) : base(agent, target) {
			weight = 2f;
			goalArrive = new Arrive(agent, target);
			waypointsQueue = new Queue<IKinematic>();
			width = agent.tileWidth;
		}

		public override SteeringOutput GetSteering()
		{
			scale = agent.entity.Height;
			float minDistance = scale * width * 4;
			if(seekWaypoint == null) {

				if(waypointsQueue.Count > 0) {

					currentPoint = waypointsQueue.Dequeue();
					Debug.DrawRay(currentPoint.position, Vector3.up * scale / 5f, Color.yellow, 5f);
					seekWaypoint = new Seek(agent, currentPoint);
					weight = seekWeight;
					return seekWaypoint.GetSteering();

				} else {

					float distanceToTarget = (target.position - agent.transform.position).magnitude;
					if(distanceToTarget > minDistance * 1.2f) {
						FindPathToTarget();
					}
					
				} 
				weight = arriveWeight;
				return goalArrive.GetSteering();
			} 
			weight = seekWeight;
			SteeringOutput output = seekWaypoint.GetSteering();
			if((agent.transform.position - currentPoint.position).magnitude < minDistance) {
				seekWaypoint = null;
			}

			return output;
			
		}

		void FindPathToTarget() {
			float time = Time.time;
			if(ignorePathfinding) {
				weight = 0.5f;
				return;
			}

			if(time < lastPathfind + timeTolerance) {
				width *= 1.5f;
				tries++;
			} else {
				tries = 0;
				width = agent.tileWidth;
			}

			if(tries > maxTry) ignorePathfinding = true;

			lastPathfind = time;

			waypointsQueue.Clear();
			List<IKinematic> waypoints = Pathfinder.instance.PlanRoute(agent.transform.position, target.position, scale, scale * width, scale * 0.1f, agent.angle);
			if(waypoints.Count > 0) {
				foreach(IKinematic waypoint in waypoints) {
					waypointsQueue.Enqueue(waypoint);
				}
			} 
		}

	}

	// ========================= Arrive =========================================
	public class Arrive : SteerBehavior {
		public float targetRadius { get { return agent.scale * 0.2f;}}
		public float slowRadius { get {return agent.scale * 0.5f;}}
		public float timeToTarget = 0.1f;

		public Arrive(MovementCharacter agent, IKinematic target) : base(agent, target) {

		}

		public override SteeringOutput GetSteering()
		{
			SteeringOutput steering = new SteeringOutput();
			Vector3 direction = target.position - agent.transform.position;
			direction.y = 0;
			float distance = direction.magnitude;
			float targetSpeed;
			if(distance < targetRadius) {
				agent.stop = true;
				return steering;
			}
			if(distance > slowRadius)
				targetSpeed = agent.MaxSpeed;
			else
				targetSpeed = agent.MaxSpeed * distance / slowRadius;
			
			Vector3 desiredVelocity = direction;
			desiredVelocity.Normalize();
			desiredVelocity *= targetSpeed;
			steering.linear = desiredVelocity - agent.velocity;
			steering.linear /= timeToTarget;
			if(steering.linear.magnitude > agent.MaxAccel)
			{
				steering.linear.Normalize();
				steering.linear *= agent.MaxAccel;
			}
			return steering;
		}
	}

	// =================================== Align =========================================
	public class Align : SteerBehavior {
		public float targetRadius = 2;
		public float slowRadius = 5;
		public float timeToTarget = 0.1f;

		public Align(MovementCharacter agent, IKinematic target) : base(agent, target) {

		}

		public override SteeringOutput GetSteering()
		{
			float rotation = target.orientation - agent.orientation;

			rotation = MapToRange(rotation);

			float rotationSize = rotation;
			if(rotation < 0) rotationSize = -rotation;

			if(rotationSize < targetRadius) {
				steering.angular = 0;
				return steering;
			}

			float targetRotation;
			if(rotationSize > slowRadius)
				targetRotation = agent.MaxRotation;
			else
				targetRotation = agent.MaxRotation * rotationSize / slowRadius;

			targetRotation *= rotation / rotationSize;

			steering.angular = targetRotation - agent.rotation;
			steering.angular /= timeToTarget;

			float angularAccel = steering.angular;
			if(steering.angular < 0) angularAccel = -steering.angular;

			float maxAngularAcceleration = agent.MaxAngularAccel;
			if(angularAccel > maxAngularAcceleration)
			{
				steering.angular /= angularAccel;
				steering.angular *= maxAngularAcceleration;
			}
			return steering;
		}
	}

	// ============================= Face ====================================================//
	public class Face : Align {
		float errorMargin = 0.05f;
		protected IKinematic targetAux;
		public Face(MovementCharacter agent, IKinematic target) : base(agent, target) {
			targetAux = target;
			target = new IKinematic();
		}

		public override SteeringOutput GetSteering()
		{
			Vector3 direction = targetAux.position - agent.transform.position;
			if(direction.magnitude > 0)
			{
				float targetOrientation = Mathf.Atan2(direction.x, direction.z);
				targetOrientation *= Mathf.Rad2Deg;
				target.orientation = targetOrientation;
				if(Mathf.Abs(agent.orientation - target.orientation) < errorMargin) agent.Stop();
			}
			return base.GetSteering();
		}

	}

	// =============================== Look where you are going ==================================== 
	public class LookWhereYouAreGoing : Align
	{
		SteeringOutput zeroSteering;
		public LookWhereYouAreGoing(MovementCharacter agent) : base(agent, null) {
			target = new IKinematic();
			zeroSteering = new SteeringOutput();
		}
		public override SteeringOutput GetSteering()
		{
			if(agent.velocity.sqrMagnitude == 0) {
				return zeroSteering;
			}
				
			target.orientation = Mathf.Atan2(agent.velocity.x, agent.velocity.z) * Mathf.Rad2Deg;
			return base.GetSteering();			
		}
	}

	// ============================ Avoid Walls ================================================
	public class AvoidWall : Seek
	{
		SteeringOutput zeroSteering;
		public float avoidDistance = 10f;
		public float AvoidDistance {
			get { return avoidDistance * agent.scale; }
		}
		public float lookAhead = 5f;
		public float LookAhead {
			get { return lookAhead * agent.scale; }
		}
		public ObstacleDetector obstacleDetector = null;

		public AvoidWall(MovementCharacter agent, ObstacleDetector obsDetector) : base(agent, null) {
			obstacleDetector = obsDetector;
			target = new Kinematic(Vector3.zero);
			weight = 1.8f;
			zeroSteering = new SteeringOutput();
		}
		public override SteeringOutput GetSteering()
		{
			if(obstacleDetector && obstacleDetector.CheckObstacle())
			{
				target.position = obstacleDetector.GetPoint() + obstacleDetector.GetNormal() * AvoidDistance;
				SteeringOutput steering = base.GetSteering();
				#if UNITY_EDITOR
				Debug.DrawRay(agent.transform.position, steering.linear, Color.red);
				#endif
				return steering;
			}
			return zeroSteering;
		}
	}

	// =================================== Wander ========================================================0
	public class Wander : Seek
	{
		public float offset = 2f;
		public float Offset {
			get { return offset * agent.scale; }
		}
		public float radius = 1f;
		public float Radius
		{
			get {return radius * agent.scale; } 
		}
		public float rate = 30f;
		float wanderOrientation;
		//float realOrientation = 0f;
		//float totalAngular = 0f;

		public Wander(MovementCharacter agent) : base(agent, null) {
			target = new Kinematic(agent.transform.position);
		}

		public override SteeringOutput GetSteering()
		{
			wanderOrientation += RandomBinomial() * rate;
			float targetOrientation = wanderOrientation + agent.orientation;

			Vector3 orientationVec = GetOriAsVec(agent.orientation);
			Vector3 targetPosition = (Offset * orientationVec) + agent.transform.position;
			targetPosition = targetPosition + (GetOriAsVec(targetOrientation) * Radius);

			target.position = targetPosition;

			return base.GetSteering();

		}
	}

	// =================== Pursue ================================= //
	public class Pursue : Seek {
		public float maxPrediction = 2f;
		IKinematic targetAux;

		public Pursue(MovementCharacter agent, IKinematic target) : base(agent, target)
		{
			targetAux = target;
			targetAux = target;
			target = new Kinematic(Vector3.zero);
		}
		
		public override SteeringOutput GetSteering()
		{
			Vector3 direction = targetAux.position - agent.transform.position;
			float distance = direction.magnitude;
			float speed = agent.velocity.magnitude;
			float prediction;
			if(speed <= distance / maxPrediction)
				prediction = maxPrediction;
			else 
				prediction = distance / speed;
			target.position = targetAux.position;
			target.position += targetAux.velocity * prediction;
			return base.GetSteering();
		}

		// ============= Helpers ===================
		Vector3 VectorMultScalar(Vector3 vector, float f) {
			vector.x *= f;
			vector.y *= f;
			vector.z *= f;
			return vector;
		
		}
	}

	
}



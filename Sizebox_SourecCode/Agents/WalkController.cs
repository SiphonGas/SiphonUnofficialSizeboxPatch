using UnityEngine;
using SteeringBehaviors;

public class WalkController : MonoBehaviour {
	public CustomSteer customSteer;

	public void Initialize(MovementCharacter agent) {
		customSteer = new CustomSteer(agent, null);
	}

	public void MoveTowards(Vector3 destination) {

	}

	public void MoveLocalDirection(Vector3 direction) {

	}

	public void MoveWorldDirection(Vector3 direction) {
		customSteer.SetLinearSteering(direction);
	}

}

public class CustomSteer : SteerBehavior {
		public CustomSteer(MovementCharacter agent, IKinematic target) : base(agent, target) {
			weight = 1;
		}

		public override SteeringOutput GetSteering()
		{
			steering.linear.y = 0;

			steering.linear.Normalize();

			float maxAcceleration = agent.MaxAccel;
			steering.linear.x *= maxAcceleration;
			steering.linear.y *= maxAcceleration;
			steering.linear.z *= maxAcceleration;

			return steering;
		}

		public void SetLinearSteering(Vector3 direction) {
			steering.linear = direction;
		}
	}

using UnityEngine;
using SteeringBehaviors;

public class ArriveAction : AgentAction {

	IKinematic target;

	public ArriveAction(IKinematic target)
	{
		name = "Arrive to " + target.position;
		this.target = target;
	}

	public override void StartAction()
	{
		agent.movement.StartArriveBehavior(target);
	}

	public override bool IsCompleted(){
		return hasStarted && !agent.movement.move;
	}


	public override void Interrupt()
	{
		agent.movement.Stop();
	}
}

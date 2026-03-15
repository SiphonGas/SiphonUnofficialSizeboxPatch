using UnityEngine;
using SteeringBehaviors;

public class PursueAction : AgentAction {

	IKinematic target;

	public PursueAction(IKinematic target)
	{
		name = "Pursue";
		this.target = target;
	}

	public override void StartAction()
	{
		agent.movement.StartPursueBehavior(target);
	}

	public override bool IsCompleted(){
		return hasStarted && !agent.movement.move;
	}


	public override void Interrupt()
	{
		agent.movement.Stop();
	}
}
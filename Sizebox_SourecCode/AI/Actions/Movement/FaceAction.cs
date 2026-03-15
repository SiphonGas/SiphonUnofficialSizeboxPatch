using UnityEngine;
using SteeringBehaviors;

public class FaceAction : AgentAction {

	IKinematic target;

	public FaceAction(IKinematic target)
	{
		name = "Face to " + target.position;
		this.target = target;
	}

	public override void StartAction()
	{
		agent.movement.StartFace(target);
	}

	public override bool IsCompleted(){
		return hasStarted && !agent.movement.move;
	}


	public override void Interrupt()
	{
		agent.movement.Stop();
	}
}

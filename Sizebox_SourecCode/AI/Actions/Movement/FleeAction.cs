using UnityEngine;
using SteeringBehaviors;

public class FleeAction : AgentAction {

	float endTime = 0f;
	IKinematic target;

	public FleeAction(IKinematic target, float time = 0f)
	{
		name = "Flee";
		this.target = target;
		if(time > 0f) endTime = Time.time + time;
	}

	public override void StartAction()
	{
		agent.movement.StartFlee(target);
		
	}

	public override bool IsCompleted(){
		if(endTime > 0f && Time.time > endTime) {
			Interrupt();
			return hasStarted;
		}  
		return hasStarted && !agent.movement.move;
	}


	public override void Interrupt()
	{
		agent.movement.Stop();
	}
}
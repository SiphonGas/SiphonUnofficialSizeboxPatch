using UnityEngine;
using SteeringBehaviors;

public class WanderAction : AgentAction {

	float endTime = 0f;

	public WanderAction(float timeLimit = 0f)
	{
		name = "Wander";
		if(timeLimit > 0f) endTime = Time.time + timeLimit;
	}

	public override void StartAction()
	{
		agent.movement.StartWanderBehavior();
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

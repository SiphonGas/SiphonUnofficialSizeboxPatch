using UnityEngine;
using SteeringBehaviors;

public class SeekAction : AgentAction {

	float endTime = 0f;
	IKinematic target;

	public SeekAction(IKinematic target, float time = 0f)
	{
		name = "Seek";
		this.target = target;
		if(time > 0f) endTime = Time.time + time;
	}

	public override void StartAction()
	{
		agent.movement.StartSeekBehavior(target);
		
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
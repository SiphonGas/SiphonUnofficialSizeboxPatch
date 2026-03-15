using UnityEngine;
public class StompAction : AgentAction {
	EntityBase target;
	GiantessIK ik;

	public StompAction(EntityBase target)
	{
		name = "Stomp: " + target.name;
		this.target = target;
	}

	public override void StartAction()
	{
		agent.ik.CrushTarget(target);
	}

	public override bool IsCompleted(){
		return hasStarted && agent.ik.crushEnded || target == null || target.isDead;
	}

	public override void Interrupt()
	{
		agent.ik.cancelFootCrush = true;
	}

}

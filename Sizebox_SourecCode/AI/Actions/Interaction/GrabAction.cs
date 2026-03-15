using UnityEngine;

public class GrabAction : AgentAction {
	EntityBase target;

	public GrabAction(EntityBase targetToGrab)
	{
		name = "Grab " + targetToGrab.name;
		target = targetToGrab;
	}

	public override void StartAction()
	{
		agent.ik.hand.GrabTarget(target);
	}

	public override bool IsCompleted() {
		return hasStarted && agent.ik.hand.GrabCompleted();
	}

	public override void Interrupt()
	{
		agent.ik.hand.CancelGrab();
	}
}

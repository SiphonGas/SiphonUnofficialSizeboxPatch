using UnityEngine;

public class LookAction : AgentAction {
	EntityBase target;

	public LookAction(EntityBase targetToLook)
	{
		name = "Look at " + targetToLook.name;
		target = targetToLook;
	}

	public override void StartAction()
	{
		agent.ik.head.LookAt(target);
	}

	public override bool IsCompleted() {
		return hasStarted;
	}
}

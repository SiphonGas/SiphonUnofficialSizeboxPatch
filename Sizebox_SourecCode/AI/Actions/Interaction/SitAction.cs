using UnityEngine;
public class SitAction : AgentAction {
	Vector3 target;
	GiantessIK ik;

	public SitAction(Vector3 target)
	{
		name = "Sit: " + target;
		this.target = target;
	}

	public override void StartAction()
	{
		ik = agent.GetComponent<GiantessIK>();
		ik.SetButtTarget(target);
	}

	public override bool IsCompleted(){
		return hasStarted && ik.IsSit();
	}

	public override void Interrupt()
	{
		ik.CancelButtTarget();
	}
}

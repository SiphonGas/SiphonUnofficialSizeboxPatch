using UnityEngine;
public class BEAction : AgentAction {
	float startTime;
	float duration;
	float speed;
	bool stop;
	Giantess giantess;
	
	public BEAction(float speed, float duration = 0f)
	{
		name = "Breast Expantion: " + speed;
		this.speed = speed;
		this.duration = duration;
		stop = duration == 0f;
	}

	public override void StartAction()
	{
		startTime = Time.time;
		giantess = agent.GetComponent<Giantess>();
		giantess.StartBreatExpantion();
		giantess.BESpeed = speed;
	}

	public override void UpdateAction()
	{
		if(stop) return;
		if(Time.time > startTime + duration) {
			giantess.BESpeed = 0f;
			stop = true;
		} 
	}

	public override bool IsCompleted(){
		return hasStarted && stop;
	}

	public override void Interrupt(){
		if(!stop) giantess.BESpeed = speed;
	}

}

using UnityEngine;
public class SizeChangeAction : AgentAction {
	float startTime;
	float duration;
	float speed;
	bool stop;
	SizeChanger sizeChanger;
	public SizeChangeAction(float speed, float duration = 0f)
	{
		name = "SizeChange: " + speed;
		this.speed = speed;
		this.duration = duration;
		stop = duration == 0f;
	}

	public override void StartAction()
	{
		startTime = Time.time;
		sizeChanger = agent.GetComponent<SizeChanger>();
		if(sizeChanger == null) sizeChanger = agent.gameObject.AddComponent<SizeChanger>();
		sizeChanger.speed = speed;
	}

	public override void UpdateAction()
	{
		if(stop) return;
		if(Time.time > startTime + duration) {
			sizeChanger.speed = 0f;
			stop = true;
		} 
	}

	public override bool IsCompleted(){
		return hasStarted && stop;
	}

	public override void Interrupt(){
		if(!stop) sizeChanger.speed = 0f;
	}

}

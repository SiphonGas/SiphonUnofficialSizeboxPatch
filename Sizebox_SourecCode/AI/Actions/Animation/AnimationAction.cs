using UnityEngine;
public class AnimationAction : AgentAction {

	string animation;
	bool smoothTransition;
	bool waitToComplete = false;

	public AnimationAction(string animation, bool smooth = true, bool waitToComplete = false)
	{
		if(animation == "") Debug.LogError("Animation name is empty");
		this.waitToComplete = waitToComplete;
		this.animation = animation;
		name = "Play Animation: " + animation;
		smoothTransition = smooth;
	}

	public override void StartAction()
	{
		agent.animationManager.PlayAnimation(animation, false, !smoothTransition);
	}

	public override bool IsCompleted(){
		if(waitToComplete) return hasStarted && agent.animationManager.AnimationHasFinished();
		return hasStarted;
	}

}

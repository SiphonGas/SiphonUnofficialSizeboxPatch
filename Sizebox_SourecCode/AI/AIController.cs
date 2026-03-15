using UnityEngine;
using UnityEngine.Networking;

namespace AI {

public class AIController : MonoBehaviour {
	EntityBase agent;
	public MentalState mentalState;
	public IDecisionMaker decisionMaker;
	public BehaviorQueue behaviorQueue;
	public ActionManager actionManager;
	bool ai = false;

	void Update () {
		Execute();
	}

	public void Initialize(EntityBase entity) {
		agent = entity;
		if(agent.gameObject.layer == Layers.microLayer)
			decisionMaker = new MicroDecisionMaker(agent);
		else if(agent.isGiantess) {
			decisionMaker = new GTSDecisionMaker(agent);
		}
		behaviorQueue = new BehaviorQueue(agent);
		actionManager = agent.actionManager;
		Debug.Assert(actionManager, "Action Manager not Found");
		mentalState = new MentalState(agent);
	}

	public void EnableAI() {
		if(!IsAiEnabled()) {
			ai = true;
		}
		
	}

	public bool IsAiEnabled() {
		return ai;
	}

	public void DisableAI() {
		if(IsAiEnabled()) {
			ai = false;
			if(behaviorQueue != null) behaviorQueue.Stop();
		}
		
	}

	public void Execute() {
		if(BehaviorLists.Instance == null) return;

		if(ai && decisionMaker != null) {
			decisionMaker.Execute();
		}

		if(behaviorQueue != null) {
			behaviorQueue.Execute();
		}

		if(actionManager != null) {
			actionManager.Execute();
		}

	}

	public void InmediateCommand(Behavior newBehavior, EntityBase target, Vector3 cursorPoint) {
		behaviorQueue.ChangeBehavior(newBehavior, target, cursorPoint);
	}

	public void ScheduleCommand(Behavior newBehavior, EntityBase target, Vector3 cursorPoint) {
		behaviorQueue.ScheduleBehavior(newBehavior, target, cursorPoint);
	}

	public bool IsIdle()
	{
		return behaviorQueue.QueueIsEmpty();
	}

	void OnEnable() {
		if(behaviorQueue != null) behaviorQueue.Stop();		
	}
}

}

using UnityEngine;
using System.Collections.Generic;

public class ActionManager : MonoBehaviour {
	AgentAction active;
	Queue<AgentAction> queue;
	Humanoid agent;

	public void ScheduleAction(AgentAction action) {
		action.agent = agent;
		queue.Enqueue(action);
		// Debug.Log("Scheduled Action: " + action.name);		
	}

	public void Execute() {
		if(active == null) {
			if(queue.Count == 0) {
				return;
			}
			else {
				active = queue.Dequeue();
				// Debug.Log("Start Action: " + active.name);
			}
		}

		if(active.IsCompleted()) {
			// Debug.Log("Completed Action: " + active.name);
			active = null;
		} else {
			active.Execute();
		}
	}

	void Awake()
	{
		queue = new Queue<AgentAction>();
		agent = GetComponent<Humanoid>();
		agent.animationManager = gameObject.AddComponent<AnimationManager>();
	}

	public void ClearQueue() {
		queue.Clear();
		// Debug.Log("Cleared Queue");
	}

	public void ClearAll() {
		ClearQueue();
		if(active != null) {
			active.Interrupt();
			active = null;
		}
		
	}

	public bool IsEmpty() {
		return queue.Count == 0 && (active == null || active.IsCompleted());
	}

}

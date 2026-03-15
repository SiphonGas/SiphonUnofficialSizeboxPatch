public class ActionSequence : AgentAction {
	protected AgentAction[] actions;
	int activeIndex = 0;
	bool initialized = false;

	public override bool CanInterrupt()
	{
		return actions[0].CanInterrupt();
	}

	public override bool CanDoBoth(AgentAction anotherAction)
	{
		foreach(AgentAction action in actions) {
			if(!action.CanDoBoth(anotherAction)) return false;
		}
		return true;
	}

	public override bool IsCompleted() {
		return activeIndex >= actions.Length;
	}

	public override void Execute()
	{
		if(!initialized) {
			foreach(AgentAction action in actions) {
				action.agent = agent;
			}
			initialized = true;
		}
		priority = actions[activeIndex].priority;

		actions[activeIndex].Execute();
		if(actions[activeIndex].IsCompleted()) {
			activeIndex++;
		}
	}

	
	
}

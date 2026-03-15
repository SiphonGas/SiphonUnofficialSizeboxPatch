public class AgentAction {
	// first action intent
	// just play an animation
	public string name = "Unknown Action";
	public int priority = 0;

	public Humanoid agent = null;
	protected bool hasStarted = false;


	public virtual bool CanInterrupt()
	{
		return false;
	}

	public virtual bool CanDoBoth(AgentAction anotherAction)
	{
		return false;
	}

	public virtual bool IsCompleted() {
		return hasStarted;
	}

	public virtual void Execute()
	{
		if(hasStarted == false) {
			StartAction();
			hasStarted = true;
		}
		UpdateAction();	
	}

	public virtual void StartAction()
	{
		// this is only executed at the beginning
	}

	public virtual void UpdateAction()
	{
		// this executes every interval
	}

	public virtual void Interrupt()
	{
		return;
	}
	
	
}

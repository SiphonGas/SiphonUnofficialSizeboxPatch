using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace AI {

public class BehaviorQueue {
	EntityBase agent;
	Lua.Entity pAgent;
	ActionManager actionManager;
	Command active;
	Queue<Command> queue;

	public void SetBehaviorByName(string name, EntityBase target, Vector3 point) {
		if(BehaviorLists.Instance == null) return;
		
		Behavior behavior = BehaviorLists.Instance.GetBehavior(name);
		if(behavior == null) return;
		ChangeBehavior(behavior, target, point);
	}

	public void QueueBehaviorByName(string name, EntityBase target, Vector3 point) {
		if(BehaviorLists.Instance == null) return;

		Behavior behavior = BehaviorLists.Instance.GetBehavior(name);
		if(behavior == null) return;
		ScheduleBehavior(behavior, target, point);
	}

	public BehaviorQueue(EntityBase entity) {
		queue = new Queue<Command>();

		actionManager = entity.actionManager;
		agent = entity;
		pAgent = agent.GetLuaEntity();

	}

	public bool IsBehaviorReactive() {
		if(active == null) return false;
		return active.behavior.react;
	}

	public void ChangeBehavior(Behavior newBehavior, EntityBase target, Vector3 cursorPoint)
	{
		// clear previous command
		Command command = new Command();

		// run the behavior
		command.behavior = newBehavior;
		command.target = target;
		command.cursorPoint = cursorPoint;
		command.agent = agent;
		command.pAgent = pAgent;

		queue.Clear();
		Stop();
		queue.Enqueue(command);
	}

	public void ScheduleBehavior(Behavior newBehavior, EntityBase target, Vector3 cursorPoint)
	{
		// clear previous command
		Command command = new Command();

		// run the behavior
		command.behavior = newBehavior;
		command.target = target;
		command.cursorPoint = cursorPoint;
		command.agent = agent;
		command.pAgent = pAgent;
		queue.Enqueue(command);
	}

	public void Stop() {		
		if(active != null) {
			actionManager.ClearAll();
			active.Exit();
			active = null;
		} 
	}

	public void Execute() {		
		if(queue.Count > 0 && CurrentBehaviorEnded()) {
			active = queue.Dequeue();
			active.Start();
		}

		if(active != null) {
			if(active.instance.autoFinish) {
				if(actionManager.IsEmpty()) {
					Stop();
					return;
				}
			}
			active.Execute();
		}
	}

	bool CurrentBehaviorEnded() {
		return active == null;
	}

	public bool QueueIsEmpty()
	{
		return queue.Count == 0 && CurrentBehaviorEnded();
	}

	public class Command {
		public Behavior behavior;
		public BehaviorInstance instance;
		public Vector3 cursorPoint;
		public EntityBase target;
		public Lua.Entity pAgent;
		public EntityBase agent;
		bool hasStarted = false;

		public void Execute() {
			if(!hasStarted) Start();
			Update();
		}

		public void Update()
		{
			instance.Update();
		}

		public void Exit() {
			instance.Exit();
		}

		public void Start()
		{
			Lua.Entity pTarget = null;
			if(target) pTarget = target.GetLuaEntity();

			instance = behavior.CreateInstance(pAgent, pTarget, cursorPoint);
			Script script = behavior.script;

			script.Globals["cursor"] = new PCursor(cursorPoint);

			
			instance.Start();

			hasStarted = true;
			
		}
	}

	

}

[MoonSharpUserDataAttribute]
public class PCursor {
	public Lua.Vector3 point;

	[MoonSharpHiddenAttribute]
	public PCursor(Vector3 cursorPoint) {
		point = new Lua.Vector3(cursorPoint);
	}
}

}

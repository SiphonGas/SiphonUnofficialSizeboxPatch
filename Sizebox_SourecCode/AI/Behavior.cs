using MoonSharp.Interpreter;
using System.Collections.Generic;
using UnityEngine;

public enum EntityType { None, Entity, Humanoid, Giantess, Micro, Player, Oneself }

public class Behavior {
	public string name = "";
	public string text = "";
	public string code = "";
	public EntityType agent = EntityType.Giantess;
	public EntityType target = EntityType.None;
	public bool ai = false;

	// MoonSharp data
	public Script script;
	public List<BehaviorScore> scores;
	public bool react = false;
	public bool hidden = false;
	Table table;
	DynValue createFunc;

	public Behavior(Table t, Script s) {
		script = s;
		table = t;
		// 1. Serialize the values
		foreach(TablePair pair in table.Pairs) {
			if(pair.Key.Type != DataType.String) continue;
			string key = pair.Key.String;
			// Debug.Log("Key: "+ pair.Key + "Value" + pair.Value);
			if(key == "name") {
				text = pair.Value.String;
			}
			else if(key == "agentType") agent = StringToType(pair.Value.String);
			else if(key == "targetType") target = StringToType(pair.Value.String);
			else if(key == "scores") AddScores(pair.Value);
			else if(key == "react") react = pair.Value.Boolean;
			else if(key == "hidden") hidden = pair.Value.Boolean;

			
		}
		// 2. Find the methods
		// test create an instance
		createFunc = script.Globals.Get("CreateInstance");
				
	}

	public BehaviorInstance CreateInstance(Lua.Entity agent, Lua.Entity target, Vector3 cursorPoint) {
		if(agent == null) Debug.LogError("No agent");
		BehaviorInstance ins = null;
		try {			
			DynValue a = script.Call(createFunc, text, agent, target, new Lua.Vector3(cursorPoint));
			ins = new BehaviorInstance(a.Table, script);
			ins.behaviorFilename = name;			
		} catch (ScriptRuntimeException ex) {
			Debug.LogError(name + " " + ex.DecoratedMessage);
		}
		return ins;
		
	}

	void AddScores(DynValue value) {
		if(value.Type != DataType.Table) {
			Debug.LogError("behavior.scores must be in a table");
			return;
		}
		scores = new List<BehaviorScore>();
		foreach(TablePair pair in value.Table.Pairs) {
			string key = pair.Key.String;
			float val = (float) pair.Value.Number;
			try {
				BehaviorScore score = new BehaviorScore(key, val);
				scores.Add(score);
			}
			catch {
				Debug.LogWarning(name + " " + key + " is not an valid score");
			}				
		}
		ai = true;

	}

	EntityType StringToType(string type) {
		if(type == "entity") return EntityType.Entity;
		if(type == "humanoid") return EntityType.Humanoid;
		if(type == "giantess") return EntityType.Giantess;
		if(type == "micro") return EntityType.Micro;
		if(type == "player") return EntityType.Player;
		if(type == "oneself") return EntityType.Oneself;
		return EntityType.None;
	}

}

public class BehaviorInstance {
	public string behaviorFilename;
	public bool autoFinish = false;
	Table instance;
	Script script;
	DynValue start;
	DynValue update;
	DynValue exit;
	public BehaviorInstance(Table i, Script s) {
		instance = i;
		script = s;

		start = GetMethod("Start"); 
		update = GetMethod("Update");
		if(update.IsNil()) autoFinish = true;
		exit = GetMethod("Exit");
	}

	DynValue GetMethod(string methodName) {
		DynValue method = instance.Get(methodName);
		if(method.IsNil()) {
			method = instance.MetaTable.Get(methodName);
			if(method.IsNil()) {
				method = instance.MetaTable.Get("__index").Table.MetaTable.Get(methodName);
			}
		}
		return method;
	}

	public void Start() {
		RunMethod(start);
	}

	public void Update() {
		if(autoFinish) return;
		RunMethod(update);
	}

	public void Exit() {
		RunMethod(exit);
	}

	void RunMethod(DynValue method) {
		if(method.IsNil()) return;
		try {
			script.Call(method, instance);
		} catch (ScriptRuntimeException ex) {
			Debug.LogError(behaviorFilename + " " + ex.DecoratedMessage);
		}
	}
	


}

public enum ScoreType { normal, fear, curiosity, hostile }

public class BehaviorScore {
	public ScoreType type;
	public float value;
	public BehaviorScore(string t, float val) {
		type = (ScoreType) System.Enum.Parse( typeof( ScoreType ), t );
		value = Mathf.Clamp01(val / 100f);
	}
}




[MoonSharpUserDataAttribute]
public class PBehavior {

	public DynValue scores {
		set {
			if(value.Type != DataType.Table) {
				Debug.LogError("behavior.scores must be in a table");
				return;
			}
			behavior.scores = new List<BehaviorScore>();
			foreach(TablePair pair in value.Table.Pairs) {
				string key = pair.Key.String;
				float val = (float) pair.Value.Number;
				try {
					BehaviorScore score = new BehaviorScore(key, val);
					behavior.scores.Add(score);
				}
				catch {
					Debug.LogWarning(behavior.name + " " + key + " is not an valid score");
				}				
			}
			behavior.ai = true;

		}
	}

	string TypeToString(EntityType type) {
		switch(type) {
			case EntityType.Entity:
				return "entity";
			case EntityType.Humanoid:
				return "humanoid";
			case EntityType.Giantess:
				return "giantess";
			case EntityType.Micro:
				return "micro";
			case EntityType.Player:
				return "player";
			case EntityType.Oneself:
				return "oneself";
		}
		return "none";
	}

	Behavior behavior;
	[MoonSharpHiddenAttribute]
	public PBehavior(Behavior behavior) {
		this.behavior = behavior;
	}
}

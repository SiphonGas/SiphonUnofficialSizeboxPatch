using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviorLists {
	public static BehaviorLists Instance;

	Dictionary<EntityType, List<Behavior>> behaviorLists;
	Dictionary<string, Behavior> behaviorDict;

	public BehaviorLists() {
		behaviorLists = new Dictionary<EntityType, List<Behavior>>();
		behaviorDict = new Dictionary<string, Behavior>();
	}

	public static void Initialize() {
		Instance = new BehaviorLists();
	}

	public void AddBehavior(Behavior b) {
		EntityType type = b.target;
		behaviorDict[b.text] = b;
		if(!behaviorLists.ContainsKey(type)) behaviorLists.Add(type, new List<Behavior>());
		List<Behavior> list;
		if(behaviorLists.TryGetValue(type, out list)) {
			list.Add(b);
		}
	}

	public List<Behavior> GetListBehaviors(EntityType type) {
		if(behaviorLists == null) return null;

		List<Behavior> behaviors = new List<Behavior>();
		List<Behavior> list;
		if(behaviorLists.ContainsKey(type) && behaviorLists.TryGetValue(type, out list)) {
			behaviors.AddRange(list);
		}
		return behaviors;
	}

	public static List<Behavior> GetBehaviors(EntityType type) {
		if(Instance != null) return Instance.GetListBehaviors(type);
		return null;
	}

	public Behavior GetBehavior(string behaviorName) {
		if(behaviorDict.ContainsKey(behaviorName)) {
			return behaviorDict[behaviorName];
		} else {
			Debug.LogError("Behavior " + behaviorName + " not found");
			return null;
		}
	}
}

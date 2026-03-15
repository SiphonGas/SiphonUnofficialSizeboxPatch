using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI {

public class GTSDecisionMaker : IDecisionMaker {
	float lastCheck = 0f;
	float checkInterval = 1f;
	EntityBase agent;
	AIController ai;
	static List<Behavior> aiBehaviors;
	int frame = 0;
	int frameModule = 30;

	public GTSDecisionMaker(EntityBase entity) {
		this.agent = entity;
		ai = entity.ai;
	}

	public override void Execute() {
		if(aiBehaviors.Count == 0) return;
		frame = (frame + 1) % frameModule;
		if(ai.IsIdle()) MakeDecision();
		else if(Time.time > lastCheck + checkInterval) MakeDecision(true);
	}

	public void MakeDecision(bool onlyReactive = false) {
		// 0. obtain the desicions and value results
		lastCheck = Time.time;
		List<Decision> decisions = CheckDecisions(onlyReactive);
		if(decisions.Count == 0) return;
		// 1. calculate the total score
		float totalScore = 0f;
		for(int i = 0; i < decisions.Count; i++) {
			totalScore += decisions[i].score;
		}

		// 2. choosen the value at Random
		float choiceValue = Random.value * totalScore;

		// 3. find  a behavior with the probability
		float accumuledScore = 0f;
		Decision selectDecision = null;
		for(int i = 0; i < decisions.Count; i++) {
			accumuledScore += decisions[i].score;
			if(accumuledScore > choiceValue) {
				selectDecision = decisions[i];
				break;
			}
		}

		// Debug.Log(entity.name + ": " + selectDecision.behavior.name);
		// if(selectDecision.target != null) Debug.Log("Target:" + selectDecision.target.name);

		// 4. schedule the choosen behavior
		if(onlyReactive) ai.InmediateCommand(selectDecision.behavior, selectDecision.target, Vector3.zero);
		else ai.ScheduleCommand(selectDecision.behavior, selectDecision.target, Vector3.zero);
	}

	public static void InitializeBehaviorList() {
		aiBehaviors = new List<Behavior>();
	}

	public static void RegisterBehavior(Behavior behav) {
		// some restrictions to make it work now
		if(behav.agent == EntityType.Giantess || behav.agent == EntityType.Humanoid) {
			aiBehaviors.Add(behav);
		}
		
	}

	public List<Decision> CheckDecisions(bool onlyReactive = false) {
		List<Decision> decisions = new List<Decision>();

		ai.mentalState.Update();
		// choose the target
		EntityBase humanoidTarget = ai.mentalState.ChooseTarget();
		EntityBase microTarget = null;
		
		if(!onlyReactive) {
			microTarget = agent.senses.GetRandomEntity(100);
			if(microTarget == null) microTarget = MicroManager.FindClosestMicro(agent, agent.Height);
		}

		foreach(Behavior beh in aiBehaviors) {
			if(onlyReactive && !beh.react) continue;
			EntityBase target = null;

			// choose target or cancel
			if(beh.target == EntityType.Micro) {
				if(microTarget == null) continue;
				target = microTarget;
			} 
			
			else if(beh.target == EntityType.Giantess || beh.target == EntityType.Humanoid) {
				if(humanoidTarget == null) continue;
				target = humanoidTarget;
			} 
			
			else if(beh.target == EntityType.Player) {
				target = GameController.playerInstance;
			} 
			
			else if(beh.target == EntityType.Oneself) {
				target = agent;
			}

			// score calculation
			float finalScore = 0f;
			foreach(BehaviorScore score in beh.scores) {
				finalScore += EvaluateScore(score);
			}

			// clamp and decide if the score is used or not
			finalScore = Mathf.Clamp01(finalScore);
			if(finalScore > 0) {
				decisions.Add(new Decision(beh, target, finalScore));
			}
			
		}
		return decisions;

	}

	float EvaluateScore(BehaviorScore score) {
		float val = 1f;
		switch(score.type) {
			case ScoreType.fear: val = ai.mentalState.fear; break;
			case ScoreType.curiosity: val = ai.mentalState.curiosity; break;
			case ScoreType.hostile: val = ai.mentalState.hostile; break;
		}
		return val * score.value;

	}

	public class Decision {
		public Behavior behavior;
		public EntityBase target;
		public float score;
		public Decision(Behavior b, EntityBase t, float s) {
			behavior = b;
			target = t;
			score = s;
		}
		
	}
}

}

using UnityEngine;
using MoonSharp.Interpreter;

namespace Lua
{
	/// <summary>
    /// Controls the AI of humanoid agent.
    /// </summary> <summary>
    /// This component will only exists if the current entity is the type humanoid and is controllable for the computer.
    /// </summary> <summary>
    /// The internal AI of characters in dividef into 3 levels.
    /// </summary> <summary>
    /// Actions: is the most simple, and is composed of a single action like Stomp(), MoveTo(), Grow(). They are added to a queue that manages the sequences of actions.
    /// </summary> <summary>
    /// Behaviors: Those are sequences of actions, but also can include custom scripting as well. Those are scripted in .lua files. One agent can do only one behavior at the time. There is the possibilty to queue multiple behaviors.
    /// </summary>  <summary>
    /// DecisionMaker: This is internal to the engine. Their function is to choose between multiple behaviors depending in some conditions.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class AI {
		EntityBase entity;

		[MoonSharpHiddenAttribute]
		public AI(EntityBase entity) {
			if(entity == null) Debug.LogError("Creating AI with no entity");
			this.entity = entity;
		}

		/// <summary>
        /// Stop all Actions, including the current one.
        /// </summary>
		public void StopAction() {
			entity.actionManager.ClearAll();
		}

		/// <summary>
        /// Will cancel all future Actions, except the current one.
        /// </summary>
		public void CancelQueuedActions() {
			entity.actionManager.ClearQueue();
		}

		/// <summary>
        /// Retrurns true if the agent is doing any action, or has queued future actions.
        /// </summary>
        /// <returns></returns>
		public bool IsActionActive() {
			return !entity.actionManager.IsEmpty();
		}

		/// <summary>
        /// Returns true if the entity is currently Executing a behavior.
        /// </summary>
        /// <returns></returns>
		public bool IsBehaviorActive() {
			return !entity.ai.behaviorQueue.QueueIsEmpty();
		}

		/// <summary>
        /// Will Stop the current behavior. It will trigger the Behavior:Exit() method.
        /// </summary>
		public void StopBehavior() {
			entity.ai.behaviorQueue.Stop();
		}

		/// <summary>
        /// Disables the AI Decision Maker, the agent will only accept commands, by the menu, or by the other scrips.
        /// </summary>
		public void DisableAI() {
			entity.ai.DisableAI();
		}
		/// <summary>
        /// Enables the AI Decision Maker, the agent will automatically choose another behavior once the current one finishes.
        /// </summary>
		public void EnableAI() {
			entity.ai.EnableAI();
		}

		/// <summary>
        /// Returns true if the AI Decision Maker is enabled. The AI Decision Maker will automatically choose another behavior for the agent once the current one finishes. 
        /// </summary>
        /// <returns></returns>
		public bool IsAIEnabled() {
			return entity.ai.IsAiEnabled();
		}

		/// <summary>
        /// Change the current behavior for another one. The name must the the string when the behavior was added.
        /// </summary>
        /// <param name="name"></param>
		public void SetBehavior(string name) {
			entity.ai.behaviorQueue.SetBehaviorByName(name, null, UnityEngine.Vector3.zero);
		}

		/// <summary>
        /// Change the current behavior for another one. The name must the the string when the behavior was added.
        /// </summary>
        /// <param name="name"></param>
		public void SetBehavior(string name, Entity target) {
			entity.ai.behaviorQueue.SetBehaviorByName(name, target.entity, UnityEngine.Vector3.zero);
		}

		/// <summary>
        /// Change the current behavior for another one. The position will act as the cursor.position.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="position"></param>
		public void SetBehavior(string name, Vector3 position) {
			entity.ai.behaviorQueue.SetBehaviorByName(name, null, position.vector3);
		}





	}
}
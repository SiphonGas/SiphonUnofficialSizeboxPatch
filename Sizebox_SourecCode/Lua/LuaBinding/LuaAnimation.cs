using MoonSharp.Interpreter;

namespace Lua
{
	/// <summary>
    /// Component to control the animation for humanoid entities.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class Animation {
		EntityBase entity;
		AnimationManager animation;

		[MoonSharpHiddenAttribute]
		public Animation(EntityBase e) {
			if(e == null) UnityEngine.Debug.LogError("Creating Animation with no entity");
			entity = e;
			animation = e.animationManager;
		}

		/// <summary>
        /// The slowest speed when the giantess is at maximun scale. (Without applying the speed multiplier)
        /// </summary>
        /// <returns></returns>
		public float minSpeed {
			get {return animation.minSpeed; }
			set {animation.minSpeed = value; }
		}

		/// <summary>
        /// The fastest speed when the giantess is at minimun scale. (Without applying the speed multiplier)
        /// </summary>
        /// <returns></returns>
		public float maxSpeed {
			get {return animation.maxSpeed; }
			set {animation.maxSpeed = value; }
		}

		/// <summary>
        /// How long it takes to transition from one animation to another
        /// </summary>
        /// <returns></returns>
		public float transitionDuration {
			get {return animation.transitionDuration; }
			set {animation.transitionDuration = value; }
		}

		/// <summary>
        /// The giantess speed is multiplied by this factor (the one that you find in the animation panel). The default value is 1. (Read Only)
        /// </summary>
        /// <returns></returns>
		public float speedMultiplier {
			get {return animation.speedMultiplier; }
		}

		/// <summary>
        /// Will transition to the specified animation.
        /// </summary>
        /// <param name="animationName"></param>
		public void Set(string animationName) {
			if(entity.actionManager != null) entity.actionManager.ScheduleAction(new AnimationAction(animationName));
			else animation.PlayAnimation(animationName);
		}
		
		/// <summary>
        /// Will transition to the specified animation and it will wait until completes before doing another action.
        /// </summary>
        /// <param name="animationName"></param>
		public void SetAndWait(string animationName) {
			if(entity.actionManager != null) entity.actionManager.ScheduleAction(new AnimationAction(animationName, true, true));
		}

		/// <summary>
        /// Returns the name of the current animation or pose. 
        /// </summary>
        /// <returns></returns>
		public string Get() {
			return animation.nameAnimation;
		}

		/// <summary>
        /// Will set the specified pose.
        /// </summary>
        /// <param name="poseName"></param>
		public void SetPose(string poseName) {
			if(entity.actionManager != null) entity.actionManager.ScheduleAction(new PoseAction(poseName));
			else animation.PlayAnimation(poseName, true);
		}

		/// <summary>
        /// Get the current Speed. 
        /// </summary>
		/// The current speed is calculated by: globalSpeed * scaleModifier * speedMultiplier. The scale modifier slow down the giantess according to the size.
        /// <returns></returns>
		public float GetSpeed() {
			return animation.GetCurrentSpeed();
		}

		/// <summary>
        /// Changes the speed of the Animation. Default is 1. The final speed can be affected by the global speed and the scale of the giantess.
        /// </summary>
        /// <param name="speed"></param>
		public void SetSpeed(float speed) {
			animation.ChangeSpeed(speed);
		}

		/// <summary>
        /// Returns true if the animation has been completed. 
        /// </summary>
        /// <returns></returns>
		public bool IsCompleted() {
			return animation.AnimationHasFinished();
		}

		/// <summary>
        /// Returns true if the animation is in transition to another animation.
        /// </summary>
        /// <returns></returns>
		public bool IsInTransition() {
			return !animation.TransitionEnded();
		}

		/// <summary>
        /// Returns true if the entity is in a pose.
        /// </summary>
        /// <returns></returns>
		public bool IsInPose() {
			return animation.IsInPose();
		}

		/// <summary>
        /// Returns true if exists an animation with the specified name.
        /// </summary>
        /// <param name="animationName"></param>
        /// <returns></returns>
		public static bool AnimationExists(string animationName) {
			return AnimationManager.AnimationExists(animationName);
		}

		/// <summary>
        /// Changes the global speed of giantess. It affects all the giantess on the scene.
        /// </summary>
        /// <param name="speed"></param>
		public static void SetGlobalSpeed(float speed) {
			GameController.ChangeSpeed(speed);
		}
	
		/// <summary>
        /// Returns the global speed of giantess.
        /// </summary>
        /// <param name="speed"></param>
		public static void GetGlobalSpeed(float speed) {
			GameController.ChangeSpeed(speed);
		}
	}
}
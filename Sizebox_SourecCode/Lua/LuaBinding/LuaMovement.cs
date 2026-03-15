using MoonSharp.Interpreter;

namespace Lua {
	/// <summary>
    /// Use this component to control the movement of agents.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class Movement {
		EntityBase entity;
		MovementCharacter movement;
		WalkController walk;
		[MoonSharpHiddenAttribute]
		public Movement(EntityBase e) {
			
			if(e == null) UnityEngine.Debug.LogError("Creating Movement with no entity");
			entity = e;
			movement = e.movement;
			walk = movement.walkController;
		}

		/// <summary>
        /// Set the movement speed. (Relative to scale)
        /// </summary>
		public void SetSpeed(float speed) {
			entity.movement.maxSpeedBase = speed;
		}

		/// <summary>
        /// Get the movement speed. (Relative to scale)
        /// </summary>
		public float GetSpeed() {
			return entity.movement.maxSpeedBase;
		}

		/// <summary>
        /// Move the character towards a point in worlds space, during one frame (to be used in Update())
        /// </summary>
        /// <param name="point">The point where the character will be heading</param>
		public void MoveTowards(Vector3 point) {
			walk.MoveTowards(point.vector3);
		}

		/// <summary>
        /// Moves to a direction relative to the player position during one frame (to be used in update). 
        /// </summary> <summary>
        /// Vector3.forward (0,0,1) will move the player forward. 
        /// </summary>
        /// <param name="direction">Direction relative to the player point of view.</param>
		public void MoveDirection(Vector3 direction) {
			walk.MoveWorldDirection(direction.vector3);
		}

		/// <summary>
        /// Turns the player the specified amount of degrees. Positive degree is right, and negative is left.
        /// </summary>
        /// <param name="degrees"></param>
		public void Turn(float degrees) {
			entity.transform.Rotate(UnityEngine.Vector3.up, degrees); 
		}

	}
}

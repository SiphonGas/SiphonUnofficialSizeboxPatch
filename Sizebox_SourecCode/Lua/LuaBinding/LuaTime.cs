using MoonSharp.Interpreter;

namespace Lua
{
	/// <summary>
    /// The interface to get time information from Unity.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class Time {
		/// <summary>
        /// The time in seconds it took to complete the last frame (Read Only).
        /// </summary>
        /// <returns></returns>
		/// Use this function to make your game frame rate independent.
		/// If you add or subtract to a value every frame chances are you should multiply with Time.deltaTime. When you multiply with Time.deltaTime you essentially express: I want to move this object 10 meters per second instead of 10 meters per frame.
		public static float deltaTime { get {return UnityEngine.Time.deltaTime;} }

		/// <summary>
        /// The total number of frames that have passed (Read Only).
        /// </summary>
        /// <returns></returns>
		public static float frameCount { get {return UnityEngine.Time.frameCount;}}

		/// <summary>
        /// The time at the beginning of this frame (Read Only). This is the time in seconds since the start of the game.
        /// </summary>
        /// <returns></returns>
		/// Returns the same value if called multiple times in a single frame. When called from inside MonoBehaviour's FixedUpdate, returns fixedTime property.
		public static float time { get {return UnityEngine.Time.time;}}

		/// <summary>
        /// The time this frame has started (Read Only). This is the time in seconds since the last level has been loaded.
        /// </summary>
        /// <returns></returns>
		public static float timeSinceLevelLoad { get {return UnityEngine.Time.timeSinceLevelLoad;}}

	}
}



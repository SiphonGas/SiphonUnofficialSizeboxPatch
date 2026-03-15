using MoonSharp.Interpreter;

namespace Lua
{
	/// <summary>
    /// Interface into the Input system.
    /// </summary> <summary>
    /// To read an axis use Input.GetAxis with one of the following default axes: "Horizontal" and "Vertical" are mapped to joystick, A, W, S, D and the arrow keys. "Mouse X" and "Mouse Y" are mapped to the mouse delta. "Fire1", "Fire2" "Fire3" are mapped to Ctrl, Alt, Cmd keys and three mouse or joystick buttons.
    /// </summary> <summary>
    /// If you are using input for any kind of movement behaviour use Input.GetAxis. It gives you smoothed and configurable input that can be mapped to keyboard, joystick or mouse. Use Input.GetButton for action like events only. Don't use it for movement, Input.GetAxis will make the script code smaller and simpler.
    /// </summary> <summary>
    /// Note also that the Input flags are not reset until "Update()", so its suggested you make all the Input Calls in the Update Loop.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class Input
	{
		/// <summary>
        /// Is any key or mouse button currently held down? (Read Only)
        /// </summary>
        /// <returns></returns>
		public static bool anyKey 
		{
			get { return UnityEngine.Input.anyKey; }
		}

		/// <summary>
        /// Returns true the first frame the user hits any key or mouse button. (Read Only)
        /// </summary>
		/// You should be polling this variable from the Update function, since the state gets reset each frame. It will not return true until the user has released all keys / buttons and pressed any key / buttons again.
        /// <returns></returns>
		public static bool anyKeyDown {
			get { return UnityEngine.Input.anyKeyDown; }
		}

		/// <summary>
        /// The current mouse position in pixel coordinates. (Read Only)
        /// </summary>
		/// The bottom-left of the screen or window is at (0, 0). The top-right of the screen or window is at (Screen.width, Screen.height).
        /// <returns></returns>
		public static Vector3 mousePosition {
			get { return new Vector3(UnityEngine.Input.mousePosition); }
		}

		/// <summary>
        /// The current mouse scroll delta. (Read Only)
        /// </summary>
        /// <returns></returns>
		public static Vector3 mouseScrollDelta {
			get { return new Vector3(UnityEngine.Input.mouseScrollDelta); }
		}

		/// <summary>
        /// Returns the value of the virtual axis identified by axisName.
        /// </summary>
		/// The value will be in the range -1...1 for keyboard and joystick input. If the axis is setup to be delta mouse movement, the mouse delta is multiplied by the axis sensitivity and the range is not -1...1.
		/// This is frame-rate independent; you do not need to be concerned about varying frame-rates when using this value.
        /// <param name="axisName"></param>
        /// <returns></returns>
		public static float GetAxis(string axisName) {
			return UnityEngine.Input.GetAxis(axisName);
		}

		/// <summary>
        /// Returns the value of the virtual axis identified by axisName with no smoothing filtering applied.
        /// </summary>
		/// The value will be in the range -1...1 for keyboard and joystick input. Since input is not smoothed, keyboard input will always be either -1, 0 or 1. This is useful if you want to do all smoothing of keyboard input processing yourself.
        /// <param name="axisName"></param>
        /// <returns></returns>
		public static float GetAxisRaw(string axisName) {
			return UnityEngine.Input.GetAxisRaw(axisName);
		}

		/// <summary>
        /// Returns true while the virtual button identified by buttonName is held down.
        /// </summary>
		/// Think auto fire - this will return true as long as the button is held down.
		/// Use this only when implementing events that trigger an action, eg, shooting a weapon. Use GetAxis for input that controls continuous movement.
        /// <param name="buttonName"></param>
        /// <returns></returns>
		public static bool GetButton(string buttonName) {
			return UnityEngine.Input.GetButton(buttonName);
		}

		/// <summary>
        /// Returns true during the frame the user pressed down the virtual button identified by buttonName.
        /// </summary>
		/// You need to call this function from the Update function, since the state gets reset each frame. It will not return true until the user has released the key and pressed it again.
		/// Use this only when implementing action like events IE: shooting a weapon.
		/// Use Input.GetAxis for any kind of movement behaviour.
        /// <param name="buttonName"></param>
        /// <returns></returns>
		public static bool GetButtonDown(string buttonName){
			return UnityEngine.Input.GetButtonDown(buttonName);
		}

		/// <summary>
        /// Returns true the first frame the user releases the virtual button identified by buttonName.
        /// </summary>
		/// You need to call this function from the Update function, since the state gets reset each frame. It will not return true until the user has pressed the button and released it again.
        /// <param name="buttonName"></param>
		/// Use this only when implementing action like events IE: shooting a weapon.
		/// Use Input.GetAxis for any kind of movement behaviour.
        /// <returns></returns>
		public static bool GetButtonUp(string buttonName){
			return UnityEngine.Input.GetButtonUp(buttonName);
		}

		/// <summary>
        /// Returns true while the user holds down the key identified by name. Think auto fire.
        /// </summary>
		/// When dealing with input it is recommended to use Input.GetAxis and Input.GetButton instead since it allows end-users to configure the keys.
        /// <param name="name"></param>
        /// <returns></returns>
		public static bool GetKey(string name) {
			return UnityEngine.Input.GetKey(name);
		}

		/// <summary>
        /// Returns true during the frame the user starts pressing down the key identified by name.
        /// </summary>
		/// You need to call this function from the Update function, since the state gets reset each frame. It will not return true until the user has pressed the key and released it again.
		/// When dealing with input it is recommended to use Input.GetAxis and Input.GetButton instead since it allows end-users to configure the keys.
        /// <param name="name"></param>
        /// <returns></returns>
		public static bool GetKeyDown(string name) {
			return UnityEngine.Input.GetKeyDown(name);
		}

		/// <summary>
        /// Returns true during the frame the user releases the key identified by name.
        /// </summary>
		/// You need to call this function from the Update function, since the state gets reset each frame. It will not return true until the user has pressed the key and released it again.
		/// When dealing with input it is recommended to use Input.GetAxis and Input.GetButton instead since it allows end-users to configure the keys.
        /// <param name="name"></param>
        /// <returns></returns>
		public static bool GetKeyUp(string name) {
			return UnityEngine.Input.GetKeyUp(name);
		}

		/// <summary>
        /// Returns whether the given mouse button is held down.
        /// </summary>
		/// button values are 0 for left button, 1 for right button, 2 for the middle button.
        /// <param name="button"></param>
        /// <returns></returns>
		public static bool GetMouseButton(int button) {
			return UnityEngine.Input.GetMouseButton(button);
		}

		/// <summary>
        /// Returns true during the frame the user pressed the given mouse button.
        /// </summary>
		/// You need to call this function from the Update function, since the state gets reset each frame. It will not return true until the user has released the mouse button and pressed it again. button values are 0 for left button, 1 for right button, 2 for the middle button.
        /// <param name="button"></param>
        /// <returns></returns>
		public static bool GetMouseButtonDown(int button) {
			return UnityEngine.Input.GetMouseButtonDown(button);
		}

		/// <summary>
        /// Returns true during the frame the user releases the given mouse button.
        /// </summary>
		/// You need to call this function from the Update function, since the state gets reset each frame. It will not return true until the user has released the mouse button and pressed it again. button values are 0 for left button, 1 for right button, 2 for the middle button.
        /// <param name="button"></param>
        /// <returns></returns>
		public static bool GetMouseButtonUp(int button) {
			return UnityEngine.Input.GetMouseButtonUp(button);
		}
	}

	/// <summary>
    /// Access to display information. 
    /// </summary> <summary>
    /// Screen class can be used to get the list of supported resolutions, switch the current resolution, hide or show the system mouse pointer.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class Screen
	{
		/// <summary>
        /// Is the game running fullscreen?
        /// </summary>
		/// It is possible to toggle fullscreen mode by changing this property:
        /// <returns></returns>
		public static bool fullScreen 
		{
			get { return UnityEngine.Screen.fullScreen; }
		}

		/// <summary>
        /// The current height of the screen window in pixels (Read Only).
        /// </summary>
		/// This is the actual height of the player window (in fullscreen it is also the current resolution).
        /// <returns></returns>
		public static int height 
		{
			get { return UnityEngine.Screen.height; }
		}

		/// <summary>
        /// The current width of the screen window in pixels (Read Only).
        /// </summary>
		/// This is the actual width of the player window (in fullscreen it is also the current resolution).
        /// <returns></returns>
		public static int width 
		{
			get { return UnityEngine.Screen.width; }
		}
	}
}

using MoonSharp.Interpreter;


namespace Lua
{
	/// <summary>
    /// Inverse Kinematics lets you animate individual bones to create procedural animations.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class IK {
		GiantessIK _ik;
		/// <summary>
        /// Enable / Disable the IK to be used in scripts.
        /// </summary>
        /// <returns></returns>
		public bool enabled { get { return _ik.luaIKEnabled;} set { _ik.luaIKEnabled = value; }}
		/// <summary>
        /// Left Foot Effector
        /// </summary>
        /// <returns></returns>
		public IKEffector leftFoot {get; private set; }
		/// <summary>
        /// Right Foot Effector
        /// </summary>
        /// <returns></returns>
		public IKEffector rightFoot {get; private set; }
		/// <summary>
        /// Left Hand Effector
        /// </summary>
        /// <returns></returns>
		public IKEffector leftHand {get; private set; }
		/// <summary>
        /// Right Hand Effector
        /// </summary>
        /// <returns></returns>
		public IKEffector rightHand {get; private set; }
		/// <summary>
        /// Body Effector (hips)
        /// </summary>
        /// <returns></returns>
		public IKEffector body {get; private set; }
		[MoonSharpHiddenAttribute]
		public IK(GiantessIK ik) {
			_ik = ik;
			leftFoot = new IKEffector(ik.leftFootEffector);
			rightFoot = new IKEffector(ik.rightFootEffector);
			leftHand = new IKEffector(ik.leftHandEffector);
			rightHand = new IKEffector(ik.rightHandEffector);
			body = new IKEffector(ik.bodyEffector);
		}

	}
}


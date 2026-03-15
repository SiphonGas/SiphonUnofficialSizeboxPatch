using MoonSharp.Interpreter;


namespace Lua
{
	/// <summary>
    /// Each effector lets you control one bone and animate the body.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class IKEffector {
		IKBone effector;
		/// <summary>
        /// Target position of the bone.
        /// </summary>
        /// <returns></returns>
		public Vector3 position {
			get { return new Vector3(effector.position);}
			set { effector.position = value.vector3; }
		}

		/// <summary>
        /// Position Weight, how much percentage the character bone must match the target position. (0 to 1).
        /// </summary>
        /// <returns></returns>
		public float positionWeight {
			get { return effector.positionWeight; }
			set { effector.positionWeight = value; }
		}

		/// <summary>
        /// Target Rotation of the bone
        /// </summary>
        /// <returns></returns>
		public Quaternion rotation {
			get { return new Quaternion(effector.rotation);}
			set { effector.rotation = value.quaternion; }
		}

		/// <summary>
        /// Rotation Weight, how much percentage the bone must match the target rotation (0 to 1).
        /// </summary>
        /// <returns></returns>
		public float rotationWeight {
			get { return effector.rotationWeight; }
			set { effector.rotationWeight = value; }
		}

		[MoonSharpHiddenAttribute]
		public IKEffector(IKBone ikBone) {
			effector = ikBone;
		}

	}
}
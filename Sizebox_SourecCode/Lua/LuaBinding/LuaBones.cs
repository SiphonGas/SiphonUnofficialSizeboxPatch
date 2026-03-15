using MoonSharp.Interpreter;

namespace Lua 
{
	[MoonSharpUserDataAttribute]
	/// <summary>
    /// Access bone transforms of humanoid characters. 
    /// </summary>
	public class Bones {
		UnityEngine.Animator _animator;

		// Body

		/// <summary>
        /// Head Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform head {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.Head); }
		}

		/// <summary>
        /// Hips Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform hips {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.Hips); }
		}

		/// <summary>
        /// Spine Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform spine {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.Spine); }
		}

		// Left Arm
		/// <summary>
        /// Left Upper Arm Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform leftUpperArm {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.LeftUpperArm); }
		}

		/// <summary>
        /// Left Lower Arm Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform leftLowerArm {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.LeftLowerArm); }
		}


		/// <summary>
        /// Left Hand Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform leftHand {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.LeftHand); }
		}

		// Right Arm
		/// <summary>
        /// Right Upper Arm Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform rightUpperArm {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.RightUpperArm); }
		}

		/// <summary>
        /// Right Lower Arm Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform rightLowerArm {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.RightLowerArm); }
		}

		/// <summary>
        /// Right Hand Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform rightHand {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.RightHand); }
		}

		// Right Leg
		/// <summary>
        /// Right Upper Leg Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform rightUpperLeg {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.RightUpperLeg); }
		}

		/// <summary>
        /// Right Lower Leg Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform rightLowerLeg {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.RightLowerLeg); }
		}

		/// <summary>
        /// Right Foot Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform rightFoot {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.RightFoot); }
		}

		// Left Leg
		/// <summary>
        /// Left Upper Leg Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform leftUpperLeg {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.LeftUpperLeg); }
		}

		/// <summary>
        /// Left Lower Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform leftLowerLeg {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.LeftLowerLeg); }
		}

		/// <summary>
        /// Left Foot Bone Transform
        /// </summary>
        /// <returns></returns>
		public Transform leftFoot {
			get { return GetBoneTransform(UnityEngine.HumanBodyBones.LeftFoot); }
		}

		[MoonSharpHiddenAttribute]
		public Bones(UnityEngine.Animator animator) 
		{
			if(animator == null) UnityEngine.Debug.LogError("No animator to create body bones.");
			_animator = animator;
		}

		Transform GetBoneTransform(UnityEngine.HumanBodyBones humanBodyBone) {
			UnityEngine.Transform bone = _animator.GetBoneTransform(humanBodyBone);
			if(bone == null) return null;
			else return new Transform(bone);
		}

	
		
	}
}



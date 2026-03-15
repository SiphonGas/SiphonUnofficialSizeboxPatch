using MoonSharp.Interpreter;

namespace Lua {
	/// <summary>
    /// Control of an object's position through physics simulation.
    /// </summary> <summary>
    /// In a script, the FixedUpdate function is recommended as the place to apply forces and change Rigidbody settings (as opposed to Update, which is used for most other frame update tasks). The reason for this is that physics updates are carried out in measured time steps that don't coincide with the frame update. FixedUpdate is called immediately before each physics update and so any changes made there will be processed directly.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class Rigidbody {

		UnityEngine.Rigidbody _rigidbody;
		/// <summary>
        /// The angular drag of the object.
        /// </summary>
		/// Angular drag can be used to slow down the rotation of an object. The higher the drag the more the rotation slows down.
        /// <returns></returns>
		public float angularDrag { get {return _rigidbody.angularDrag;} set{_rigidbody.angularDrag = value;}}

		/// <summary>
        /// The angular velocity vector of the rigidbody measured in radians per second.
        /// </summary>
		/// In most cases you should not modify it directly, as this can result in unrealistic behaviour.
        /// <returns></returns>
		public Vector3 angularVelocity{ get {return new Vector3(_rigidbody.angularVelocity);} set{ _rigidbody.angularVelocity = value.vector3;}}

		/// <summary>
        /// The drag of the object.
        /// </summary>
		/// Drag can be used to slow down an object. The higher the drag the more the object slows down.
        /// <returns></returns>
		public float drag { get {return _rigidbody.drag;} set{_rigidbody.drag = value;}}

		/// <summary>
        /// Controls whether physics will change the rotation of the object.
        /// </summary>
		/// If freezeRotation is enabled, the rotation is not modified by the physics simulation.
        /// <returns></returns>
		public bool freezeRotation { get {return _rigidbody.freezeRotation;} set{_rigidbody.freezeRotation = value;}}

		/// <summary>
        /// The mass of the rigidbody.
        /// </summary>
		/// Different Rigidbodies with large differences in mass can make the physics simulation unstable.
		/// Higher mass objects push lower mass objects more when colliding. Think of a big truck, hitting a small car.
        /// <returns></returns>
		public float mass {
			get{return _rigidbody.mass;}
			set {_rigidbody.mass = value;}
		}

		/// <summary>
        /// The maximimum angular velocity of the rigidbody. (Default 7) range { 0, infinity }.
        /// </summary>
		/// The angular velocity of rigidbodies is clamped to maxAngularVelocity to avoid numerical instability with fast rotating bodies.
        /// <returns></returns>
		public float maxAngularVelocity {
			get{return _rigidbody.maxAngularVelocity;}
			set {_rigidbody.maxAngularVelocity = value;}
		}

		/// <summary>
        /// The position of the rigidbody.
        /// </summary>
		/// Rigidbody.position allows you to get and set the position of a Rigidbody using the physics engine. If you change the position of a Rigibody using Rigidbody.position, the transform will be updated after the next physics simulation step. This is faster than updating the position using Transform.position, as the latter will cause all attached Colliders to recalculate their positions relative to the Rigidbody. 
		///If you want to continuously move a rigidbody use MovePosition instead, which takes interpolation into account.
        /// <returns></returns>
		public Vector3 position {
			get { return new Vector3(_rigidbody.position);}
			set { _rigidbody.position = value.vector3;}
		}

		/// <summary>
        /// The rotation of the rigidbody.
        /// </summary>
		/// Rigidbody.rotation allows you to get and set the rotation of a Rigidbody using the physics engine. If you change the rotation of a Rigibody using Rigidbody.rotation, the transform will be updated after the next physics simulation step. This is faster than updating the rotation using Transform.rotation, as the latter will cause all attached Colliders to recalculate their rotation relative to the Rigidbody. 
		/// If you want to continuously rotate a rigidbody use MoveRotation instead, which takes interpolation into account.
        /// <returns></returns>
		public Quaternion rotation {
			get { return new Quaternion(_rigidbody.rotation);}
			set { _rigidbody.rotation = value.quaternion;}
		}

		Gravity _gravity;
		
		/// <summary>
        /// Controls whether gravity affects this rigidbody.
        /// </summary>
		/// If set to false the rigidbody will behave as in outer space.
        /// <returns></returns>
		public bool useGravity {
			get{ 
				if(_gravity == null) _gravity = _rigidbody.GetComponent<Gravity>();
				if(_gravity != null) return _gravity.useGravity;
				else return _rigidbody.useGravity;
				}
			set {
				if(_gravity == null) _gravity = _rigidbody.GetComponent<Gravity>();
				if(_gravity != null) _gravity.useGravity = value;
				else _rigidbody.useGravity = value;
				}
		}

		/// <summary>
        /// The velocity vector of the rigidbody.
        /// </summary>
		/// In most cases you should not modify the velocity directly, as this can result in unrealistic behaviour. Don't set the velocity of an object every physics step, this will lead to unrealistic physics simulation. A typical example where you would change the velocity is when jumping in a first person shooter, because you want an immediate change in velocity.
        /// <returns></returns>
		public Vector3 velocity {
			get { return new Vector3(_rigidbody.velocity);}
			set { _rigidbody.velocity = value.vector3;}
		}

		/// <summary>
        /// The center of mass of the rigidbody in world space (Read Only).
        /// </summary>
        /// <returns></returns>
		public Vector3 worldCenterOfMass {
			get { return new Vector3(_rigidbody.worldCenterOfMass);}
		}

		/// <summary>
        /// Controls whether physics affects the rigidbody.
        /// </summary>
		/// If isKinematic is enabled, Forces, collisions or joints will not affect the rigidbody anymore. The rigidbody will be under full control of animation or script control by changing transform.position. Kinematic bodies also affect the motion of other rigidbodies through collisions or joints. Eg. can connect a kinematic rigidbody to a normal rigidbody with a joint and the rigidbody will be constrained with the motion of the kinematic body. Kinematic rigidbodies are also particularly useful for making characters which are normally driven by an animation, but on certain events can be quickly turned into a ragdoll by setting isKinematic to false.
        /// <returns></returns>
		public bool isKinematic {
			get{return _rigidbody.isKinematic;}
			set {_rigidbody.isKinematic = value;}
		}

		/// <summary>
        /// Applies a force to a rigidbody that simulates explosion effects.
        /// </summary>
		/// The explosion is modelled as a sphere with a certain centre position and radius in world space; normally, anything outside the sphere is not affected by the explosion and the force decreases in proportion to distance from the centre. However, if a value of zero is passed for the radius then the full force will be applied regardless of how far the centre is from the rigidbody.
        /// <param name="explosionForce">The force of the explosion (which may be modified by distance).</param>
        /// <param name="explosionPosition">The centre of the sphere within which the explosion has its effect.</param>
        /// <param name="explosionRadius">The radius of the sphere within which the explosion has its effect.</param>
		public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius) {
			_rigidbody.AddExplosionForce(explosionForce, explosionPosition.vector3, explosionRadius);
		}

		/// <summary>
        /// Adds a force to the Rigidbody.
        /// </summary>
		/// Force is applied continuously along the direction of the force vector.
        /// <param name="force">Force vector in world coordinates.</param>
		public void AddForce(Vector3 force){
			_rigidbody.AddForce(force.vector3);
		}

		/// <summary>
        /// Adds a force to the rigidbody relative to its coordinate system.
        /// </summary>
        /// <param name="force"></param>
		public void AddRelativeForce(Vector3 force){
			_rigidbody.AddRelativeForce(force.vector3);
		}

		/// <summary>
        /// Moves the rigidbody to position.
        /// </summary>
		/// Use Rigidbody.MovePosition to move a Rigidbody, complying with the Rigidbody's interpolation setting.
        /// <param name="position">The new position for the Rigidbody object.</param>
		public void MovePosition(Vector3 position){
			_rigidbody.MovePosition(position.vector3);
		}

		/// <summary>
        /// Rotates the rigidbody to rotation.
        /// </summary>
		/// Use Rigidbody.MoveRotation to rotate a Rigidbody, complying with the Rigidbody's interpolation setting.
        /// <param name="rot">The new rotation for the Rigidbody.</param>
		public void MoveRotation(Quaternion rot){
			_rigidbody.MoveRotation(rot.quaternion);
		}

		/// <summary>
        /// The closest point to the bounding box of the attached colliders.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
		public Vector3 ClosestPointOnBounds(Vector3 position){
			return new Vector3(_rigidbody.ClosestPointOnBounds(position.vector3));
		}

		[MoonSharpHiddenAttribute]
		public Rigidbody(UnityEngine.Rigidbody rigidbody) {
			if(rigidbody == null) UnityEngine.Debug.LogError("Creating empty rigidbody");
			_rigidbody = rigidbody;
		}
	}
}


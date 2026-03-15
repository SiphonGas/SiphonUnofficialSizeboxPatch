using MoonSharp.Interpreter;
using SteeringBehaviors;

namespace Lua
{
	/// <summary>
    /// A Entity represents Characters and Objects
    /// </summary>
	/// Entities are things that can be spawned in game, like Objects, Giantesses, Micros, and Even the player is considered and Entity.
	[MoonSharpUserDataAttribute]
	public class Entity {
		[MoonSharpHiddenAttribute]
		public EntityBase entity;

		Transform _transform;
		/// <summary>
        /// The transform component associated to this entity. It contains data about the position, rotation and scale used by the Unity Engine.
        /// </summary>
		public Transform transform {
			get { 
				if(_transform == null) _transform = new Transform(entity.myTransform);
				return _transform;
			}
		}

		Rigidbody _rigidbody;
		/// <summary>
        /// Control the physics of the Entity. Normal objects don't come with physics by default, but player and npc they have it for movement.
        /// </summary>
		public Rigidbody rigidbody {
			get {
				if(_rigidbody == null && entity.rbody != null) _rigidbody = new Rigidbody(entity.rbody);
				return _rigidbody;
			}
		}

		AI _ai;
		/// <summary>
        /// The ai component controls the ai behaviors of the entity. 
        /// </summary>
		public AI ai {
			get {
				if(_ai == null && entity.ai != null) _ai = new AI(entity);
				return _ai;
			}
		}

		Animation _animation;
		/// <summary>
        /// Component for controling the animation of humanoid entities.
        /// </summary>
		public Animation animation {
			get {
				if(_animation == null && entity.animationManager != null) _animation = new Animation(entity);
				return _animation;
			}
		}

		Bones _bones;
		UnityEngine.Animator _animator;
		/// <summary>
        /// Access the bone transforms of the model (head, hands, feet, etc).
        /// </summary>
        /// <returns></returns>
		public Bones bones {
			get {
				if(_bones == null) {
					_animator = entity.GetComponent<UnityEngine.Animator>();
					if(_animator != null) _bones = new Bones(_animator);
				}
				return _bones;
			}
		}

		IK _ik;
		/// <summary>
        /// Inverse Kinematics for the model
        /// </summary>
        /// <returns></returns>
		public IK ik {
			get {
				if(!entity.isGiantess || entity.ik == null) return null;
				if(_ik == null) {
					_ik = new IK(entity.ik);
				}
				return _ik;
			}
		}

		Senses _senses;
		/// <summary>
        /// Manages the senses of the entity such as the vision.
        /// </summary>
		public Senses senses {
			get {
				if(_senses == null && entity.senses != null) _senses = new Senses(entity);
				return _senses;
			}
		}

		/// <summary>
        /// Get the current position on world space of this entity.
        /// </summary>
        /// <returns></returns>
		public Vector3 position {
			get {return transform.position;} set { transform.position = value; }
		}

		/// <summary>
        /// Get the id associated to this entity.
        /// </summary>
        /// <returns></returns>
		public int id {
			get { return entity.id; }
		}

		/// <summary>
        /// The name of this entity.
        /// </summary>
        /// <returns></returns>
		public string name {
			get {return entity.name;} set {entity.name = value; }
		}

		/// <summary>
        /// The scale of this entity. Use this instead of the transform when possible.
        /// </summary>
        /// <returns></returns>
		public virtual float scale {
			get { return entity.Scale;} set { entity.ChangeScale(value); }
		}

		/// <summary>
        /// The max scale for this entity.
        /// </summary>
        /// <returns></returns>
		public virtual float maxSize {
			get { return entity.maxSize;} set { entity.maxSize = value; }
		}

		/// <summary>
        /// The min scale for this entity.
        /// </summary>
        /// <returns></returns>
		public virtual float minSize {
			get { return entity.minSize;} set { entity.minSize = value; }
		}

		/// <summary>
        /// Safely deletes the entity (don't deletes the player).
        /// </summary>
		public void Delete() {
			entity.DestroyObject();
		}

		/// <summary>
        /// Calculate the relative distance to a target. The scale is based on the entity scale.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
		public float DistanceTo(Entity target) {
			return DistanceTo(target.position);
		}

		/// <summary>
        /// Calculate the relative distance to a point. The scale is based on the entity scale.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
		public float DistanceTo(Vector3 point) {
			UnityEngine.Vector3 position = entity.myTransform.position;
			float xDistance = position.x - point.vector3.x;
			float zDistance = position.z - point.vector3.z;
			float distance = Mathf.Abs(xDistance * xDistance + zDistance * zDistance);
			return distance / entity.Height;
		}

		/// <summary>
        /// Returns the closes Micro Entity. It can also return the player.
        /// </summary>
        /// <returns></returns>
		public Entity FindClosestMicro() {
			Micro micro = MicroManager.FindClosestMicro(entity, entity.Height);
			if(micro) return micro.GetLuaEntity();
			return null;
		}

		/// <summary>
        /// Returns true if the entity is a humanoid character.
        /// </summary>
        /// <returns></returns>
		public bool isHumanoid() {
			return entity.isHumanoid;
		}

		/// <summary>
        /// Returns true if the entity is a giantess (.gts).
        /// </summary>
        /// <returns></returns>
		public bool isGiantess() {
			return entity.isGiantess;
		}

		/// <summary>
        /// Returns true if this entity is player controlled (micro or giantess).
        /// </summary>
        /// <returns></returns>
		public bool isPlayer() {
			return entity.isPlayer;
		}

		/// <summary>
        /// Returns true if the entity is a micro (.micro)
        /// </summary>
        /// <returns></returns>
		public bool isMicro() {
			return entity.isMicro;
		}


		public bool ActionsCompleted()
		{
			if(ai != null) return !ai.IsActionActive();
			return true;
		}

		public virtual void Sit(Lua.Vector3 place) {
			UnityEngine.Debug.LogError("Only giantess can sit.");
		}

		public virtual void Pursue(Entity target) {
			UnityEngine.Debug.LogError("Objects can't pursue.");
		}

		/// <summary>
        /// The breasts of the giantess will grow at a constant speed relative to their size (i.e. 0.1 = 10% per second). Negative values will make it shrink.
        /// </summary>
        /// <param name="speed"></param>
		public virtual void BE(float speed) {
			AddAction(new BEAction(speed));
		}

		/// <summary>
        /// The breasts of the giantess grow at a constant speed during a specified amount of time, relative to their size (i.e. 0.1 = 10% per second). Negative values will make it shrink.
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="time"></param>
		public virtual void BE(float speed, float time) {
			AddAction(new BEAction(speed, time));
		}

		// ===================== AGENTS =================================== //
		
		public void SetAnimation(string animationName) {
			if(animation != null) animation.Set(animationName);
		}

		public void CompleteAnimation(string animationName) {
			if(animation != null) animation.SetAndWait(animationName);
		}

		public void SetPose(string pose) {	
			if(animation != null) animation.SetPose(pose);
		}

		
		public void CancelAction() {
			if(ai != null) ai.StopAction();
		}

		/// <summary>
        /// The entity will grow at a constant speed relative to their size (i.e. 0.1 = 10% per second). Negative values will make it shrink.
        /// </summary>
        /// <param name="speed"></param>
		public void Grow(float speed) {
			AddAction(new SizeChangeAction(speed));		
		}

		/// <summary>
        /// The entity will grow at a constant speed during a specified amount of time, relative to their size (i.e. 0.1 = 10% per second). Negative values will make it shrink.
        /// </summary>
        /// <param name="speed"></param>
		public void Grow(float speed, float time) {
			AddAction(new SizeChangeAction(speed, time));
		}

		/// <summary>
        /// The entity will walk to a designed point.
        /// </summary>
        /// <param name="destination">Vector3 that cointains the destination point in world coordinates</param>
		public void MoveTo(Vector3 destination) {
			if(!AddAction(new ArriveAction(new Kinematic(destination.vector3)))){
				entity.Move(destination.vector3);
			}
		}

		// ================= Movement =======================//
		/// <summary>
        /// The entity will walk towards another target entity, and stop when it reaches it.
        /// </summary>
        /// <param name="targetEntity"></param>
		public void MoveTo(Entity targetEntity) {
			if(!AddAction(new ArriveAction(new TransformKinematic(targetEntity.entity.myTransform)))){
				entity.Move(targetEntity.entity.myTransform.position);	
			}
		}

		public void Face(Entity target) {
			AddAction(new FaceAction(new TransformKinematic(target.entity.myTransform)));
		} 

		/// <summary>
        /// The entity will seek toward another target. Used in the "Follow" command.
        /// </summary>
        /// <param name="target"></param>
		public void Seek(Entity target) {		
			Seek(target, 0f);
		}

		/// <summary>
        /// The entity will seek toward another target during the specified amount of time (in seconds).
        /// </summary>
        /// <param name="target"></param>
        /// <param name="time"></param>
		public void Seek(Entity target, float time) {		
			AddAction(new SeekAction(new TransformKinematic(target.entity.myTransform), time));
		}

		/// <summary>
        /// The entity will seek toward a position during the specified amount of time (in seconds).
        /// </summary>
        /// <param name="target"></param>
        /// <param name="time"></param>
		public void Seek(Vector3 position, float time) {		
			AddAction(new SeekAction(new Kinematic(position.vector3), time));
		}

		/// <summary>
        /// The entity will wander without stopping.
        /// </summary>
		public void Wander() {
			AddAction(new WanderAction());
		}

		/// <summary>
        /// The entity will wander during the specified amount of time.
        /// </summary>
        /// <param name="time"></param>
		public void Wander(float time) {		
			AddAction(new WanderAction(time));
		}

		/// <summary>
        /// The entity will flee from the target during the specified amount of time. A time of 0 will make it unlimited.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="time"></param>
		public void Flee(Entity target, float time) {
			AddAction(new FleeAction(new TransformKinematic(target.entity.myTransform), time));
		}

		/// <summary>
        /// The entity will flee from the position during the specified amount of time. A time of 0 will make it unlimited.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="time"></param>
		public void Flee(Vector3 position, float time) {
			AddAction(new FleeAction(new Kinematic(position.vector3), time));
		}

		// ============== IK ========================= //
		/// <summary>
        /// If the entity is a giantess will stomp a target.
        /// </summary>
        /// <param name="target"></param>
		public void Stomp(Entity target) {
			if(entity.ik == null) return;
			AddAction(new StompAction(target.entity));
		}

		/// <summary>
        /// If the entity is a giantess will grab a target.
        /// </summary>
        /// <param name="target"></param>
		public void Grab(Entity target) {
			if(entity.ik == null || entity.ik.hand == null) return;
			AddAction(new GrabAction(target.entity));
		}
		/// <summary>
        /// If the entity is a giantess it will look towards a target.
        /// </summary>
        /// <param name="target"></param>
		public void LookAt(Entity target) {
			if(entity.ik == null) return;
			AddAction(new LookAction(target.entity));
		}

		// === Common ==========// 
		/// <summary>
        /// Returns true if the entity is dead or has been crushed.
        /// </summary>
        /// <returns></returns>
		public bool IsDead() {
			if(entity == null) return true;
			if(!entity.gameObject.activeSelf) return true;
			return entity.isDead;
		}

		protected bool AddAction(AgentAction action) {
			if(!entity.actionManager) return false;
			entity.actionManager.ScheduleAction(action);
			return true;
		}


		[MoonSharpHiddenAttribute]
		public Entity(EntityBase entity) {
			if(entity == null) UnityEngine.Debug.LogError("Error, empty entity.");
			this.entity = entity;
		}
	}
}
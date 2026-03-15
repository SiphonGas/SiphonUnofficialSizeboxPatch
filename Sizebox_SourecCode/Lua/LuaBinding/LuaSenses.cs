using MoonSharp.Interpreter;
using System.Collections.Generic;

namespace Lua {
	/// <summary>
    /// Control the senses of a entity such as the vision.
    /// </summary>
	[MoonSharpUserDataAttribute]
	public class Senses {
		EntityBase entity;
		SenseController senses;
		[MoonSharpHiddenAttribute]
		public Senses(EntityBase entity) {
			if(entity == null) UnityEngine.Debug.LogError("Creating Senses with no entity");
			this.entity = entity;
			senses = entity.senses;
		}

		/// <summary>
        /// Changes the base visibility distance. This is multiplied to the target scale. If the entity is 0.05 compared to the agent, and the baseVisibility distance is 100, then the target will be visible at most at 5 meters (in the agent scale).
        /// </summary>
        /// <returns></returns>
		public float baseVisibilityDistance {
			get {return senses.maxDistace;}
			set {senses.maxDistace = value;}
		}

		/// <summary>
        /// Modify the field of view of the agent. The value can range from 0 to 360. 
        /// </summary>
        /// <returns></returns>
		public float fieldOfView {
			get {return senses.fieldOfView;}
			set {senses.fieldOfView = value;}
		}

		/// <summary>
        /// Returns true if the entity can see their target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
		public bool CanSee(Entity target) {
			return entity.senses.CheckVisibility(target.entity);
		}


		/// <summary>
        /// Return the list of all visible entities. You have to choose a max distance relative to the agent, this is to reduce the number of entities to perform the visibiliy tests. 
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
		public List<Entity> GetVisibleEntities(float distance) {
			List<EntityBase> entities = entity.senses.GetVisibleEntities(distance);
			List<Entity> finalEntities = new List<Entity>();
			foreach(EntityBase e in entities) {
				finalEntities.Add(e.GetLuaEntity());
			}
			return finalEntities;
		}

		/// <summary>
        /// Returns all the micros in the agent radius (relative to their size).
        /// </summary>
        /// <param name="radius"></param>
        /// <returns></returns>
		public List<Entity> GetMicrosInRadius(float radius) {
			List<Micro> micros = MicroManager.FindMicrosInRadius(entity, radius);
			List<Entity> finalEntities = new List<Entity>();
			foreach(Micro micro in micros) {
				finalEntities.Add(micro.GetLuaEntity());
			}
			return finalEntities;
		}
	}
}


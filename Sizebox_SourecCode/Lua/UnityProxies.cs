using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using Lua;

[MoonSharpUserDataAttribute]
public class UnityProxies {

	public class World {
		public float gravity {
			get { return -Physics.gravity.y; }
			set { Physics.gravity = UnityEngine.Vector3.down * value;
				  Gravity.gravity = value; }
		}
	}

	public class AllGiantess {
		public float globalSpeed {
			get { return GameController.globalSpeed; }
			set { GameController.globalSpeed = value; }
		}

		public float maxSize {
			get {return Giantess.maxScale; }
			set { Giantess.maxScale = value; }
		}

		public float minSize {
			get {return Giantess.minScale; }
			set { Giantess.minScale = value; }
		}

		Dictionary<int, Entity> instances;

		public IDictionary<int, Entity> list {
			get { return instances; }
		}

		[MoonSharpHiddenAttribute]
		public AllGiantess() {
			instances = new Dictionary<int, Entity>();
			instances.Clear();
			ObjectManager.Instance.OnGiantessAdd += AddGTS;
			ObjectManager.Instance.OnGiantessRemove += RemoveGTS;

		}

		void AddGTS(int id) {			
			Giantess gtsController;
			if( ObjectManager.Instance.giantessList.TryGetValue(id, out gtsController)) {
				Entity giantess = gtsController.GetLuaEntity();
				instances[id] = giantess;
			}
			
		}

		void RemoveGTS(int id) {
			instances.Remove(id);
		}

	}
	
}

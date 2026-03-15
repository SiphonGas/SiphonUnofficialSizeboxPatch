using System.Collections.Generic;
using UnityEngine;

public class MicroManager {
	Dictionary<int, Micro> microDictionary;
	int count;
	Transform cameraTransform;
	Vector3 cameraPosition;
	float maxDistanceBase = 500f;
	int frame;

	public MicroManager() {
		microDictionary = new Dictionary<int,Micro>();
        count = 0;
		cameraTransform = Camera.main.transform;
	}

	public void AddMicro(Micro micro) {
		micro.id = count;
		microDictionary.Add(micro.id, micro);
		Debug.Log("Micro Spawned: " + count);
		count++;
	}

	public void RemoveMicro(int id) {
		microDictionary.Remove(id);
	}

	public void Update() {
		frame++;
		if(frame % 60 != 0) return;

		cameraPosition = cameraTransform.position;
		foreach(KeyValuePair<int, Micro> pair in microDictionary) {
			Micro micro = pair.Value;
			if(micro == null) {
				continue;
			}
			MicroUpdate(micro);
		}
	}

	public void MicroUpdate(Micro micro) {

		Vector3 microPosition = micro.myTransform.position;

		float distanceX = microPosition.x - cameraPosition.x;
		float distanceY = microPosition.y - cameraPosition.y;
		float distanceZ = microPosition.z - cameraPosition.z;

		float sqrDistance = distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ;
		float maxDistance = maxDistanceBase * micro.myTransform.lossyScale.y;

		bool isVisible = micro.IsVisible();
		bool insideRadius = sqrDistance < maxDistance * maxDistance;

		if(isVisible && !insideRadius) {
			micro.Render(false);
		} else if (!isVisible && insideRadius) {
			micro.Render(true);
		}

	}

	public static Micro FindClosestMicro(EntityBase entity, float scale) {
		float radius = 0.01f * scale;
		float maxRadius = 100f * scale;
		List<Micro> micros = null;
		while(radius < maxRadius && (micros == null || micros.Count == 0)) {
			micros = CheckRadius(entity, radius);
			radius = radius * 2f;
		}
		if(micros.Count > 0)
			return micros[0];
		return null;
	}

	public static List<Micro> FindMicrosInRadius(EntityBase entity, float radius) {
		radius *= entity.Height * 0.625f;
		List<Micro> micros = CheckRadius(entity, radius);
		return micros;
	}

	static List<Micro> CheckRadius(EntityBase entity, float radius) {
		Collider[] colliders = Physics.OverlapSphere(entity.myTransform.position, radius, Layers.crushableMask);
		List<Micro> micros = new List<Micro>();
		foreach(Collider targetGo in colliders) {
			if(Giantess.ignorePlayer && targetGo.gameObject.layer == Layers.playerLayer) continue;
			Micro micro = Micro.FindMicroComponent(targetGo.gameObject);
			if(micro != null && !micro.isDead && !micro.locked && entity.id != micro.id) {
				micros.Add(micro);
			}
		}
		return micros;
	}




	
}

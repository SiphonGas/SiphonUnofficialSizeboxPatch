using UnityEngine;
using System.Collections.Generic;

public class SenseController  {
	EntityBase entity;
	Transform head;
	public float maxDistace = 200;
	public float fieldOfView = 100f;

	public SenseController(EntityBase entity) {
		this.entity = entity;
		head = entity.transform;
		if(entity.animationManager != null) {
			head = entity.animationManager.unityAnimator.GetBoneTransform(HumanBodyBones.Head);
		}
	}
	
	public bool CheckVisibility(EntityBase target) {
		return VisibilityTest(target);		
	}

	bool VisibilityTest(EntityBase target) {
		if(target == null) return false;
		Vector3 startRay = head.position;
		Vector3 endRay = target.transform.position + Vector3.up * target.Height * 0.5f;
		
		float sizeRatio = target.Height / entity.Height;
		float distance = maxDistace * entity.Height * sizeRatio;

		// Debug.DrawRay(startRay, head.forward * distance, Color.yellow);
		float angleRotation = fieldOfView * 0.5f;
		Debug.DrawRay(startRay, Quaternion.AngleAxis(-angleRotation, head.up) * head.forward * distance, Color.yellow);
		Debug.DrawRay(startRay, Quaternion.AngleAxis(angleRotation, head.up) * head.forward * distance, Color.yellow);


		Vector3 directionRay = (endRay - startRay).normalized * distance;
		float distanceToPlayer = Vector3.Distance(startRay, endRay);

		if( distanceToPlayer < distance) {
			

			// Check Angle
			float angle = Vector3.Angle(head.forward, directionRay);
			if(angle < angleRotation) {

				// Do Raycast
				RaycastHit hit;
				bool hasHit = Physics.Raycast(startRay, directionRay, out hit, distanceToPlayer, Layers.visibilityMask);
				if(!hasHit || hit.transform == target.transform) {
					Debug.DrawLine(startRay, endRay, Color.green);
					return true;					
				}
				Debug.DrawLine(startRay, hit.point, Color.red);
			}
		} 
		return false;

	}

	public List<EntityBase> GetVisibleEntities(float maxDistance) {
		Vector3 center = entity.transform.position;
		float radius = entity.Height * maxDistace;
		List<EntityBase> entities = new List<EntityBase>();
		List<EntityBase> visibleEntities = new List<EntityBase>();

		Collider[] colliders = Physics.OverlapSphere(center, radius, Layers.crushableMask);
		foreach(Collider targetGo in colliders) {
			if(Giantess.ignorePlayer && targetGo.gameObject.layer == Layers.playerLayer) continue;
			Micro microAgent = Micro.FindMicroComponent(targetGo.gameObject);
			if(microAgent != null && !microAgent.isDead && !microAgent.locked) {
				entities.Add(microAgent);
			}
		}

		foreach(EntityBase target in entities) {
			if(VisibilityTest(target)) {
				visibleEntities.Add(target);
			}
		}

		return visibleEntities;
	}

	public EntityBase GetRandomEntity(float distance) {
		List<EntityBase> entities = GetVisibleEntities(distance);
		if(entities.Count > 0) return entities[Random.Range(0, entities.Count)];
		return null;
	}

}

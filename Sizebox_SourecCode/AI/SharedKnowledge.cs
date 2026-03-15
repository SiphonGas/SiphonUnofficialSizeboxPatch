using UnityEngine;

public class SharedKnowledge : IListener {
	public EntityBase dangerEntity;
	float baseRadius = 3f;
	public SharedKnowledge() {
		GameController.Instance.eventManager.RegisterListener(this, Interest.Crush);
		dangerEntity = GameController.playerInstance;
	}

	public void OnNotify(IEvent e) {
		CrushEvent ce = (CrushEvent) e;
		if(ce.crusher != null) dangerEntity = ce.crusher;
	}

	public float CheckDanger(EntityBase agent) {
		float dangerValue = 0f;
		if(dangerEntity == null) return dangerValue;
		// 1. check size
		dangerValue = SizeFactor(agent, dangerEntity);
		if(dangerValue == 0f) return dangerValue;

		// 2. check distance
		float radius = baseRadius * dangerEntity.Height;
		Vector3 agentPosition = agent.transform.position;
		Vector3 gtsPosition = dangerEntity.transform.position;
		dangerValue *= DistanceFactor(gtsPosition, agentPosition, radius);

		return dangerValue;

	}

	public float SizeFactor(EntityBase micro, EntityBase gts) {
		float relation = gts.Height / micro.Height;
		return Mathf.Clamp01((relation - 1f) / 10f);
	}

	public float DistanceFactor(Vector3 center, Vector3 point, float radius) {
		float x = point.x - center.x;
		float z = point.z - center.z;
		
		float partA = x*x + z*z;
		
		if(partA < radius * radius) {
			float d = Mathf.Sqrt(partA);
			float val = 1f - d / radius;
			return val * val;
		}
		else return 0f;
	}
}

using UnityEngine;

public class MentalState {
	
	public float fear;
	public float curiosity;
	public float hostile;
	EntityBase entity;
	public MentalState(EntityBase entity) {
		this.entity = entity;
		curiosity = Mathf.Pow(Random.value, 8);
		// Debug.Log("curiosity: " + curiosity);
		hostile = Mathf.Pow(Random.value, 2);
		// Debug.Log("hostile: " + hostile);
	}

	public void Update() {
		fear = GameController.Instance.sharedKnowledge.CheckDanger(entity) * (1 - curiosity);
		if(fear > Random.value) fear = 1f;
		else fear = 0f;
	}

	public EntityBase ChooseTarget() {
		EntityBase target = GameController.Instance.sharedKnowledge.dangerEntity;
		if(target == null) return null;
		
		if(entity.transform.IsChildOf(target.transform)) return null;
		return target;
	}
}

using UnityEngine;
using System.Collections;

public class EventIndicator : MonoBehaviour {
	public GameObject target;
	public CreateMesh meshHelper;
	public string nameMorph;
	public float weight;
	public float time;


	void OnTriggerEnter(Collider col) {
		if (col.gameObject.layer == LayerMask.NameToLayer("Player")){
			Debug.Log("Player entered in the trigger" + col.gameObject.layer);
			if(target == null) {
				Debug.Log("There is no target");
				return;
			}
			GetComponent<ParticleSystem>().Stop();
			MMD4MecanimMorphHelper mf = target.AddComponent<MMD4MecanimMorphHelper>();
			mf.morphName = this.nameMorph;
			mf.morphWeight = this.weight;
			mf.morphSpeed = this.time;
			if(meshHelper == null) {
				Debug.Log("The mesh is no updated");
				return;
			}
			StartCoroutine("UpdateMesh");
		}
	}

	IEnumerator UpdateMesh() {
		yield return new WaitForSeconds(time);
		meshHelper.update = true;
		Destroy(gameObject);

	}
}

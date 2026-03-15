using UnityEngine;
using System.Collections;

public class TerrainTree : MonoBehaviour, IDestructible {
	bool fallen = false;

	public void Destroy(Vector3 contactPoint, float scale) {
		if(fallen || scale < transform.lossyScale.y * 6f) return;
		fallen = true;

		Collider collider = GetComponent<Collider>();
		if(collider != null) collider.enabled = false;

		StartCoroutine(FallCoroutine(contactPoint));
	}

	public IEnumerator FallCoroutine(Vector3 contactPoint) {
		Vector3 direction = transform.position - contactPoint;
		float y = Quaternion.LookRotation(direction).eulerAngles.y;
		Quaternion targetRotation = Quaternion.Euler(90, y, 0);
		while(transform.localRotation != targetRotation) {
			transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, 8 * Time.deltaTime);
			yield return null;
		}
	}


}

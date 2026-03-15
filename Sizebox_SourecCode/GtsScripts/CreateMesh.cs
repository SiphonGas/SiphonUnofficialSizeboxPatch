using UnityEngine;
using System.Collections;

[RequireComponent (typeof (MeshCollider))]
public class CreateMesh : MonoBehaviour {
	SkinnedMeshRenderer meshRenderer;
    MeshCollider thisCollider;
	public bool update;
    public float scale = 1f;
    public Giantess gtsController;
    // private bool timeSetted = false;
	 
	// Use this for initialization
	void Awake () {
		meshRenderer = GetComponent<SkinnedMeshRenderer>();
        thisCollider = GetComponent<MeshCollider>();
        gameObject.layer = Layers.objectLayer;
	}

    void UpdateMeshCollider()
    {
        UpdateCollider();
    }

    
    public void UpdateCollider() {
        if(meshRenderer.isVisible)
        {
            Mesh colliderMesh = new Mesh();
            meshRenderer.BakeMesh(colliderMesh);
            colliderMesh.RecalculateNormals();
            colliderMesh.RecalculateBounds();
            thisCollider.sharedMesh = null;

            // DebugHelper.StartCron();
            thisCollider.sharedMesh = colliderMesh;
            // DebugHelper.LogCron("Static Mesh");
        }
        else
        {
            StartCoroutine(UpdateWhenVisible());
        }		
	}

    public void DestroyMesh()
    {
        thisCollider.sharedMesh = null;
    }

    IEnumerator UpdateMesh() {
        yield return new WaitForSeconds(Random.Range(1f,2f));
        UpdateCollider();
    }

    IEnumerator UpdateWhenVisible()
    {
        while(meshRenderer.isVisible == false)
        {
            yield return null;
        }
        UpdateCollider();
    }
}

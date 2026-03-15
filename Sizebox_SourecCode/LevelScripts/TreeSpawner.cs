using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour {
	GameObject[] treePrefabs;
	PlayerCamera mainCamera;
	int maxTrees = 2000;
	int totalTreeCount = 0;
	int treesPerPatch = 20;

	float patchSize = 300;
	int patchX = 0;
	int patchZ = 0;
	int depth = 3;


	List<Patch> patchs;
	Patch poolingPatch;
	Queue<Patch> patchsToFill;

	public class Patch {
		public int x;
		public int z;
		public GameObject[] trees;
		public int treeCount = 0;

		public bool IsVisible() {
			if(treeCount == 0) return false;
			Vector3 viewport = Camera.main.WorldToViewportPoint(trees[0].transform.position);
			if(viewport.x < 0 || viewport.x > 1) return false;
			if(viewport.y < 0 || viewport.y > 1) return false;
			if(viewport.z < 0) return false;
			return true;
		}
	}

	Vector3 highestPoint = new Vector3(0,1000,0);
	float lowestPoint = 200;
	Transform root;
	bool editorMode = false;

	// Use this for initialization
	void Start () {

		#if UNITY_EDITOR
		editorMode = true;
		root = new GameObject("Trees").transform;
		#endif

		treePrefabs = Resources.LoadAll<GameObject>("Nature/Tree");

		mainCamera = Camera.main.GetComponent<PlayerCamera>();
		patchs = new List<Patch>();

		patchsToFill = new Queue<Patch>();
	}

	void PlaceTree(Vector3 position, Patch patch) {
		Vector3 hight = CenterOrigin.VirtualToWorld(highestPoint);
		hight.x = position.x;
		hight.z = position.z;

		RaycastHit hit;
		bool hasHit = Physics.Raycast(hight, Vector3.down, out hit, highestPoint.y - lowestPoint, Layers.walkableMask);
		if(!hasHit) return;
		if(hit.collider.gameObject.layer != Layers.mapLayer) return;
		// check angle
		float angle = Vector3.Angle(Vector3.up, hit.normal);
		if(angle > 30) return;

		SpawnTree(hit.point, patch);

	}

	void SpawnTree(Vector3 point, Patch patch) {
		if(totalTreeCount >= maxTrees) {
			PoolTree(point, patch);
		} else {
			CreateTree(point, patch);
		}
		
	}

	void PoolTree(Vector3 point, Patch patch) {
		GameObject tree = GetTreeFromPool();
		if(tree == null) {
			// Debug.LogWarning("No tree found");
			return;
		}
		patch.trees[patch.treeCount] = tree;
		tree.transform.position = point;
		patch.treeCount++;
	}

	GameObject GetTreeFromPool() {
		if(poolingPatch == null || poolingPatch.treeCount == 0) {
			PoolPatch();
		}
		if(poolingPatch == null || poolingPatch.treeCount == 0) {
			// Debug.LogWarning("No pool found");
			return null;
		}
		
		poolingPatch.treeCount--;
		GameObject tree = poolingPatch.trees[poolingPatch.treeCount];
		poolingPatch.trees[poolingPatch.treeCount] = null;
		return tree;

	}

	void PoolPatch() {
		if(patchs.Count == 0) {
			//Debug.LogWarning("No patches found");
			return;
		}
		// find closest patch
		int index = 0;
		int maxDistance = 0;
		for(int i = 0; i < patchs.Count; i++) {
			Patch p = patchs[i];
			int distance = Mathf.Abs(p.x - patchX) + Mathf.Abs(p.z - patchZ);
			if(distance > maxDistance && !p.IsVisible()) {
				index = i;
				maxDistance = distance;
			}
		}
		poolingPatch = patchs[index];
		// Debug.Log("Pooling patch with " + poolingPatch.treeCount + " trees at distance " + maxDistance );
		patchs.RemoveAt(index);

		if(poolingPatch.treeCount == 0) {
			PoolPatch();
		}
	}

	void CreateTree(Vector3 point, Patch patch) {
		patch.trees[patch.treeCount] = Instantiate<GameObject>(treePrefabs[Random.Range(0, treePrefabs.Length)], point, Quaternion.identity);
		if(editorMode) {
			patch.trees[patch.treeCount].transform.SetParent(root);
		}
		patch.treeCount++;
		totalTreeCount++;
	}

	void Update() {
		CheckPatch();
		FillPatchs();
	}

	void CheckPatch() {
		if(mainCamera.target == null) return;
		Vector3 currentPostion = CenterOrigin.WorldToVirtual(mainCamera.target.position);
		int x = (int) Mathf.Floor((currentPostion.x / patchSize));
		int z = (int) Mathf.Floor((currentPostion.z / patchSize));

		if((x != patchX || z != patchZ)) {
			int dx = x - patchX;
			int dz = z - patchZ;
			patchX = x;
			patchZ = z;
			RenderNeighboors(x + dx * depth, z + dz * depth);
		}
		
	}

	void FillPatchs() {
		if(patchsToFill.Count == 0) return;
		Patch patch = patchsToFill.Dequeue();
		RenderPatch(patch);
		patchs.Add(patch);
	}

	void RenderNeighboors(int x, int z) {
		int depth = 4;
		for (int dx = -depth; dx <= depth; dx++ ) {
			for(int dz = -depth; dz <= depth; dz++) {
				int nx = x + dx;
				int nz = z + dz;
				if(!PatchExists(nx, nz)) {
					Patch patch = CreatePatch(nx, nz);
					patchsToFill.Enqueue(patch);					
				}
			}
		}
	}

	void RenderPatch(Patch patch) {

		Vector3 patchStart = CenterOrigin.VirtualToWorld(new Vector3(patch.x * patchSize, 0, patch.z * patchSize));
		for(int i = 0; i < treesPerPatch; i++) {
			float dx = Random.Range(0f, patchSize);
			float dz = Random.Range(0f, patchSize);
			Vector3 randomPoint = new Vector3(dx, 0, dz);
			Vector3 point = patchStart + randomPoint;
			PlaceTree(point, patch);			
		}
	}

	Patch CreatePatch(int x, int z) {
		Patch patch = new Patch();
		patch.x = x;
		patch.z = z;
		patch.trees = new GameObject[treesPerPatch];
		patch.treeCount = 0;
		return patch;
	}

	bool PatchExists(int x, int z) {
		Patch patch;
		for(int i = 0; i < patchs.Count; i++) {
			patch = patchs[i];
			if(patch.x == x && patch.z == z) {
				return true;
			}
		}

		foreach(Patch p in patchsToFill) {
			if(p.x == x && p.z == z) {
				return true;
			}
		}

		return false;
	}
}

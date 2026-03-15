using UnityEngine;
using System.Collections.Generic;

public class DynamicMeshCreator : MonoBehaviour {

	// Use this for initialization
	void Start () {
		// access the skinned mesh renderer data
		SkinnedMeshRenderer renderer = GetComponent<SkinnedMeshRenderer>();
		Mesh oldMesh = renderer.sharedMesh;
		int bonesCount = renderer.bones.Length;

		
		

		// Obtain the total list of faces
		int totalFaces = oldMesh.triangles.Length / 3;

		// create list of triangles for each bone and initialize them
		List<int>[] newTriangles = new List<int>[bonesCount];

		for(int i = 0; i < newTriangles.Length; i++)
		{
			newTriangles[i] = new List<int>();
		}

		// each vertex will have only one bone, for simplicity
		bool[,] vertexToBone = new bool[bonesCount, oldMesh.vertexCount];

		for(int b = 0; b < bonesCount; b++) {
			for(int i = 0; i < oldMesh.vertexCount; i++) {
				vertexToBone[b, i] = false;
			}
		}
		// each face will be only in one bone, look for them
		// in each face the three vertices need to be in the same face to count
		int t = 0;
		for(int i = 0; i < totalFaces; i++)
		{
			int baseIndex = i * 3;

			int v1 = oldMesh.triangles[baseIndex];
			int v2 = oldMesh.triangles[baseIndex + 1];
			int v3 = oldMesh.triangles[baseIndex + 2];

			int bi0 = oldMesh.boneWeights[v1].boneIndex0;
			int bi1 = oldMesh.boneWeights[v2].boneIndex0;
			int bi2 = oldMesh.boneWeights[v3].boneIndex0;

			int[] bones = {bi0, -1, -1};

			if(bi0 != bi1){
				bones[1] = bi1;
				if(bi1 != bi2) {
					bones[2] = bi2;
				}
			} else if(bi0 != bi2) {
				bones[1] = bi2;
			}

			for(int b = 0; b < bones.Length && bones[b] > -1; b++)
			{
				int bone = bones[b];
				newTriangles[bone].Add(v1);
				newTriangles[bone].Add(v2);
				newTriangles[bone].Add(v3);

				vertexToBone[bone, v1] = true;
				vertexToBone[bone, v2] = true;
				vertexToBone[bone, v3] = true;

				t++;

			}
		}
		Debug.Log(t + " faces found");
		


		// this is the index for each list when adding vertex
		int[] newVertexCount = new int[bonesCount];
		for(int i = 0; i < bonesCount; i++) newVertexCount[i] = 0;

		// this will be the translaction from original index to per bone index		
		int[,] translation = new int[bonesCount, oldMesh.vertexCount];

		// count all vertex in each bone to create the arrays
		for(int b = 0; b < bonesCount; b++) {
			for(int i = 0; i < oldMesh.vertexCount; i++)
			{
				translation[b, i] = -1;
				if(vertexToBone[b, i])
					newVertexCount[b] += 1;
			}
		}

		// array creation to add them to each mesh in bone
		List<Vector3[]> newMeshVertices = new List<Vector3[]>();
		List<int[]> newMeshTriangles = new List<int[]>();

		// index count for each bone
		int[] vertexCounter = new int[bonesCount];

		// local offset when adding the collider to the bone
		Vector3[] boneOffset = new Vector3[bonesCount];

		// intilization of each array and values to add to the meshes
		for(int i = 0; i < bonesCount; i++) {
			newMeshVertices.Add(new Vector3[newVertexCount[i]]);
			vertexCounter[i] = 0;
			newMeshTriangles.Add(new int[newTriangles[i].Count]);
			boneOffset[i] = transform.InverseTransformPoint(renderer.bones[i].transform.position);
		}

		Debug.Log("bone count: " + bonesCount);
		// create the translation array and apply the offsets
		for(int bone = 0; bone < bonesCount; bone++)
		{
			int v = 0;
			for(int i = 0; i < oldMesh.vertexCount; i++)
			{
				if(vertexToBone[bone,i])
				{
					if(v==0) Debug.Log("Initalized bone: " + bone);
					else if(v >= newMeshVertices[bone].Length -1) Debug.Log("Completed Bone: " + bone);

					newMeshVertices[bone][v] = oldMesh.vertices[i] - boneOffset[bone];
					translation[bone, i] = v;
					v++;
				}
				
			}
		}
		
		// if they are vertices in one bone, then add the collider
		for(int bone = 0; bone < bonesCount; bone++)
		{
			if(newVertexCount[bone] < 1) continue;

			Mesh newMesh = new Mesh();
			for(int i = 0; i < newTriangles[bone].Count; i++)
			{
				int originalVertexIndex = newTriangles[bone][i];
				int newValue = translation[bone, originalVertexIndex];
				if(newValue > newMeshVertices[bone].Length) Debug.Log("Error in bone: "+ bone + " new value: " + newValue + " is bigger than the vertex count: "+ newMeshVertices[bone].Length);
				newMeshTriangles[bone][i] = newValue;
			}
			newMesh.vertices = newMeshVertices[bone];
			// some triangles are not fine, check the bounds
			newMesh.triangles = newMeshTriangles[bone];
			newMesh.name = "Mesh " + bone;

			MeshCollider collider = renderer.bones[bone].gameObject.AddComponent<MeshCollider>();
			collider.sharedMesh = null;
			collider.sharedMesh = newMesh;
		}
	}
			
}

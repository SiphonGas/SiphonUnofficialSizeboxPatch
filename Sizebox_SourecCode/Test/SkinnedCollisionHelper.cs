using System;
using System.Collections;
using UnityEngine;

public class SkinnedCollisionHelper : MonoBehaviour
{
	private class CVertexWeight
	{
		public int index;

		public Vector3 localPosition;

		public Vector3 localNormal;

		public float weight;

		public CVertexWeight(int i, Vector3 p, Vector3 n, float w)
		{
			this.index = i;
			this.localPosition = p;
			this.localNormal = n;
			this.weight = w;
		}
	}

	private class CWeightList
	{
		public Transform transform;

		public ArrayList weights;

		public CWeightList()
		{
			this.weights = new ArrayList();
		}
	}

	public bool forceUpdate;

	public bool updateOncePerFrame = true;

	public bool calcNormal = true;

	private bool IsInit;

	private SkinnedCollisionHelper.CWeightList[] nodeWeights;

	private SkinnedMeshRenderer skinnedMeshRenderer;

	private MeshCollider meshCollider;

	private Mesh meshCalc;

	private void Start()
	{
		this.Init();
	}

	public bool Init()
	{
		if (this.IsInit)
		{
			return true;
		}
		this.skinnedMeshRenderer = base.GetComponent<SkinnedMeshRenderer>();
		this.meshCollider = base.GetComponent<MeshCollider>();
		if (this.meshCollider != null && this.skinnedMeshRenderer != null)
		{
			this.meshCalc = UnityEngine.Object.Instantiate<Mesh>(this.skinnedMeshRenderer.sharedMesh);
			this.meshCalc.name = this.skinnedMeshRenderer.sharedMesh.name + "_calc";
			this.meshCollider.sharedMesh = this.meshCalc;
			this.meshCalc.MarkDynamic();
			Vector3[] vertices = this.skinnedMeshRenderer.sharedMesh.vertices;
			Vector3[] normals = this.skinnedMeshRenderer.sharedMesh.normals;
			Matrix4x4[] bindposes = this.skinnedMeshRenderer.sharedMesh.bindposes;
			BoneWeight[] boneWeights = this.skinnedMeshRenderer.sharedMesh.boneWeights;
			this.nodeWeights = new SkinnedCollisionHelper.CWeightList[this.skinnedMeshRenderer.bones.Length];
			for (int i = 0; i < this.skinnedMeshRenderer.bones.Length; i++)
			{
				this.nodeWeights[i] = new SkinnedCollisionHelper.CWeightList();
				this.nodeWeights[i].transform = this.skinnedMeshRenderer.bones[i];
			}
			for (int j = 0; j < vertices.Length; j++)
			{
				BoneWeight boneWeight = boneWeights[j];
				if (boneWeight.weight0 != 0f)
				{
					Vector3 p = bindposes[boneWeight.boneIndex0].MultiplyPoint3x4(vertices[j]);
					Vector3 n = bindposes[boneWeight.boneIndex0].MultiplyPoint3x4(normals[j]);
					this.nodeWeights[boneWeight.boneIndex0].weights.Add(new SkinnedCollisionHelper.CVertexWeight(j, p, n, boneWeight.weight0));
				}
				if (boneWeight.weight1 != 0f)
				{
					Vector3 p2 = bindposes[boneWeight.boneIndex1].MultiplyPoint3x4(vertices[j]);
					Vector3 n2 = bindposes[boneWeight.boneIndex1].MultiplyPoint3x4(normals[j]);
					this.nodeWeights[boneWeight.boneIndex1].weights.Add(new SkinnedCollisionHelper.CVertexWeight(j, p2, n2, boneWeight.weight1));
				}
				if (boneWeight.weight2 != 0f)
				{
					Vector3 p3 = bindposes[boneWeight.boneIndex2].MultiplyPoint3x4(vertices[j]);
					Vector3 n3 = bindposes[boneWeight.boneIndex2].MultiplyPoint3x4(normals[j]);
					this.nodeWeights[boneWeight.boneIndex2].weights.Add(new SkinnedCollisionHelper.CVertexWeight(j, p3, n3, boneWeight.weight2));
				}
				if (boneWeight.weight3 != 0f)
				{
					Vector3 p4 = bindposes[boneWeight.boneIndex3].MultiplyPoint3x4(vertices[j]);
					Vector3 n4 = bindposes[boneWeight.boneIndex3].MultiplyPoint3x4(normals[j]);
					this.nodeWeights[boneWeight.boneIndex3].weights.Add(new SkinnedCollisionHelper.CVertexWeight(j, p4, n4, boneWeight.weight3));
				}
			}
			this.UpdateCollisionMesh(false);
			this.IsInit = true;
			return true;
		}
		return false;
	}

	public bool Release()
	{
		UnityEngine.Object.Destroy(this.meshCalc);
		return true;
	}

	public void UpdateCollisionMesh(bool _bRelease = true)
	{
		Vector3[] vertices = this.meshCalc.vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = Vector3.zero;
		}
		SkinnedCollisionHelper.CWeightList[] array = this.nodeWeights;
		for (int j = 0; j < array.Length; j++)
		{
			SkinnedCollisionHelper.CWeightList cWeightList = array[j];
			Matrix4x4 localToWorldMatrix = cWeightList.transform.localToWorldMatrix;
			foreach (SkinnedCollisionHelper.CVertexWeight cVertexWeight in cWeightList.weights)
			{
				vertices[cVertexWeight.index] += localToWorldMatrix.MultiplyPoint3x4(cVertexWeight.localPosition) * cVertexWeight.weight;
			}
		}
		for (int k = 0; k < vertices.Length; k++)
		{
			vertices[k] = base.transform.InverseTransformPoint(vertices[k]);
		}
		this.meshCalc.vertices = vertices;
		this.meshCollider.enabled = false;
		this.meshCollider.enabled = true;
	}

	private void Update()
	{
	}

	private void LateUpdate()
	{
		if (!this.IsInit)
		{
			return;
		}
		if (this.forceUpdate)
		{
			if (this.updateOncePerFrame)
			{
				this.forceUpdate = false;
			}
			this.UpdateCollisionMesh(true);
		}
	}
}

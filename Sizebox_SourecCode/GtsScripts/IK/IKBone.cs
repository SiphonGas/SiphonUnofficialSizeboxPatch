using RootMotion.FinalIK;
using UnityEngine;

public class IKBone {
	IKEffector effector;
	public Vector3 position;
	public Quaternion rotation;
	public float positionWeight;
	public float rotationWeight;

	// Use this for initialization
	public IKBone(IKEffector effector) {
		this.effector = effector;
	}
	
	// Update is called once per frame
	public void Update () {
		effector.position = position;
		effector.rotation = rotation;
		effector.positionWeight = positionWeight;
		effector.rotationWeight = rotationWeight;
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;

public class PlayerIK : MonoBehaviour {

	FullBodyBipedIK ik;
	Player playercontrol;
	bool ikEnabled = true;
	GrounderFBBIK grounder;
	float weight;
	public float weightWhenWalking = 0.3f;
	bool ikSetUp = false;

	// Use this for initialization
	void Start () {
		playercontrol = GetComponent<Player>();

		BipedReferences references = new BipedReferences();
		BipedReferences.AutoDetectReferences(ref references, transform, BipedReferences.AutoDetectParams.Default);
		ik = gameObject.AddComponent<FullBodyBipedIK>();

		if(!references.root) return;
		ik.solver.iterations = 0;
		ik.SetReferences(references, null);
		ik.solver.SetLimbOrientations(BipedLimbOrientations.UMA);
		// Optimization
		ik.solver.leftArmChain.reach = 0f;
		ik.solver.rightArmChain.reach = 0f;
		ik.solver.leftLegChain.reach = 0f;
		ik.solver.rightLegChain.reach = 0f;
		ik.solver.spineMapping.twistWeight = 0f;
		ik.solver.spineStiffness = 0f;
		ik.solver.pullBodyHorizontal = 0f;
		ik.solver.pullBodyVertical = 0f;

		GameObject grounderGo = new GameObject("Grounder");
		grounderGo.transform.SetParent(transform, false);
		grounder = grounderGo.AddComponent<GrounderFBBIK>();
		grounder.ik = ik;
		grounder.solver.layers = Layers.walkableMask;
		grounder.solver.footRotationSpeed = 10f;

		ikSetUp = true;
	}

	void Update() {
		if(!ikSetUp) return;

		float scale = transform.lossyScale.y;
		float nextWeight = 0f;
		bool walking = !playercontrol.IsFlying() && !playercontrol.isClimbing();
		if(ikEnabled && !walking) {
			ikEnabled = false;
		} else if (!ikEnabled && walking) {
			ikEnabled = true;
		}
		if(ikEnabled) {
			if(playercontrol.isMoving) {
				nextWeight = weightWhenWalking;
			} else {
				nextWeight = 1f;
			}
		}
		weight = Mathf.Lerp(weight, nextWeight, Time.deltaTime * 2f);
		grounder.solver.maxStep = 0.5f * scale;
		grounder.solver.footRadius = 0.15f * scale;
		grounder.weight = weight;
	}
}

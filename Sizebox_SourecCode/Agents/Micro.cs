using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;

public class Micro : Humanoid {

	protected bool isInFloor = false;
	protected int gtsLayers = 0;

	protected override void Awake() {
		isMicro = true;
		base.Awake();
	}

	public virtual void Crushed(EntityBase gts)
	{
		// empty but somebody can be crushed
	}

	protected virtual void HandleGiantessContact()
	{
		// to do when entering in contact with a giantess
	}

	protected virtual void OnCollisionEnter(Collision collision)
	{		
		bool crushEnabled = MicroNPC.crushEnabled;
		GameObject go = collision.gameObject;
		int layer = go.layer;
		bool isPlayer = (layer == Layers.playerLayer);
		bool isMicro = (layer == Layers.microLayer);

		if(layer == Layers.mapLayer || layer == Layers.defaultLayer)
		{
			isInFloor = true;
			transform.SetParent(null);
			return;
		} 

		// this adds the ability to walk on moving giantess	
		// check layer
		// get type
		// check size
		// check point of contact
		if(crushEnabled && (isPlayer || isMicro))
		{
			Humanoid agent = go.GetComponent<Humanoid>();
			if(agent && agent.Height > this.Height * 6f) {
				Crushed(agent);
			}
		}

		if(layer == Layers.objectLayer) {
			// find if parent is a giantesst
			Transform t = transform.parent;
			while( t != null ) {
				if(t.gameObject.layer == Layers.gtsBodyLayer && go.GetComponent<GiantessBone>()) {
					transform.SetParent(collision.transform);
					t = null;
				}
				t = t.parent;
			}			
			isInFloor = true;
			return;
		}

		if(layer == Layers.gtsBodyLayer)
		{
			GiantessBone gtsBone = go.GetComponent<GiantessBone>();
			if(gtsBone == null) return;

			gtsLayers++;

			if(gtsBone.isGrabbing && (gtsLayers > 1 || !isInFloor)) {
				transform.SetParent(collision.transform);
				return;
			}

			if(crushEnabled && isInFloor && gtsBone.canCrush && (gtsBone.giantess.Height > this.Height * 10f)) {
				bool isCrushed = false;
				if(isMicro) 
					isCrushed = true;
				else {
					for(int i = 0; i < collision.contacts.Length; i++) {
						Vector3 point = collision.contacts[i].point;
						point = transform.InverseTransformPoint(point);
						if(point.y > 1.4f) {
							isCrushed = true;
							break;
						}
					}
				}
				
				if(isCrushed){
					Crushed(gtsBone.giantess);
				}

				return;
			}


			HandleGiantessContact();
						
			if(!isInFloor && transform.parent != collision.transform) {
				transform.SetParent(collision.transform);
				return;
			} 

			
		}
	}

	protected virtual void OnCollisionExit(Collision collision)
	{
		int layer = collision.gameObject.layer;
		if(layer == Layers.mapLayer)
		{
			isInFloor = false;
			transform.SetParent(null);
			return;
		}

		if(layer == Layers.objectLayer) {
			isInFloor = false;
			return;
		}

		if(layer == Layers.gtsBodyLayer && collision.gameObject.GetComponent<GiantessBone>())
		{
			gtsLayers--;
		}
	}

	protected virtual void CheckGround()
	{
		if(gtsLayers > 0) return;
		if(transform.parent == null) return;
		RaycastHit hit;
		Vector3 origin = transform.position + transform.up * 1f * transform.localScale.y;
		float distance = 20f * transform.localScale.y;
		bool hasHit = Physics.Raycast(origin, -transform.up, out hit, distance);
		Debug.DrawLine(origin, origin - transform.up * distance);
		if(!hasHit)// || hit.collider.gameObject.layer != LayerManager.gtsBodyLayer)
		{
			transform.SetParent(null);
		}
	}

	public static Micro FindMicroComponent(GameObject go) {
		Transform currentLevel = go.transform;
		while(currentLevel != null) {
			Micro ma = currentLevel.gameObject.GetComponent<Micro>();
			if(ma != null) return ma;
			currentLevel = currentLevel.parent;
		}
		return null;
	}

	public override List<Behavior> GetListBehaviors()
	{
		List<Behavior> behaviors = BehaviorLists.GetBehaviors(EntityType.Micro);
		behaviors.AddRange(base.GetListBehaviors());
		return behaviors;
	}

	public override List<EntityType> GetTypesEntity() {
		List<EntityType> types = base.GetTypesEntity();
		types.Add(EntityType.Micro);
		return types;
	}

}
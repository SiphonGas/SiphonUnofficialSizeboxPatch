using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SteeringBehaviors;

namespace NPC 
{
	public class TankControl : MonoBehaviour {
		EntityBase entity;
		Transform target;

		// Use this for initialization
		void Start () {
			entity = GetComponent<EntityBase>();
			entity.AddMovementComponent();
		}
		
		// Update is called once per frame
		void Update () {
			if(target == null && GameController.playerInstance != null) {
				target = GameController.playerInstance.myTransform;
				entity.movement.StartSeekBehavior(new TransformKinematic(target));
			}
		}
	}
}


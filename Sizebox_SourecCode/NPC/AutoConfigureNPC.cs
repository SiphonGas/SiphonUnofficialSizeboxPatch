using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class AutoConfigureNPC : MonoBehaviour {

	public float height = 1.7f;
	public float radius = 0.2f;
	CapsuleCollider colisionador;
	Rigidbody rigid;
	MicroNPC controller;
	Animator anim;
	// Use this for initialization
	void Start () {
		rigid = gameObject.GetComponent<Rigidbody>();
		colisionador = gameObject.GetComponent<CapsuleCollider>();
		controller = gameObject.GetComponent<MicroNPC>();
		anim = gameObject.GetComponent<Animator>();
		if(rigid == null)
		{
			rigid = gameObject.AddComponent<Rigidbody>();
		}
		if(colisionador == null)
		{
			colisionador = gameObject.AddComponent<CapsuleCollider>();
		}
		if(controller == null)
		{
			controller = gameObject.AddComponent<MicroNPC>();
		}
		gameObject.layer = LayerMask.NameToLayer("NPC");
		
	}
	
	// Update is called once per frame
	void Update () {
		colisionador.height = height;
		colisionador.center = new Vector3(0, height/2, 0);
		colisionador.radius = radius;
		rigid.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
		RuntimeAnimatorController runtimeControl = (RuntimeAnimatorController) Resources.Load("TinyController", typeof(RuntimeAnimatorController));
		anim.runtimeAnimatorController = runtimeControl;
		
		
	}
}

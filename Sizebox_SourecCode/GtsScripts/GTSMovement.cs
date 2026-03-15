using UnityEngine;

public class GTSMovement : MonoBehaviour {
	LayerMask gtsLayer; 

	public Giantess giantess;
	CapsuleCollider capsuleCollider;
	Rigidbody rbody;
	bool terrainColliderActive = true;
	TerrainCollider terrainCollider;
	float terrainScale = 20f;
	GiantessIK ik;
	public bool onlyMoveWithPhysics = false;
	Gravity gravity;


	// collision detector parameters

	public void SetGiantess(Giantess giantess) {
		this.giantess = giantess;
		this.giantess.rbody = rbody;
		rbody.constraints = RigidbodyConstraints.FreezeRotation;
		transform.position = giantess.myTransform.position;
	}
	
	void Awake()
	{
		terrainCollider = GameObject.FindObjectOfType<TerrainCollider>();
		

		rbody = gameObject.AddComponent<Rigidbody>();
		rbody.freezeRotation = true;

		gravity = gameObject.AddComponent<Gravity>();
		gravity.baseScale = 100f;

	}

	// Use this for initialization
	void Start () {
		gtsLayer = Layers.gtsCapsuleLayer;
		

		// remember to update this size when scaling the character
		capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
		capsuleCollider.center = new Vector3(0,800,0);
		capsuleCollider.radius = 200;
		capsuleCollider.height = 1600;

		gameObject.layer = gtsLayer;
		
		

		

	}
	
	// Update is called once per frame
	void Update () {
		if(transform.lossyScale.y != giantess.Scale) {
			transform.localScale = Vector3.one * giantess.Scale;
		}

		UpdateCharacterPosition();
		CollisionChoose();
		
		
	}

	void CollisionChoose() {
		if(terrainCollider && capsuleCollider) {
			float size = transform.lossyScale.y;
			if(terrainColliderActive && size > terrainScale) {
				Physics.IgnoreCollision(capsuleCollider, terrainCollider, true);
				terrainColliderActive = false;
				DisableGrounder(true);
				
				Debug.Log("Collision OFF");
			} else if (!terrainColliderActive && size < terrainScale) {
				Physics.IgnoreCollision(capsuleCollider, terrainCollider, false);
				terrainColliderActive = true;
				DisableGrounder(false);
				Debug.Log("Collision ON");
			}
		}
	}

	void DisableGrounder(bool disable) {
		if(ik == null) {
			ik = giantess.GetComponent<GiantessIK>();
		}
		if(ik != null && ik.grounder != null) {
			ik.grounder.gameObject.SetActive(!disable);
		}

	}


	void UpdateCharacterPosition()
	{
		
		float deltaTime = Time.deltaTime;
		Transform myTransform = transform;
		if(onlyMoveWithPhysics || giantess.movement.move) {
			MoveTransformToCapsule();
			giantess.myTransform.rotation = Quaternion.Slerp(giantess.myTransform.rotation, myTransform.rotation, 10 * deltaTime);
		} else {
			MoveCapsuleToTransform();
			myTransform.rotation = giantess.myTransform.rotation;
		}		
	}



	void MoveCapsuleToTransform() {
		Vector3 newPosition = giantess.myTransform.position;
		rbody.position = newPosition;
		// giantess.virtualPosition = CenterOrigin.WorldToVirtual(newPosition);
	}

	void MoveTransformToCapsule() {
		float deltaTime = Time.deltaTime;
		Transform myTransform = transform;
		Vector3 position = myTransform.position;

		Vector3 giantessMeshPosition = CenterOrigin.VirtualToWorld(giantess.virtualPosition);

		Vector3 newPosition = Vector3.Lerp(giantessMeshPosition, position, deltaTime * 10f);
		giantess.Move(newPosition);
	}

	public void EnableCollider(bool enable) {
		capsuleCollider.enabled = enable;
		gravity.useGravity = enable;	
	}


}


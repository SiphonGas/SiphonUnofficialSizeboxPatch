using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using AI;

public class EntityBase : NetworkBehaviour {
	[SyncVarAttribute(hook="OnScaleChanged")]
	float networkScale = 1;

	[SyncVarAttribute(hook="OnVirtualPositionChanged")]
	public Vector3 virtualPosition;
	
	public Vector3 realPosition {
		get { return CenterOrigin.VirtualToWorld(virtualPosition); }
	}
	
	public int id = -1;
	public float offset = 0;
	public float baseScale = 1;
	public float Height {get {return myTransform.lossyScale.y * baseScale; }}
	public float AccurateScale {get { float scale = myTransform.lossyScale.y; if(isGiantess) scale *= 1000; return scale;}}
	public float Scale { get {return myTransform.lossyScale.y; }}

	
	Vector3 normal = Vector3.up;
	public bool isPositioned = true;
	public bool isGiantess = false;
	public bool isHumanoid = false;
	public bool isMicro = false;
	public bool isPlayer = false;
	public float maxSize = 1000f;
	public float minSize = 0.001f;
	public bool rotationEnabled = true;

	public GiantessIK ik;
	public bool isDead = false;

	// ==== Unity Components ========== //
	public Rigidbody rbody;
	bool visible = true;
	Renderer[] meshRenderers;

	// ==== Ai Components ====== //
	Lua.Entity _luaEntity;
	public AnimationManager animationManager;
	public AIController ai;
	public ActionManager actionManager;
	public MovementCharacter movement {protected set; get;}
	public SenseController senses;

	public Transform myTransform;

	Collider[] colliderList;

	public bool locked = false;
	[SerializeField]
	bool hasNetworkTransform;

	public override void OnStartAuthority() {
		EditPlacement.Instance.OnObjectSpawned(this);
		ObjectManager.Instance.OnObjectSpawned(this);
	}

	void OnScaleChanged(float newScale) {
		networkScale = newScale;
		if(!hasAuthority) {
			_ChangeScale(newScale);
		}
	}

	void OnVirtualPositionChanged(Vector3 newPos) {
		if(!hasAuthority) {
			virtualPosition = newPos;
		}		
	}

	protected virtual void Awake() {
		myTransform = transform;
		UpdateRealPosition(myTransform.position);
		hasNetworkTransform = (GetComponent<NetworkTransform>() != null);
	}

	public override void OnStartClient() {
		_ChangeScale(networkScale);
	}

	public Lua.Entity GetLuaEntity() {
		if(_luaEntity == null) _luaEntity = new Lua.Entity(this);
		return _luaEntity;
	}

	void UpdateRealPosition(Vector3 position) {
		virtualPosition = CenterOrigin.WorldToVirtual(position);
	}

	public void AddMovementComponent() {
		movement = gameObject.AddComponent<MovementCharacter>();
	}


	
	public void SetActive(bool active) {
		gameObject.SetActive(active);
	}



	public virtual void Lock() {
		if(locked) return;
		locked = true;
		
		if(actionManager != null) {
			actionManager.ClearAll();
		}

		Rigidbody rb = GetComponent<Rigidbody>();
		if(rb == null) return;
		rb.isKinematic = true;

		Gravity g = GetComponent<Gravity>();
		if(g == null) return;
		g.useGravity = false;
	}
	
	public virtual void Move(Vector3 world_pos)
	{
		if(hasAuthority) {
			Vector3 virtual_pos = CenterOrigin.WorldToVirtual(world_pos);
			_Move(virtual_pos);
			CmdMove(virtual_pos);
		}
	}

	[CommandAttribute]
	void CmdMove(Vector3 newPosition) {
		if(hasNetworkTransform) {
			virtualPosition = newPosition;
		} else {
			RpcMove(newPosition);
		}
	}

	[ClientRpcAttribute]
	void RpcMove(Vector3 virtual_pos) {
		if(!hasAuthority) {
			_Move(virtual_pos);
		}		
	}

	void _Move(Vector3 virtual_pos) {
		virtualPosition = virtual_pos;
		Vector3 world_pos = CenterOrigin.VirtualToWorld(virtual_pos);

		myTransform.position = world_pos;
		myTransform.Translate(Vector3.up * offset * Height);
	}

	public virtual void ChangeScale(float newScale)
	{
		_ChangeScale(newScale);
		if(hasAuthority || isServer) {
			CmdChangeScale(newScale);
		}
		
	}

	[CommandAttribute]
	public void CmdChangeScale(float newScale) {
		networkScale = newScale;
	}

	[ClientRpcAttribute]
	void RpcChangeScale(float newScale) {
		_ChangeScale(newScale);		
	}

	public void _ChangeScale(float newScale) {
		if(virtualPosition == Vector3.zero || gameObject.layer == Layers.playerLayer || gameObject.layer == Layers.microLayer) {
			UpdateRealPosition(myTransform.position);
		}

		if(newScale > maxSize) newScale = maxSize;
		if(newScale < minSize) newScale = minSize;

		Transform previousParent = myTransform.parent;

		myTransform.SetParent(null);
		myTransform.localScale = Vector3.one * newScale;
		myTransform.SetParent(previousParent);

        if(isGiantess)
        {
            Move(CenterOrigin.VirtualToWorld(virtualPosition));
        }
	}

	public virtual void ChangeRotation(Vector3 newRotation) {
		if(rotationEnabled) {
			if(hasAuthority) {
				_ChangeRotation(newRotation);
				CmdChangeRotation(newRotation);
			}
		}		
	}

	[CommandAttribute]
	void CmdChangeRotation(Vector3 newRotation) {
		if(!hasNetworkTransform) {
			RpcChangeRotation(newRotation);
		}
	}

	[ClientRpcAttribute]
	void RpcChangeRotation(Vector3 newRotation) {
		if(!hasAuthority) {
			_ChangeRotation(newRotation);
		}
		
	}

	void _ChangeRotation(Vector3 newRotation) {
		myTransform.Rotate(newRotation);
	}

	public virtual void ChangeOffset(float newOffset)
	{
		if(isServer) RpcChangeOffset(newOffset);
	}

	[ClientRpcAttribute]
	void RpcChangeOffset(float newOffset) {
		float diffOffset = newOffset - offset;
		myTransform.position += normal * diffOffset * Height;
		// transform.Translate(Vector3.up * diffOffset * RealScale);
		offset = newOffset;
	}

	
	public virtual void SetCollider(bool value)
	{
		if (colliderList == null)
		{
			FindColliders();
		}
		for (int i = 0; i < colliderList.Length; i++)
		{
			colliderList[i].enabled = value;
		}

	}


	// private functions 
	protected virtual void FindColliders()
	{
		colliderList = GetComponentsInChildren<Collider>();
		foreach(Collider collider in colliderList) {
			float sizeY = collider.bounds.size.y;
			if(sizeY > baseScale) {
				baseScale = sizeY;
			}
		}
	}

	public virtual List<EntityType> GetTypesEntity() {
		List<EntityType> types = new List<EntityType>();
		types.Add(EntityType.Entity);
		return types;
	}

	public virtual List<Behavior> GetListBehaviors()
	{
		return BehaviorLists.GetBehaviors(EntityType.Entity);
	}

	public virtual void DestroyObject(bool recursive = true)
	{
		if(hasAuthority || isServer) {
			CmdUnspawn(recursive);
		}
	}

	public void _DestroyObject(bool recursive) {
		if(recursive) {
			EntityBase[] children = GetComponentsInChildren<EntityBase>(true);
			for(int i = 0; i < children.Length; i++) {
				children[i].myTransform.SetParent(null);
			}
			for(int i = 0; i < children.Length; i++) {
				children[i].DestroyObject(false);
			}
		}
		
		Destroy(gameObject);
	}

	[CommandAttribute]
	void CmdUnspawn(bool recursive) {
		NetworkServer.UnSpawn(gameObject);
		_DestroyObject(recursive);

	}

	public void Render(bool enabled) {
		if(visible == enabled) return;		
		visible = enabled;
		if(meshRenderers == null) meshRenderers = GetRenderers();
		for(int i = 0; i < meshRenderers.Length; i++) {
			meshRenderers[i].enabled = enabled;
		}
	}

	public bool IsVisible() {
		return visible;
	}

	protected Renderer[] GetRenderers() {
		return GetComponentsInChildren<Renderer>(); 
	}

	public void Place() {
		if(isServer) {
			RpcPlace();
		}
	}

	[ClientRpcAttribute]
	public void RpcPlace() {
		OnPlaced();
	}

	public virtual void OnPlaced() {
		
	}

	public virtual Vector3 GetEyesPosition() {
		return Vector3.zero;
	}



}
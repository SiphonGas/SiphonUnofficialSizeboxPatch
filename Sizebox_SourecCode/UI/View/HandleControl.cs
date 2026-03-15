using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleControl : MonoBehaviour {
	public EntityBase smartObject;
	enum State { NotDrag, Drag}
	State state;
	Camera mainCamera;
	Collider xAxis;
	Collider yAxis;
	Collider zAxis;
	Collider planeX;
	Collider planeY;
	Collider planeZ;
	Vector3 axis;
	Vector3 initialPoint;
	Vector3 initialTransform;



	// Use this for initialization
	void Start () {
		state = State.NotDrag;
		mainCamera = Camera.main;
		Collider[] collider = GetComponentsInChildren<Collider>();
		xAxis = collider[0];
		yAxis = collider[1];
		zAxis = collider[2];
		planeX = collider[3];
		planeY = collider[4];
		planeZ = collider[5];
		DisablePlanes();
	}

	void DisablePlanes() {
		planeX.enabled = false;
		planeY.enabled = false;
		planeZ.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		if(smartObject) {
			
			State nextState = state;
			Vector3 mousePositionInScreen = Input.mousePosition;
			RaycastHit hit;
			Ray ray;
			switch(state) {
				case State.NotDrag:
					transform.position = smartObject.transform.position;
					transform.localScale = Vector3.one * smartObject.Height;
					bool hitUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
					if(!hitUI && Input.GetMouseButtonDown(0)) {
						ray = mainCamera.ScreenPointToRay(mousePositionInScreen);
						if(Physics.Raycast(ray, out hit, Mathf.Infinity, Layers.actionSelectionMask)) {
							if(hit.collider.gameObject.layer != Layers.uiLayer) return;
							if(hit.collider == xAxis) {
								axis = Vector3.right;
							}
							else if (hit.collider == yAxis) {
								axis = Vector3.up;
							}
							else if (hit.collider == zAxis) {
								axis = Vector3.forward;
							}

							planeX.enabled = axis.x == 0f;
							planeY.enabled = axis.y == 0f;
							planeZ.enabled = axis.z == 0f;

							initialTransform = transform.position;
							initialPoint = transform.position + Vector3.Scale(hit.point - initialTransform, axis);

							/*if(Physics.Raycast(ray, out hit, Mathf.Infinity, LayerManager.auxMask)) {
								initialPoint = transform.position + Vector3.Scale(hit.point - initialTransform, axis);
							} */
							nextState = State.Drag;	
							
											
						}
					}
					break;

				case State.Drag: 
					ray = mainCamera.ScreenPointToRay(mousePositionInScreen);
					if(Physics.Raycast(ray, out hit, Mathf.Infinity, Layers.auxMask)) {
						Vector3 movement = Vector3.Scale(hit.point - initialPoint, axis);
						transform.position = initialTransform + movement;
						smartObject.Move(transform.position);
					} 
					if(Input.GetMouseButtonUp(0)) {
						DisablePlanes();
						nextState = State.NotDrag;
					}
						
					break;
			}
			state = nextState;
			
		}
		
	}
}

using UnityEngine;

public class Layers : MonoBehaviour {
	public static LayerMask defaultLayer;
	public static LayerMask mapLayer;
	public static LayerMask gtsBodyLayer;
	public static LayerMask playerLayer;
	public static LayerMask microLayer;
	public static LayerMask objectLayer;
	public static LayerMask buildingLayer;
	public static LayerMask uiLayer;
	public static LayerMask gtsCapsuleLayer;
	public static LayerMask auxLayer;
	public static LayerMask destroyerLayer;
	public static LayerMask vehicelsLayer;
	public static LayerMask ignoreWheelcastLayer;
	public static LayerMask detachableLayer;

	public static LayerMask placementMask;
	public static LayerMask cameraCollisionMask;
	public static LayerMask vehicleCameraCollisionMask;
	public static LayerMask mapMask;
	public static LayerMask buildingMask;
	public static LayerMask gtsCollisionCheckMask;
	public static LayerMask walkableMask;
	public static LayerMask crushableMask;
	public static LayerMask gtsWalkableMask;
	public static LayerMask auxMask;
	public static LayerMask actionSelectionMask;
	public static LayerMask visibilityMask;
	public static LayerMask pathfindingMask;

	static bool initialized = false;
	// Use this for initialization


	public static void Initialize() {
		if(initialized) return;

		string defaultL = "Default";
		string map = "Map";
		string gtsBody = "GTSBody";
		string player = "Player";
		string micro = "Micro";
		string obj = "Object";
		string building = "Building";
		string ui = "UI";
		string aux = "Aux";
		string gtsCapsule = "GTSCapsule";
		string destroyer = "Destroyer";
		string vehicles = "Vehicles";
		string ignoreWheelcast = "Ignore Wheel Cast";
		string detachable = "Detachable Part";

		defaultLayer = LayerMask.NameToLayer(defaultL);
		mapLayer = LayerMask.NameToLayer(map);
		gtsBodyLayer = LayerMask.NameToLayer(gtsBody);
		playerLayer = LayerMask.NameToLayer(player);
		microLayer = LayerMask.NameToLayer(micro);
		objectLayer = LayerMask.NameToLayer(obj);
		buildingLayer = LayerMask.NameToLayer(building);
		uiLayer = LayerMask.NameToLayer(ui);
		gtsCapsuleLayer = LayerMask.NameToLayer(gtsCapsule);
		auxLayer = LayerMask.NameToLayer(aux);
		destroyerLayer = LayerMask.NameToLayer(destroyer);
		vehicelsLayer = LayerMask.NameToLayer(vehicles);
		ignoreWheelcastLayer = LayerMask.NameToLayer(ignoreWheelcast);
		detachableLayer = LayerMask.NameToLayer(detachable);



		placementMask = LayerMask.GetMask(new string[] {defaultL, map, gtsBody, obj});
		cameraCollisionMask = LayerMask.GetMask(new string[] {defaultL, map, gtsBody, obj, building});
		vehicleCameraCollisionMask = LayerMask.GetMask(new string[] {defaultL, map, gtsBody, building, obj});
		mapMask = LayerMask.GetMask(new string[] {map});
		buildingMask = LayerMask.GetMask(new string[] {building});
		gtsCollisionCheckMask = LayerMask.GetMask(new string[] {map, obj, gtsBody, defaultL});
		walkableMask = LayerMask.GetMask(new string[] { defaultL, map, gtsBody, obj, building});
		gtsWalkableMask = LayerMask.GetMask(new string[] { map, obj});
		crushableMask = LayerMask.GetMask(new string[] { player, micro});
		auxMask = LayerMask.GetMask(new string[] { aux });
		actionSelectionMask = LayerMask.GetMask(new string[] { defaultL, map, gtsBody, player, micro, obj, building, ui });
		visibilityMask = LayerMask.GetMask(new string[] { map, obj, micro, player, gtsCapsule, building });
		pathfindingMask = LayerMask.GetMask(new string[] { map, obj, building});

		initialized = true;
		
	}
}

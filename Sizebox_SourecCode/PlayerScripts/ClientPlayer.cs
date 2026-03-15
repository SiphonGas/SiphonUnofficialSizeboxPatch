using UnityEngine;
using UnityEngine.Networking;

public class ClientPlayer : NetworkBehaviour {
	public static ClientPlayer Instance;

	public override void OnStartLocalPlayer() {
		Instance = this;
		GameController.Instance.RegisterPlayer(this);
		NetworkManagerHUD hud = GameObject.FindObjectOfType<NetworkManagerHUD>();
		hud.showGUI = false;
	}

	public void Update() {
		if(isLocalPlayer && GameController.playerInstance) {
			transform.position = GameController.playerInstance.transform.position;
		}
	}


	[CommandAttribute]
	public void CmdSpawnPlayer(string name)
	{	
		GameObject skin = ObjectManager.LoadPlayer(name);
		NetworkHash128 modelHash = ObjectManager.GetHash(skin.name);
		
		GameObject go = (GameObject) Instantiate(skin, ObjectManager.Instance.spawnPoint, transform.rotation);
		NetworkServer.SpawnWithClientAuthority(go, modelHash, connectionToClient);		
	}

	[CommandAttribute]
	public void CmdSpawnPlayableGiantess(string name, Vector3 position, float scale)
	{	
		NetworkHash128 modelHash = (ObjectManager.GetHash(name + "_"));
		GameObject gts = ObjectManager.Instance.InstantiatePlayableGiantess(name, position, scale);
		NetworkServer.SpawnWithClientAuthority(gts, modelHash, connectionToClient);		
	}

	[CommandAttribute]
	public void CmdSpawnGiantess(string name, Vector3 position, Quaternion rotation, float scale) 
	{	
		NetworkHash128 modelHash = (ObjectManager.GetHash(name));
		GameObject gts = ObjectManager.Instance.InstantiateGiantess(name, position, rotation, scale);
		NetworkServer.SpawnWithClientAuthority(gts, modelHash, connectionToClient);		
	}

	[CommandAttribute]
	public void CmdSpawnObject(string name, Vector3 position, Quaternion rotation, float scale)
	{	
		NetworkHash128 modelHash = (ObjectManager.GetHash(name));
		GameObject obj = ObjectManager.Instance.InstantiateObject(name, position, rotation, scale);
		NetworkServer.SpawnWithClientAuthority(obj, modelHash, connectionToClient);		
	}

	[CommandAttribute]
	public void CmdSpawnMicro(bool female, Vector3 position, Quaternion rotation, float scale) 
	{
		MicroNPC micro = null;
		NetworkHash128 modelHash;
		if(female) {
			micro = NPCSpawner.Instance.SpawnFemaleNPC();
			modelHash = ObjectManager.femaleMicroAssetId;
		}
		else {
			micro = NPCSpawner.Instance.SpawnMaleNPC();
			modelHash = ObjectManager.maleMicroAssetId; 
		}
		GameObject obj = ObjectManager.Instance.InstantiateMicro(micro, position, rotation, scale);
		NetworkServer.Spawn(obj, modelHash);
	}

	


}

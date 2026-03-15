using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ServerPlayerControl : MonoBehaviour {

	// Use this for initialization
	public GameObject playerPrefab;
	IOManager modelmanager;

	void Start() {
	    NetworkServer.RegisterHandler(MsgType.AddPlayer, OnAddPlayerMessage);
		modelmanager = IOManager.GetIOManager();
	}

	void OnAddPlayerMessage(NetworkMessage netMsg)
	{

	    GameObject thePlayer = (GameObject) Instantiate(modelmanager.LoadRandomPlayerModel(), Vector3.zero, Quaternion.identity);

	    // This spawns the new player on all clients
	    NetworkServer.AddPlayerForConnection(netMsg.conn, thePlayer, 0);
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour {
	public static NPCSpawner Instance;
	List<MicroNPC> NPCListMale;
	List<MicroNPC> NPCListFemale;
	public GameObject blood;
	PlayerCamera cam;
	ObjectManager agentController;

	// Use this for initialization
	void Start () {
		Instance = this;
		cam = GetComponent<PlayerCamera>();
		agentController = GetComponent<ObjectManager>();
		NPCListMale = agentController.maleMicroModels;
		NPCListFemale = agentController.femaleMicroModels;
	}

	public void SpawnMicro(bool female) {
		Vector3 spawnPosition = cam.target.transform.position + (cam.target.transform.forward + Vector3.up) * cam.targetScale * 2;
		ClientPlayer.Instance.CmdSpawnMicro(female, spawnPosition, Quaternion.identity, cam.targetScale);
	}

	public MicroNPC SpawnMaleNPC()
	{
		if (NPCListMale.Count > 0) {
			int number = Random.Range(0,NPCListMale.Count);
			return NPCListMale[number];
		}
		Debug.LogError("No Male micro found");
		return null;
	}

	public MicroNPC SpawnFemaleNPC()
	{
		if (NPCListFemale.Count > 0) {
			int number = Random.Range(0,NPCListFemale.Count);
			return NPCListFemale[number];
		}
		Debug.LogError("No female micro found");
		return null;
	}


}

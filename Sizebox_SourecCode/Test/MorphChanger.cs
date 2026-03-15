using UnityEngine;
using System.Collections;

public class MorphChanger : MonoBehaviour {
	MMD4MecanimModel model;
	MMD4MecanimModel.Morph[]  morphs;

	// Use this for initialization
	void Start () {
		model = GetComponent<MMD4MecanimModel>();
		morphs = model.morphList;
		for(int i = 0; i < morphs.Length; i++)
		{
			Debug.Log(morphs[i].morphData.nameEn + ", " + morphs[i].morphCategory);
			morphs[i].weight2 = 1f;
			Debug.Log("weigth 1: " + morphs[i].weight + ", weigth 2: " + morphs[i].weight2);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

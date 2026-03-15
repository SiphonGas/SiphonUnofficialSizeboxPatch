using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSettings : MonoBehaviour {
	public float maxGtsSize = 1000f;
	public float gtsStartingScale = 1f;
	public float maxPlayerSize = 1000f;
	public float minPlayerSize = 0.1f;
	public float scale = 10000f;
	public float startingSize = 1f;
	public bool macro = false;

	// Use this for initialization
	void Awake () {
		Giantess.maxScale = maxGtsSize;
		ResizeCharacter.minSize = minPlayerSize;
		ResizeCharacter.maxSize = maxPlayerSize;
		GameController.startingSize = startingSize;
		GameController.referenceScale = scale;
		GameController.IsMacroMap = macro;		
		InterfaceControl.macroStartingScale = gtsStartingScale;
		
	}
}

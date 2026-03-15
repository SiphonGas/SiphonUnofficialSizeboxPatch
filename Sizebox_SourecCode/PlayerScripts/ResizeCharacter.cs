using UnityEngine;
using UnityEngine.Networking;

public class ResizeCharacter : NetworkBehaviour {
    // Fix sync in multiplayer
    EntityBase entity;
    public float scaleModifier = 1f;
    
    public static float maxSize = 1000f;
    public static float minSize = 0.1f;

    
    bool sizeUp;
    bool sizeDown;
    
	public float sizeChangeRate = 0.7f;	

    string sizeUpButton = "SizeUp";
    string sizeDownButton = "SizeDown";

    void GetInput() {
        if(!GameController.inputEnabled) return;
        sizeUp = Input.GetButton(sizeUpButton);
        sizeDown = Input.GetButton(sizeDownButton);
    }

    void Awake() {
        entity = GetComponent<EntityBase>();

        if(entity.isGiantess) {
            scaleModifier = 1000f;
        } else {
            scaleModifier = 1f;
        }
    }

	
	// Update is called once per frame
	void Update () {
        if(hasAuthority) {
            GetInput();
        
            if (sizeUp) ChangeScale(entity.Scale * (1 + sizeChangeRate * Time.deltaTime));
            else if (sizeDown) ChangeScale(entity.Scale * (1 - sizeChangeRate * Time.deltaTime));
        }
    }

    public void ChangeScale(float scale)
    {
        if (scale < minSize / scaleModifier) scale = minSize / scaleModifier;
        else if (scale > maxSize / scaleModifier) scale = maxSize / scaleModifier;
        entity.ChangeScale(scale);
    }

}

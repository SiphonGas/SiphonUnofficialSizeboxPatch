using UnityEngine;
using System.Collections.Generic;


public class InterfaceControl : MonoBehaviour {
	public bool commandEnabled = true;
	public static float macroStartingScale = 1f;
	public static float microStartingScale = 1f;
	public EntityBase selectedEntity {get; private set;}
	public Humanoid humanoid {get; private set;}
	public Giantess giantess { get; private set;}

	// catalogs
	public List<Sprite[]> catalog {get; private set;}
	public string[] animations {get; private set;}

	// rotation values
	float lastRotationX;
	float lastRotationY;
	float lastRotationZ;
	public bool lockRotation {get; private set;}

	// scale values
	public float lastMicroScale {get; private set;}
	public float lastMacroScale {get; private set;}

	public IOManager modelManager {get; private set;}

	public delegate void Selected();
	public event Selected OnSelected;
	float giantessOffsetDivisor = 3f;

	// Use this for initialization
	void Awake () {
		modelManager = IOManager.GetIOManager();

		lastMacroScale = macroStartingScale;
		lastMicroScale = microStartingScale;
		lockRotation = false;

		SetSelectedObject(null);

		catalog = new List<Sprite[]>();

		catalog.Add(modelManager.GetObjectsThumbnails());
		catalog.Add(modelManager.GetGtsThumbnails());
		catalog.Add(Resources.LoadAll<Sprite>("PosesThumb"));

		animations = modelManager.GetAnimationList();

	}


	
	public void SetSelectedObject(EntityBase obj) {
		selectedEntity = obj;
		if(selectedEntity == null) {
			humanoid = null;
			giantess = null;
		} else {
			humanoid = selectedEntity.GetComponent<Humanoid>();
			giantess = selectedEntity.GetComponent<Giantess>();
		}
			
		if(OnSelected != null)
			OnSelected();
	}

	// Rotation Options

	public float GetYRotation()
	{
		if(selectedEntity)
			lastRotationY = 0f;
		return 0f;
	}

	public float GetXRotation()
	{
		if(selectedEntity)
			lastRotationX = 0f;
		return 0f;
	}

	public float GetZRotation()
	{
		if(selectedEntity)
			lastRotationZ = 0f;
		return 0f;
	}

	public void RotateYAxis(float angle)
	{
		if(selectedEntity)
		{
			selectedEntity.ChangeRotation(new Vector3(0, lastRotationY - angle, 0));
			lastRotationY = angle;
		}
	}

	public void RotateXAxis(float angle)
	{
		if(selectedEntity)
		{
			selectedEntity.ChangeRotation(new Vector3(lastRotationX - angle, 0, 0));
			lastRotationX = angle;
		}
	}

	public void RotateZAxis(float angle)
	{
		if(selectedEntity)
		{
			selectedEntity.ChangeRotation(new Vector3(0, 0, lastRotationZ - angle));
			lastRotationZ = angle;
		}
	}

	// Scale Options

	public void SetScale(float scale)
	{
		if(selectedEntity)
		{
			scale = scale / 100;
			float newScale = Mathf.Pow(10,scale);
			selectedEntity.ChangeScale(newScale);
			if(selectedEntity.isGiantess)
				lastMacroScale = selectedEntity.transform.lossyScale.y;
			else 
				lastMicroScale = selectedEntity.transform.lossyScale.y;

		}
	}

	public float GetScale()
	{
		if(selectedEntity) 
			return Mathf.Log10(selectedEntity.transform.lossyScale.y) * 100;
		return 1f;
	}

	// Position Options
	public float GetYAxisOffset() {
		float offset = 0f;
		if(selectedEntity != null) {
			offset = selectedEntity.offset * 300f;
			if(giantess) offset *= giantessOffsetDivisor;
		} else {
			Debug.Log("object null");
		}	
		return offset;
	}

	public void SetYAxisOffset(float offset)
	{
		if(selectedEntity) {
			offset = offset / 300f;
			if(giantess) offset /= giantessOffsetDivisor;
			selectedEntity.ChangeOffset(offset);
		}
	}

	public Giantess.MorphData[] GetMorphList()
	{
		if(giantess == null) return null; 
		return giantess.morphs;
	}

	public void SetMorph(int i, float weight)
	{
		if(giantess == null) return;
		giantess.SetMorphValue(i, weight);
	}

	public void SetAnimation(string animationName)
	{
		if(humanoid == null ) return;
		AgentAction action = new AnimationAction(animationName, false);
		humanoid.ai.DisableAI();
		humanoid.actionManager.ClearAll();
		humanoid.actionManager.ScheduleAction(action);
	}

	public void UpdateCollider()
	{
		if(giantess == null ) return;
		giantess.UpdateAllColliders();
	}

	public void ChangeAnimationSpeed(float speed) {
		if(giantess == null ) return;
		giantess.movement.anim.ChangeSpeed(speed);
	}

	public float GetAnimationSpeed() {
		if(giantess == null ) return 0f;
		return giantess.movement.anim.speedMultiplier;
	}

	public void SetPose(string namePose)
	{
		if(humanoid == null ) return;
		humanoid.ai.DisableAI();
		humanoid.actionManager.ClearAll();
		humanoid.animationManager.PlayAnimation(namePose, true);
	}

	public void EnablePoseIK(bool enable) {
		if(giantess == null) return;
		giantess.ik.SetPoseIK(enable);
	}

	public void DeleteObject()
	{
		selectedEntity.DestroyObject();
		SetSelectedObject(null);
	}

	public void LockRotation(bool value) {
		lockRotation = value;
	}

	public void PlayAsGiantess(string name) {
		Player player = GameController.playerInstance;
		GameController.Instance.spawner.CmdSpawnPlayableGiantess(name, player.myTransform.position, player.Scale * 0.001f);
	}
}

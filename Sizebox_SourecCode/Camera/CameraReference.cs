public class CameraReference : EntityBase {

	// Use this for initialization
	public override void DestroyObject(bool recursive = true)
	{
		// this is to avoid the destruction of this camera reference
		return;
	}
}

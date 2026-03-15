using MoonSharp.Interpreter;

namespace Lua	
{
	/// <summary>
	/// Position, rotation and scale of an object.
	/// </summary>
	/// Every object in a scene has a Transform. It's used to store and manipulate the position, rotation and scale of the object. Every Transform can have a parent, which allows you to apply position, rotation and scale hierarchically. 

	[MoonSharpUserDataAttribute]
	public class Transform {

		Transform GetTransform(UnityEngine.Transform unityTransform) {
			if(unityTransform == null) return null;
			return new Transform(unityTransform);
		}
		
		/// <summary>
		/// The number of children the Transform has.
		/// </summary>
		/// <returns></returns>
		public int childCount { get {return _tf.childCount;}}
		/// <summary>
		/// The rotation as Euler angles in degrees.
		/// </summary>
		/// <returns></returns>
		public Vector3 eulerAngles { 
			get {return new Vector3(_tf.eulerAngles);} 
			set { _tf.eulerAngles = value.vector3; }
		}

		/// <summary>
		/// The blue axis of the transform in world space.
		/// </summary>
		/// <returns></returns>
		public Vector3 forward { 
			get {return new Vector3(_tf.forward);} 
			set { _tf.forward = value.vector3; }
		}

		/// <summary>
		/// The rotation as Euler angles in degrees relative to the parent transform's rotation.
		/// </summary>
		/// <returns></returns>
		public Vector3 localEulerAngles { 
			get {return new Vector3(_tf.localEulerAngles);} 
			set { _tf.localEulerAngles = value.vector3; }
		}

		/// <summary>
		/// Position of the transform relative to the parent transform.
		/// </summary>
		/// <returns></returns>
		public Vector3 localPosition { 
			get {return new Vector3(_tf.localPosition);} 
			set { _tf.localPosition = value.vector3; }
		}

		/// <summary>
		/// The rotation of the transform relative to the parent transform's rotation.
		/// </summary>
		/// <returns></returns>
		public Quaternion localRotation { 
			get {return new Quaternion(_tf.localRotation);} 
			set { _tf.localRotation = value.quaternion; }
		}
		/// <summary>
		/// The scale of the transform relative to the parent.
		/// </summary>
		/// <returns></returns>//
		public Vector3 localScale { 
			get {return new Vector3(_tf.localScale);} 
			set { _tf.localScale = value.vector3; }
		}
		/// <summary>
		/// The global scale of the object (Read Only).
		/// </summary>
		/// <returns></returns>
		public Vector3 lossyScale { 
			get {return new Vector3(_tf.lossyScale);} 
		}

		/// <summary>
		/// The name of the object.
		/// </summary>
		/// <returns></returns>
		public string name {
			get {return _tf.name;} 
			set { _tf.name = value; }
		}

		/// <summary>
		/// The parent of the transform.
		/// </summary>
		/// <returns></returns>
		public Transform parent {
			get {return GetTransform(_tf.parent);}
		}

		/// <summary>
		/// The position of the transform in world space.
		/// </summary>
		/// <returns></returns>
		public Vector3 position { 
			get {return new Vector3(_tf.position);} 
			set { _tf.position = value.vector3; }
		}

		/// <summary>
		/// The red axis of the transform in world space.
		/// </summary>
		/// <returns></returns>
		public Vector3 right { 
			get {return new Vector3(_tf.right);} 
			set { _tf.right = value.vector3; }
		}
		/// <summary>
		/// Returns the topmost transform in the hierarchy.
		/// </summary>
		/// <returns></returns>
		public Transform root {
			get { return GetTransform(_tf.root);}
		}
		/// <summary>
		/// The rotation of the transform in world space stored as a Quaternion.
		/// </summary>
		/// <returns></returns>
		public Quaternion rotation { 
			get {return new Quaternion(_tf.rotation);} 
			set { _tf.rotation = value.quaternion; }
		}
		/// <summary>
		/// The green axis of the transform in world space.
		/// </summary>
		/// <returns></returns>
		public Vector3 up { 
			get {return new Vector3(_tf.up);} 
			set { _tf.up = value.vector3; }
		}

		/// <summary>
		/// Unparents all children.
		/// </summary>
		public void DetachChildren() {
			_tf.DetachChildren();
		}
		/// <summary>
		/// Finds a child by name and returns it.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Transform Find(string name) {
			return GetTransform(_tf.Find(name));
		}
		/// <summary>
		/// Returns a transform child by index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Transform GetChild(int index) {
			return GetTransform(_tf.GetChild(index));
		}

		/// <summary>
		/// Gets the sibling index.
		/// </summary>
		/// <returns></returns>
		public int GetSiblingIndex() {
			return _tf.GetSiblingIndex();
		}
		/// <summary>
		/// Transforms a direction from world space to local space. The opposite of Transform.TransformDirection.
		/// </summary>
		/// <param name="direction"></param>
		/// <returns></returns>
		public Vector3 InverseTransformDirection(Vector3 direction) {
			return new Vector3(_tf.InverseTransformDirection(direction.vector3));
		}
		/// <summary>
		/// Transforms a direction from world space to local space. The opposite of Transform.TransformDirection.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public Vector3 InverseTransformDirection(float x, float y, float z) {
			return new Vector3(_tf.InverseTransformDirection(x,y,z));
		}	
		/// <summary>
		/// Transforms position from world space to local space.
		/// </summary>
		/// <param name="direction"></param>
		/// <returns></returns>
		public Vector3 InverseTransformPoint(Vector3 direction) {
			return new Vector3(_tf.InverseTransformPoint(direction.vector3));
		}
		/// <summary>
		/// Transforms position from world space to local space.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public Vector3 InverseTransformPoint(float x, float y, float z) {
			return new Vector3(_tf.InverseTransformPoint(x,y,z));
		}
		/// <summary>
		/// Transforms a vector from world space to local space. The opposite of Transform.TransformVector.
		/// </summary>
		/// <param name="direction"></param>
		/// <returns></returns>
		public Vector3 InverseTransformVector(Vector3 direction) {
			return new Vector3(_tf.InverseTransformVector(direction.vector3));
		}
		/// <summary>
		/// Transforms a vector from world space to local space. The opposite of Transform.TransformVector.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public Vector3 InverseTransformVector(float x, float y, float z) {
			return new Vector3(_tf.InverseTransformVector(x,y,z));
		}
		/// <summary>
		/// Is this transform a child of parent?
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		public bool IsChildOf(Transform parent) {
			return _tf.IsChildOf(parent._tf);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		public void LookAt(Transform target) {
			_tf.LookAt(target._tf);
		}
		/// <summary>
		/// Rotates the transform so the forward vector points at /target/'s current position.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="worldUp"></param>
		public void LookAt(Transform target, Vector3 worldUp) {
			_tf.LookAt(target._tf, worldUp.vector3);
		}
		/// <summary>
		/// Rotates the transform so the forward vector points at /target/'s current position.
		/// </summary>
		/// <param name="worldPosition"></param>
		public void LookAt(Vector3 worldPosition) {
			_tf.LookAt(worldPosition.vector3);
		}
		/// <summary>
		/// Rotates the transform so the forward vector points at /target/'s current position.
		/// </summary>
		/// <param name="worldPosition"></param>
		/// <param name="worldUp"></param>
		public void LookAt(Vector3 worldPosition, Vector3 worldUp) {
			_tf.LookAt(worldPosition.vector3, worldUp.vector3);
		}
		/// <summary>
		/// Applies a rotation of eulerAngles.z degrees around the z axis, eulerAngles.x degrees around the x axis, and eulerAngles.y degrees around the y axis (in that order).
		/// </summary>
		/// <param name="eulerAngles"></param>
		public void Rotate(Vector3 eulerAngles){
			_tf.Rotate(eulerAngles.vector3);
		}
		/// <summary>
		/// Applies a rotation of eulerAngles.z degrees around the z axis, eulerAngles.x degrees around the x axis, and eulerAngles.y degrees around the y axis (in that order).
		/// </summary>
		/// <param name="xAngle"></param>
		/// <param name="yAngle"></param>
		/// <param name="zAngle"></param>
		public void Rotate(float xAngle, float yAngle, float zAngle){
			_tf.Rotate(xAngle, yAngle, zAngle);
		}
		/// <summary>
		/// Applies a rotation of eulerAngles.z degrees around the z axis, eulerAngles.x degrees around the x axis, and eulerAngles.y degrees around the y axis (in that order).
		/// </summary>
		/// <param name="axis"></param>
		/// <param name="angle"></param>
		public void Rotate(Vector3 axis, float angle){
			_tf.Rotate(axis.vector3, angle);
		}
		/// <summary>
		/// Applies a rotation of eulerAngles.z degrees around the z axis, eulerAngles.x degrees around the x axis, and eulerAngles.y degrees around the y axis (in that order).
		/// </summary>
		/// <param name="point"></param>
		/// <param name="axis"></param>
		/// <param name="angle"></param>
		public void Rotate(Vector3 point, Vector3 axis, float angle){
			_tf.RotateAround(point.vector3, axis.vector3, angle);
		}
		/// <summary>
		/// Set the parent of the transform.
		/// </summary>
		/// <param name="parent"></param>
		public void SetParent(Transform parent) {
			_tf.SetParent(parent._tf);
		}
		/// <summary>
		/// Set the parent of the transform.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="worldPositionStays"></param>
		public void SetParent(Transform parent, bool worldPositionStays) {
			_tf.SetParent(parent._tf, worldPositionStays);
		}
		/// <summary>
		/// Transforms direction from local space to world space.
		/// </summary>
		/// <param name="direction"></param>
		/// <returns></returns>
		public Vector3 TransformDirection(Vector3 direction) {
			return new Vector3(_tf.TransformDirection(direction.vector3));
		}
		/// <summary>
		/// Transforms direction from local space to world space.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public Vector3 TransformDirection(float x, float y, float z) {
			return new Vector3(_tf.TransformDirection(x, y, z));
		}
		/// <summary>
		/// Transforms position from local space to world space.
		/// </summary>
		/// <param name="direction"></param>
		/// <returns></returns>
		public Vector3 TransformPoint(Vector3 direction) {
			return new Vector3(_tf.TransformPoint(direction.vector3));
		}
		/// <summary>
		/// Transforms position from local space to world space.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public Vector3 TransformPoint(float x, float y, float z) {
			return new Vector3(_tf.TransformPoint(x, y, z));
		}
		/// <summary>
		/// Transforms vector from local space to world space.
		/// </summary>
		/// <param name="direction"></param>
		/// <returns></returns>
		public Vector3 TransformVector(Vector3 direction) {
			return new Vector3(_tf.TransformVector(direction.vector3));
		}
		/// <summary>
		/// Transforms vector from local space to world space.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public Vector3 TransformVector(float x, float y, float z) {
			return new Vector3(_tf.TransformVector(x, y, z));
		}
		/// <summary>
		/// Moves the transform in the direction and distance of translation.
		/// </summary>
		/// <param name="translation"></param>
		public void Translate(Vector3 translation){
			_tf.Translate(translation.vector3);
		}
		/// <summary>
		/// Moves the transform by x along the x axis, y along the y axis, and z along the z axis.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public void Translate(float x, float y, float z){
			_tf.Translate(x, y, z);
		}
		/// <summary>
		/// Moves the transform in the direction and distance of translation.
		/// </summary>
		/// <param name="translation"></param>
		/// <param name="relativeTo"></param>
		/// The movement is applied relative to relativeTo's local coordinate system. If relativeTo is null, the movement is applied relative to the world coordinate system.
		public void Translate(Vector3 translation, Transform relativeTo){
			_tf.Translate(translation.vector3, relativeTo._tf);
		}
		/// <summary>
		/// Moves the transform in the direction and distance of translation.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <param name="relativeTo"></param>
		/// The movement is applied relative to relativeTo's local coordinate system. If relativeTo is null, the movement is applied relative to the world coordinate system.
		public void Translate(float x, float y, float z, Transform relativeTo){
			_tf.Translate(x, y, z, relativeTo._tf);
		}

		[MoonSharpHiddenAttribute]
		public UnityEngine.Transform _tf;
		[MoonSharpHiddenAttribute]
		public Transform(UnityEngine.Transform transform) {

			if(transform == null) UnityEngine.Debug.LogError("Creating empty transform");
			_tf = transform;
		}


	}
}
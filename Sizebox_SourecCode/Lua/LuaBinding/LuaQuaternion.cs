using MoonSharp.Interpreter;

namespace Lua {
	/// <summary>
	/// Quaternions are used to represent rotations.
	/// <summary>
	/// They are compact, don't suffer from gimbal lock and can easily be interpolated. Unity internally uses Quaternions to represent all rotations.
	/// They are based on complex numbers and are not easy to understand intuitively. You almost never access or modify individual Quaternion components (x,y,z,w); most often you would just take existing rotations (e.g. from the Transform) and use them to construct new rotations (e.g. to smoothly interpolate between two rotations). The Quaternion functions that you use 99% of the time are: Quaternion.LookRotation, Quaternion.Angle, Quaternion.Euler, Quaternion.Slerp, Quaternion.FromToRotation, and Quaternion.identity. (The other functions are only for exotic uses.)

	[MoonSharpUserDataAttribute]
	public class Quaternion {
		[MoonSharpHiddenAttribute]
		public UnityEngine.Quaternion quaternion;
		/// <summary>
		/// W component of the Quaternion. Don't modify this directly unless you know quaternions inside out.
		/// </summary>
		/// <returns></returns>
		public float w { get { return quaternion.w;} set {quaternion.w = value; }}

		/// <summary>
		/// X component of the Quaternion. Don't modify this directly unless you know quaternions inside out.
		/// </summary>
		/// <returns></returns>
		public float x { get { return quaternion.x;} set {quaternion.x = value; }}
		/// <summary>
		/// 	Y component of the Quaternion. Don't modify this directly unless you know quaternions inside out.
		/// </summary>
		/// <returns></returns>
		public float y { get { return quaternion.y;} set {quaternion.y = value; }}
		/// <summary>
		/// Z component of the Quaternion. Don't modify this directly unless you know quaternions inside out.
		/// </summary>
		/// <returns></returns>
		public float z { get { return quaternion.z;} set {quaternion.z = value; }}
		/// <summary>
		/// Returns the euler angle representation of the rotation.
		/// </summary>
		/// <returns></returns>
		public Vector3 eulerAngles { get { return new Vector3(quaternion.eulerAngles); }}
		
		/// <summary>
		/// The identity rotation (Read Only).
		/// </summary>
		/// <returns></returns>
		public static Quaternion identity {get {return new Quaternion(UnityEngine.Quaternion.identity);}}

		/// <summary>
		/// Constructs new Quaternion with given x,y,z,w components.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <param name="w"></param>
		/// <returns></returns>
		public static Quaternion New(float x, float y, float z, float w) {
			return new Quaternion(new UnityEngine.Quaternion(x,y,z,w));
		}	

		[MoonSharpHiddenAttribute]
		public Quaternion(UnityEngine.Quaternion quaternion) {
			this.quaternion = quaternion;
		}

		public override string ToString() {
			return quaternion.ToString();
		}

		[MoonSharpUserDataMetamethod("__concat")]
		public static string Concat(Quaternion o, string v)
		{
			return o.ToString() + v;
		}

		[MoonSharpUserDataMetamethod("__concat")]
		public static string Concat(string v, Quaternion o)
		{
			return v + o.ToString();
		}

		[MoonSharpUserDataMetamethod("__concat")]
		public static string Concat(Quaternion o1, Quaternion o2)
		{
			return o1.ToString() + o2.ToString();
		}

		[MoonSharpUserDataMetamethod("__eq")]
		public static bool Eq(Quaternion o1, Quaternion o2)
		{
			return o1.quaternion == o2.quaternion;
		}


		public static Quaternion operator *(Quaternion lhs, Quaternion rhs)
		{
			return new Quaternion(lhs.quaternion * rhs.quaternion);
		}

		public static Vector3 operator *(Quaternion rotation, Vector3 point)
		{
			return new Vector3(rotation.quaternion * point.vector3);
		}

		/// <summary>
		/// Set x, y, z and w components of an existing Quaternion.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <param name="w"></param>
		public void Set(float x, float y, float z, float w) {
			quaternion.Set(x,y,z,w);
		}

		/// <summary>
		/// Creates a rotation which rotates from fromDirection to toDirection.
		/// </summary>
		/// <param name="fromDirection"></param>
		/// <param name="toDirection"></param>
		public void SetFromToRotation(Vector3 fromDirection, Vector3 toDirection) {
			quaternion.SetFromToRotation(fromDirection.vector3, toDirection.vector3);
		}

		/// <summary>
		/// Creates a rotation with the specified forward and upwards directions.
		/// </summary>
		/// <param name="view"></param>
		public void SetLookRotation(Vector3 view) {
			quaternion.SetLookRotation(view.vector3);
		}

		/// <summary>
		/// Creates a rotation with the specified forward and upwards directions.
		/// </summary>
		/// <param name="view"></param>
		/// <param name="up"></param>
		public void SetLookRotation(Vector3 view, Vector3 up) {
			quaternion.SetLookRotation(view.vector3, up.vector3);
		}

		/// <summary>
		/// Returns the angle in degrees between two rotations a and b.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float Angle(Quaternion a, Quaternion b) {
			return UnityEngine.Quaternion.Angle(a.quaternion, b.quaternion);
		}

		/// <summary>
		/// Creates a rotation which rotates angle degrees around axis.
		/// </summary>
		/// <param name="angle"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		public static Quaternion AngleAxis(float angle, Vector3 axis) {
			return new Quaternion(UnityEngine.Quaternion.AngleAxis(angle, axis.vector3));
		}

		/// <summary>
		/// The dot product between two rotations.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float Dot(Quaternion a, Quaternion b) {
			return UnityEngine.Quaternion.Dot(a.quaternion, b.quaternion);
		}

		/// <summary>
		/// Returns a rotation that rotates z degrees around the z axis, x degrees around the x axis, and y degrees around the y axis (in that order).
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public static Quaternion Euler(float x, float y, float z) {
			return new Quaternion(UnityEngine.Quaternion.Euler(x,y,z));
		}

		/// <summary>
		/// Returns a rotation that rotates z degrees around the z axis, x degrees around the x axis, and y degrees around the y axis (in that order).
		/// </summary>
		/// <param name="euler"></param>
		/// <returns></returns>
		public static Quaternion Euler(Vector3 euler) {
			return new Quaternion(UnityEngine.Quaternion.Euler(euler.vector3));
		}

		/// <summary>
		/// Creates a rotation which rotates from fromDirection to toDirection.
		/// </summary>
		/// <param name="fromDirection"></param>
		/// <param name="toDirection"></param>
		/// <returns></returns>
		/// Usually you use this to rotate a transform so that one of its axes eg. the y-axis - follows a target direction toDirection in world space.
		public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection) {
			return new Quaternion(UnityEngine.Quaternion.FromToRotation(fromDirection.vector3, toDirection.vector3));
		}

		/// <summary>
		/// Returns the Inverse of rotation.
		/// </summary>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public static Quaternion Inverse(Quaternion rotation) {
			return new Quaternion(UnityEngine.Quaternion.Inverse(rotation.quaternion));
		}

		/// <summary>
		/// Interpolates between a and b by t and normalizes the result afterwards. The parameter t is clamped to the range [0, 1].
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Quaternion Lerp(Quaternion a, Quaternion b, float t) {
			return new Quaternion(UnityEngine.Quaternion.Lerp(a.quaternion, b.quaternion, t));
		}

		/// <summary>
		/// Interpolates between a and b by t and normalizes the result afterwards. The parameter t is not clamped.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Quaternion LerpUnclamped(Quaternion a, Quaternion b, float t) {
			return new Quaternion(UnityEngine.Quaternion.LerpUnclamped(a.quaternion, b.quaternion, t));
		}

		/// <summary>
		/// Creates a rotation with the specified forward and upwards directions.
		/// </summary>
		/// <param name="forward">The direction to look in.</param>
		/// <returns>Returns the computed quaternion. If used to orient a Transform, the Z axis will be aligned with forward/ and the Y axis with upwards if these vectors are orthogonal. Logs an error if the forward direction is zero.</returns>
		public static Quaternion LookRotation(Vector3 forward){
			return new Quaternion(UnityEngine.Quaternion.LookRotation(forward.vector3));
		}

		/// <summary>
		/// Creates a rotation with the specified forward and upwards directions.
		/// </summary>
		/// <param name="forward">The direction to look in.</param>
		/// <param name="upwards">The vector that defines in which direction up is.</param>
		/// <returns>Returns the computed quaternion. If used to orient a Transform, the Z axis will be aligned with forward/ and the Y axis with upwards if these vectors are orthogonal. Logs an error if the forward direction is zero.</returns>
		public static Quaternion LookRotation(Vector3 forward, Vector3 upwards){
			return new Quaternion(UnityEngine.Quaternion.LookRotation(forward.vector3, upwards.vector3));
		}

		/// <summary>
		/// Rotates a rotation from towards to.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="maxDegreesDelta"></param>
		/// <returns></returns>
		public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDegreesDelta){
			return new Quaternion(UnityEngine.Quaternion.RotateTowards(from.quaternion, to.quaternion, maxDegreesDelta));
		}

		/// <summary>
		/// Spherically interpolates between a and b by t. The parameter t is clamped to the range [0, 1].
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Quaternion Slerp(Quaternion a, Quaternion b, float t) {
			return new Quaternion(UnityEngine.Quaternion.Slerp(a.quaternion, b.quaternion, t));
		}

		/// <summary>
		/// Spherically interpolates between a and b by t. The parameter t is not clamped.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t) {
			return new Quaternion(UnityEngine.Quaternion.SlerpUnclamped(a.quaternion, b.quaternion, t));
		}

	}
}
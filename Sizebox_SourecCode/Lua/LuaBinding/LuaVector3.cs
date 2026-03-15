using MoonSharp.Interpreter;

namespace Lua
{
	/// <summary>
	/// Representation of 3D vectors and points.
	/// </summary> 
	/// This structure is used throughout Unity to pass 3D positions and directions around. It also contains functions for doing common vector operations.
	/// 
	[MoonSharpUserDataAttribute]
	public class Vector3 {
		[MoonSharpHiddenAttribute]
		public UnityEngine.Vector3 vector3;
		/// <summary>
		/// X component of the vector.
		/// </summary>
		/// <returns></returns>
		public float x { get { return vector3.x;} set {vector3.x = value; }}
		/// <summary>
		/// Y component of the vector.
		/// </summary>
		/// <returns></returns>
		public float y { get { return vector3.y;} set {vector3.y = value; }}
		/// <summary>
		/// Z component of the vector.
		/// </summary>
		/// <returns></returns>
		public float z { get { return vector3.z;} set {vector3.z = value; }}
		/// <summary>
		/// Returns the length of this vector (Read Only).
		/// </summary>
		/// <returns></returns>
		public float magnitude { get { return vector3.magnitude; }}
		/// <summary>
		/// Returns the squared length of this vector (Read Only).
		/// </summary>
		/// <returns></returns>
		public float sqrMagnitude { get { return vector3.sqrMagnitude; }}
		/// <summary>
		/// Returns this vector with a magnitude of 1 (Read Only).
		/// </summary>
		/// <returns></returns>
		public Vector3 normalized { get { return new Vector3(vector3.normalized); }}
		/// <summary>
		/// Shorthand for writing Vector3(0, 0, -1).
		/// </summary>
		/// <returns></returns>
		public static Vector3 back { get { return new Vector3(UnityEngine.Vector3.back);}}
		/// <summary>
		/// Shorthand for writing Vector3(0, -1, 0).
		/// </summary>
		/// <returns></returns>
		public static Vector3 down { get { return new Vector3(UnityEngine.Vector3.down);}}
		/// <summary>
		/// Shorthand for writing Vector3(0, 0, 1).
		/// </summary>
		/// <returns></returns>
		public static Vector3 forward { get { return new Vector3(UnityEngine.Vector3.forward);}}
		/// <summary>
		/// Shorthand for writing Vector3(-1, 0, 0).
		/// </summary>
		/// <returns></returns>
		public static Vector3 left { get { return new Vector3(UnityEngine.Vector3.left);}}
		/// <summary>
		/// Shorthand for writing Vector3(1, 1, 1).
		/// </summary>
		/// <returns></returns>
		public static Vector3 one { get {return new Vector3(UnityEngine.Vector3.one);}}
		/// <summary>
		/// Shorthand for writing Vector3(1, 0, 0).
		/// </summary>
		/// <returns></returns>
		public static Vector3 right { get { return new Vector3(UnityEngine.Vector3.right);}}
		/// <summary>
		/// Shorthand for writing Vector3(0, 1, 0).
		/// </summary>
		/// <returns></returns>
		public static Vector3 up { get { return new Vector3(UnityEngine.Vector3.up);}}
		/// <summary>
		/// Shorthand for writing Vector3(0, 0, 0).
		/// </summary>
		/// <returns></returns>
		public static Vector3 zero { get { return new Vector3(UnityEngine.Vector3.zero);}}
		/// <summary>
		/// Creates a new vector with given x, y, z components.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public static Vector3 New(float x, float y, float z) {
			return new Vector3(new UnityEngine.Vector3(x,y,z));
		}	

		[MoonSharpHiddenAttribute]
		public Vector3(UnityEngine.Vector3 vector3) {
			this.vector3 = vector3;
		}

		public override string ToString() {
			return vector3.ToString();
		}

		[MoonSharpUserDataMetamethod("__concat")]
		public static string Concat(Vector3 o, string v)
		{
			return o.ToString() + v;
		}

		[MoonSharpUserDataMetamethod("__concat")]
		public static string Concat(string v, Vector3 o)
		{
			return v + o.ToString();
		}

		[MoonSharpUserDataMetamethod("__concat")]
		public static string Concat(Vector3 o1, Vector3 o2)
		{
			return o1.ToString() + o2.ToString();
		}

		[MoonSharpUserDataMetamethod("__eq")]
		public static bool Eq(Vector3 o1, Vector3 o2)
		{
			return o1.vector3 == o2.vector3;
		}

		public static Vector3 operator +(Vector3 o1, Vector3 o2)
		{
			return new Vector3(o1.vector3 + o2.vector3);
		}

		public static Vector3 operator -(Vector3 o1, Vector3 o2)
		{
			return new Vector3(o1.vector3 - o2.vector3);
		}

		public static Vector3 operator -(Vector3 o1)
		{
			return new Vector3(-o1.vector3);
		}

		public static Vector3 operator *(Vector3 o1, float f)
		{
			return new Vector3(o1.vector3 * f);
		}

		public static Vector3 operator *(float f, Vector3 o1)
		{
			return new Vector3(f * o1.vector3);
		}

		public static Vector3 operator /(Vector3 o1, float f)
		{
			return new Vector3(o1.vector3 / f);
		}
		/// <summary>
		/// Makes this vector have a magnitude of 1.
		/// </summary> 
		/// When normalized, a vector keeps the same direction but its length is 1.0.
		/// Note that this function will change the current vector. If you want to keep the current vector unchanged, use normalized variable.
		/// If this vector is too small to be normalized it will be set to zero.
		public void Normalize() {
			vector3.Normalize();
		}
		/// <summary>
		/// Set x, y and z components of an existing Vector3.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public void Set(float x, float y, float z) {
			vector3.Set(x,y,z);
		}
		/// <summary>
		/// Returns the angle in degrees between from and to.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public static float Angle(Vector3 from, Vector3 to) {
			return UnityEngine.Vector3.Angle(from.vector3, to.vector3);
		}
		/// <summary>
		/// Returns a copy of vector with its magnitude clamped to maxLength.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="maxLength"></param>
		/// <returns></returns>
		public static Vector3 ClampMagnitude(Vector3 vector, float maxLength) {
			return new Vector3(UnityEngine.Vector3.ClampMagnitude(vector.vector3, maxLength));
		}
		/// <summary>
		/// Cross Product of two vectors.
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static Vector3 Cross(Vector3 lhs, Vector3 rhs) {
			return new Vector3(UnityEngine.Vector3.Cross(lhs.vector3, rhs.vector3));
		}
		/// <summary>
		/// Returns the distance between a and b.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float Distance(Vector3 a, Vector3 b) {
			return UnityEngine.Vector3.Distance(a.vector3, b.vector3);
		}
		/// <summary>
		/// Dot Product of two vectors.
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static float Dot(Vector3 lhs, Vector3 rhs) {
			return UnityEngine.Vector3.Dot(lhs.vector3, rhs.vector3);
		}
		/// <summary>
		/// Linearly interpolates between two vectors.

		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Vector3 Lerp(Vector3 a, Vector3 b, float t) {
			return new Vector3(UnityEngine.Vector3.Lerp(a.vector3, b.vector3, t));
		}
		/// <summary>
		/// Linearly interpolates between two vectors.

		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) {
			return new Vector3(UnityEngine.Vector3.LerpUnclamped(a.vector3, b.vector3, t));
		}
		/// <summary>
		/// Returns a vector that is made from the largest components of two vectors.
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static Vector3 Max(Vector3 lhs, Vector3 rhs) {
			return new Vector3(UnityEngine.Vector3.Max(lhs.vector3, rhs.vector3));
		}
		/// <summary>
		/// Returns a vector that is made from the smallest components of two vectors.
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static Vector3 Min(Vector3 lhs, Vector3 rhs){
			return new Vector3(UnityEngine.Vector3.Min(lhs.vector3, rhs.vector3));
		}
		/// <summary>
		/// Moves a point current in a straight line towards a target point.
		/// </summary>
		/// <param name="current"></param>
		/// <param name="target"></param>
		/// <param name="maxDistanceDelta"></param>
		/// <returns></returns>
		public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta) {
			return new Vector3(UnityEngine.Vector3.MoveTowards(current.vector3, target.vector3, maxDistanceDelta));
		}
		/// <summary>
		/// Projects a vector onto another vector.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="onNormal"></param>
		/// <returns></returns>
		public static Vector3 Project(Vector3 vector, Vector3 onNormal) {
			return new Vector3(UnityEngine.Vector3.Project(vector.vector3, onNormal.vector3));
		}
		/// <summary>
		/// Projects a vector onto a plane defined by a normal orthogonal to the plane.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="planeNormal"></param>
		/// <returns></returns>
		public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal){
			return new Vector3(UnityEngine.Vector3.ProjectOnPlane(vector.vector3, planeNormal.vector3));
		}
		/// <summary>
		/// Reflects a vector off the plane defined by a normal.
		/// </summary>
		/// <param name="inDirection"></param>
		/// <param name="inNormal"></param>
		/// <returns></returns>
		public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal) {
			return new Vector3(UnityEngine.Vector3.Reflect(inDirection.vector3, inNormal.vector3));
		}
		/// <summary>
		/// Rotates a vector current towards target.
		/// </summary>
		/// <param name="current"></param>
		/// <param name="target"></param>
		/// <param name="maxRadiansDelta"></param>
		/// <param name="maxMagnitudeDelta"></param>
		/// <returns></returns>
		public static Vector3 RotateTowards(Vector3 current, Vector3 target, float maxRadiansDelta, float maxMagnitudeDelta) {
			return new Vector3(UnityEngine.Vector3.RotateTowards(current.vector3, target.vector3, maxRadiansDelta, maxMagnitudeDelta));
		}
		/// <summary>
		/// Multiplies two vectors component-wise.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector3 Scale(Vector3 a, Vector3 b) {
			return new Vector3(UnityEngine.Vector3.Scale(a.vector3,b.vector3));
		}
		/// <summary>
		/// Spherically interpolates between two vectors.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Vector3 Slerp(Vector3 a, Vector3 b, float t) {
			return new Vector3(UnityEngine.Vector3.Slerp(a.vector3, b.vector3, t));
		}
		/// <summary>
		/// Spherically interpolates between two vectors.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Vector3 SlerpUnclamped(Vector3 a, Vector3 b, float t) {
			return new Vector3(UnityEngine.Vector3.SlerpUnclamped(a.vector3, b.vector3, t));
		}

	}
}
using MoonSharp.Interpreter;

namespace Lua
{
	/// <summary>
	/// Class for generating random data.
	/// </summary>
	[MoonSharpUserDataAttribute]
	public static class Random {
		/// <summary>
		/// Returns a random point inside a circle with radius 1 (Read Only).
		/// </summary>
		/// <returns></returns>
		public static Vector3 insideUnitCircle {
			get { return new Vector3(UnityEngine.Random.insideUnitCircle);}
		}

		/// <summary>
		/// Returns a random point inside a sphere with radius 1 (Read Only).
		/// </summary>
		/// <returns></returns>
		public static Vector3 insideUnitSphere {
			get { return new Vector3(UnityEngine.Random.insideUnitSphere);}
		}

		/// <summary>
		/// Returns a random point on the surface of a sphere with radius 1 (Read Only).
		/// </summary>
		/// <returns></returns>
		public static Vector3 onUnitSphere {
			get { return new Vector3(UnityEngine.Random.onUnitSphere);}
		}

		/// <summary>
		/// Returns a random rotation (Read Only).
		/// </summary>
		/// <returns></returns>
		public static Quaternion rotation {
			get { return new Quaternion(UnityEngine.Random.rotation);}
		}

		/// <summary>
		/// Returns a random rotation with uniform distribution (Read Only).
		/// </summary>
		/// <returns></returns>
		public static Quaternion rotationUniform {
			get { return new Quaternion(UnityEngine.Random.rotationUniform);}
		}

		/// <summary>
		/// Returns a random number between 0.0 [inclusive] and 1.0 [inclusive] (Read Only).
		/// </summary>
		/// <returns></returns>
		public static float value {
			get { return UnityEngine.Random.value;}
		}

		/// <summary>
		/// Initializes the random number generator state with a seed. 
		/// </summary>
		/// <param name="seed"></param>
		public static void InitState(int seed){
			UnityEngine.Random.InitState(seed);
		}

		/// <summary>
		/// Returns a random float number between and min [inclusive] and max [inclusive] (Read Only).
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static float Range(int min, int max){
			return UnityEngine.Random.Range(min, max);
		}

		/// <summary>
		/// Returns a random float number between and min [inclusive] and max [inclusive] (Read Only). 
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static float Range(float min, float max){
			return UnityEngine.Random.Range(min, max);
		}

	}
}
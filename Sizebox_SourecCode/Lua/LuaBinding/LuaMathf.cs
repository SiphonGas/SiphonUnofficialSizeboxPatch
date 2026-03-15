using MoonSharp.Interpreter;

namespace Lua {
    /// <summary>
    /// A collection of common math functions.
    /// </summary>
    [MoonSharpUserDataAttribute]
    public static class Mathf {
        /// <summary>
        /// Degrees-to-radians conversion constant (Read Only). 
        /// </summary>
        /// <returns></returns>
        public static float Deg2Rad { get {return UnityEngine.Mathf.Deg2Rad;}}

        /// <summary>
        /// A tiny floating point value (Read Only).
        /// </summary>
        /// <returns></returns>
        public static float Epsilon { get {return UnityEngine.Mathf.Epsilon;}}

        /// <summary>
        /// A representation of positive infinity (Read Only).
        /// </summary>
        /// <returns></returns>
        public static float Infinity { get {return UnityEngine.Mathf.Infinity;}}

        /// <summary>
        /// A representation of negative infinity (Read Only).
        /// </summary>
        /// <returns></returns>
        public static float NegativeInfinity { get {return UnityEngine.Mathf.NegativeInfinity;}}

        /// <summary>
        /// The infamous 3.14159265358979... value (Read Only).
        /// </summary>
        /// <returns></returns>
        public static float PI { get {return UnityEngine.Mathf.PI;}}

        /// <summary>
        /// Radians-to-degrees conversion constant (Read Only).
        /// </summary>
        /// <returns></returns>
        public static float Rad2Deg { get {return UnityEngine.Mathf.Rad2Deg;}}

        /// <summary>
        /// Returns the absolute value of f.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Abs(float f) {
            return UnityEngine.Mathf.Abs(f);
        }

        /// <summary>
        /// Returns the arc-cosine of f - the angle in radians whose cosine is f.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Acos(float f){
            return UnityEngine.Mathf.Acos(f);
        }

        /// <summary>
        /// Compares two floating point values and returns true if they are similar.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Approximately(float a, float b) {
            return UnityEngine.Mathf.Approximately(a, b);
        }

        /// <summary>
        /// Returns the arc-sine of f - the angle in radians whose sine is f.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Asin(float f) {
            return UnityEngine.Mathf.Asin(f);
        }


        /// <summary>
        /// Returns the arc-tangent of f - the angle in radians whose tangent is f.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Atan(float f){
            return UnityEngine.Mathf.Atan(f);
        }

        /// <summary>
        /// Returns the angle in radians whose Tan is y/x.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static float Atan2(float y, float x){
            return UnityEngine.Mathf.Atan2(y, x);
        }

        /// <summary>
        /// Returns the smallest integer greater to or equal to f.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Ceil(float f) {
            return UnityEngine.Mathf.Ceil(f);
        }

        /// <summary>
        /// Clamps a value between a minimum float and maximum float value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float Clamp(float value, float min, float max) {
            return UnityEngine.Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Clamps value between 0 and 1 and returns value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float Clamp01(float value){
            return UnityEngine.Mathf.Clamp01(value);
        }

        /// <summary>
        /// Returns the closest power of two value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ClosestPowerOfTwo(int value) {
            return UnityEngine.Mathf.ClosestPowerOfTwo(value);
        }

        /// <summary>
        /// Returns the cosine of angle f in radians.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Cos(float f){
            return UnityEngine.Mathf.Cos(f);
        }

        /// <summary>
        /// Calculates the shortest difference between two given angles given in degrees.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static float DeltaAngle(float current, float target){
            return UnityEngine.Mathf.DeltaAngle(current, target);
        }

        /// <summary>
        /// Returns e raised to the specified power.
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        public static float Exp(float power) {
            return UnityEngine.Mathf.Exp(power);
        }

        /// <summary>
        /// Returns the largest integer smaller to or equal to f.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Floor(float f){
            return UnityEngine.Mathf.Floor(f);
        }

        /// <summary>
        /// Calculates the linear parameter t that produces the interpolant value within the range [a, b].
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float InverseLerp(float a, float b, float value) {
            return UnityEngine.Mathf.InverseLerp(a, b, value);
        }

        /// <summary>
        /// Returns true if the value is power of two.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsPowerOfTwo(int value) {
            return UnityEngine.Mathf.IsPowerOfTwo(value);
        }

        /// <summary>
        /// Linearly interpolates between a and b by t.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float Lerp(float a, float b, float t) {
            return UnityEngine.Mathf.Lerp(a, b, t);
        }

        /// <summary>
        /// Same as Lerp but makes sure the values interpolate correctly when they wrap around 360 degrees.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float LerpAngle(float a, float b, float t){
            return UnityEngine.Mathf.LerpAngle(a, b, t);
        }

        /// <summary>
        /// Linearly interpolates between a and b by t with no limit to t.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float LerpUnclamped(float a, float b, float t) {
            return UnityEngine.Mathf.LerpUnclamped(a, b, t);
        }

        /// <summary>
        /// Returns the logarithm of a specified number in a specified base.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static float Log(float f, float p) {
            return UnityEngine.Mathf.Log(f, p);
        }

        /// <summary>
        /// Returns the base 10 logarithm of a specified number.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Log10(float f) {
            return UnityEngine.Mathf.Log10(f);
        }

        /// <summary>
        /// Returns largest of two or more values.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Max(float a, float b) {
            return UnityEngine.Mathf.Max(a, b);
        }

        /// <summary>
        /// Returns largest of two or more values.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Max(params float[] values){
            return UnityEngine.Mathf.Max(values);
        }

        /// <summary>
        /// Returns the smallest of two or more values.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Min(float a, float b) {
            return UnityEngine.Mathf.Min(a, b);
        }

        /// <summary>
        /// Returns the smallest of two or more values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float Min(params float[] values){
            return UnityEngine.Mathf.Min(values);
        }

        /// <summary>
        /// Moves a value current towards target.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="maxDelta"></param>
        /// <returns></returns>
        public static float MoveTowards(float current, float target, float maxDelta){
            return UnityEngine.Mathf.MoveTowards(current, target, maxDelta);
        }

        /// <summary>
        /// Returns the next power of two value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int NextPowerOfTwo(int value){
            return UnityEngine.Mathf.NextPowerOfTwo(value);
        }

        /// <summary>
        /// Generate 2D Perlin noise.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        /// Perlin noise is a pseudo-random pattern of float values generated across a 2D plane (although the technique does generalise to three or more dimensions, this is not implemented in Unity). The noise does not contain a completely random value at each point but rather consists of "waves" whose values gradually increase and decrease across the pattern. The noise can be used as the basis for texture effects but also for animation, generating terrain heightmaps and many other things.
        public static float PerlinNoise(float x, float y){
            return PerlinNoise(x,y);
        }

        /// <summary>
        /// PingPongs the value t, so that it is never larger than length and never smaller than 0.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static float PingPong(float t, float length){
            return UnityEngine.Mathf.PingPong(t, length);
        }

        /// <summary>
        /// Returns f raised to power p.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static float Pow(float f, float p){
            return UnityEngine.Mathf.Pow(f, p);
        }

        /// <summary>
        /// Loops the value t, so that it is never larger than length and never smaller than 0.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static float Repeat(float t, float length){
            return UnityEngine.Mathf.Repeat(t, length);
        }

        /// <summary>
        /// Returns f rounded to the nearest integer.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Round(float f){
            return UnityEngine.Mathf.Round(f);
        }

        /// <summary>
        /// Returns the sign of f.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Sign(float f){
            return UnityEngine.Mathf.Sign(f);
        }

        /// <summary>
        /// 	Returns the sine of angle f in radians.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Sin(float f){
            return UnityEngine.Mathf.Sin(f);
        }

        /// <summary>
        /// Returns square root of f.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Sqrt(float f){
            return UnityEngine.Mathf.Sqrt(f);
        }

        /// <summary>
        /// Interpolates between min and max with smoothing at the limits.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float SmoothStep(float from, float to, float t){
            return UnityEngine.Mathf.SmoothStep(from, to, t);
        }

        /// <summary>
        /// Returns the tangent of angle f in radians.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static float Tan(float f) {
            return UnityEngine.Mathf.Tan(f);
        }

    }
}
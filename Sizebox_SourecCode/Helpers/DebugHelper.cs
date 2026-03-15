using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DebugHelper {

	static float startTime;

	public static void StartCron() {
		startTime = Time.realtimeSinceStartup;
	}

	public static void LogCron(string msg) {
		float diff = (Time.realtimeSinceStartup - startTime) * 1000;
		Debug.Log(msg + ": " + diff + " ms");
	}

	public static void LogAndRestart(string msg) {
		LogCron(msg);
		StartCron();
	}
}

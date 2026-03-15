using UnityEngine;

public class StateManager : MonoBehaviour {

	// static
	public static StateManager instance { 
		get {
			if(cachedInstance == null) {
				cachedInstance = new GameObject("State Manager").AddComponent<StateManager>();
				DontDestroyOnLoad(cachedInstance);
			} 
			return cachedInstance;
		}
	}
	public static StateManager cachedInstance;


	// public
	public PlayerData myData;
	public GameSettings gameSettings;

	// Use this for initialization
	void Awake () {
		myData = new PlayerData();
		gameSettings = new GameSettings();
	}
}

public class PlayerData {
	public string name;

}

public class GameSettings {
	public bool multiplayer = false;
	public string scene;
}

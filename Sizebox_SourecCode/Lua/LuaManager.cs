using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using AI;

public class LuaManager : MonoBehaviour {
	List<LuaScript> luaScripts;
	PlayerEntity p_player;
	UnityProxies.World world;
	UnityProxies.AllGiantess gts;
	LuaGlobals globalTable;
	bool updateErrorPrinted = false;

	static string initializationCode = @"
	BehaviorBase = { agent = nil }
	behaviorList = {}

	function BehaviorBase:new(o)
		o = o or {}
		setmetatable(o, self)
		self.__index = self
		return o
	end

	function BehaviorBase:Start()
	end

	function BehaviorBase:FixedUpdate()
	end

	function BehaviorBase:Exit()
	end

	function RegisterBehavior(behaviorName) 
		local b = BehaviorBase:new{name = behaviorName}
		behaviorList[behaviorName] = b
		return b
	end

	function CreateInstance(behaviorName, newagent, target, cursor)
		local BehaviorClass = behaviorList[behaviorName]
		local instance = BehaviorClass:new{name = behaviorName, agent = newagent, target = target, cursorPoint = cursor}
		return instance
	end
	
	";

	void IntializeValues()
	{
		Transform target = Camera.main.GetComponent<PlayerCamera>().target;
		Player player = target.GetComponent<Player>();
		p_player = new PlayerEntity(player);
		world = new UnityProxies.World();
		gts = new UnityProxies.AllGiantess();
		globalTable = new LuaGlobals();
	}

	// Use this for initialization
	void Start () {
		UserData.RegisterAssembly();
		IntializeValues();
		LoadBehaviors();
		LuaInitialize();
		LuaStart();
	}
	
	// Update is called once per frame
	void Update () {
		LuaUpdate();
	}

	void LoadBehaviors() {
		BehaviorLists.Initialize();
		MicroDecisionMaker.InitializeBehaviorList();
		GTSDecisionMaker.InitializeBehaviorList();

		string scriptsPath = Application.streamingAssetsPath + "/lua/behaviors/";
		string[] files = Directory.GetFiles(scriptsPath);
		foreach(string file in files) 
		{
			if(!file.EndsWith(".lua")) continue;
			string filename = Path.GetFileName(file);
			string code = File.ReadAllText(file);

			Script script = InitializeScript();

			try {
				script.DoString(code);
				try {
					Table behaviorList = script.Globals.Get("behaviorList").Table;
					foreach(TablePair pair in behaviorList.Pairs) {
						// Debug.Log("key:" + pair.Key.String);
						Behavior behavior = new Behavior(pair.Value.Table, script);
						behavior.name = filename;
						behavior.code = code;
						BehaviorLists.Instance.AddBehavior(behavior);
						if(behavior.ai) {
							MicroDecisionMaker.RegisterBehavior(behavior);
							GTSDecisionMaker.RegisterBehavior(behavior);
						}
					}
				} 
				catch (ScriptRuntimeException e)
				{
					Debug.LogError(filename + " " + e.DecoratedMessage);
				}
			}
			catch (ScriptRuntimeException e)
			{
				Debug.LogError(filename + "  " + e.DecoratedMessage);
			}			
		}
	}

	void LuaInitialize()
	{
		List<string> scripts = LoadScripts();
		luaScripts = new List<LuaScript>();

		foreach(string luaCode in scripts) 
		{
			LuaScript luaScript = new LuaScript();
			luaScript.script = InitializeScript();
			try {
				luaScript.script.DoString(luaCode);
			} catch(ScriptRuntimeException ex) {
				Debug.LogError(ex.DecoratedMessage);
			}
			


			luaScript.start = luaScript.script.Globals.Get("Start");
			if(luaScript.start == DynValue.Nil) {
				luaScript.start = null;
			}

			luaScript.update = luaScript.script.Globals.Get("Update");
			if(luaScript.update == DynValue.Nil) {
				luaScript.update = null;
			}

			luaScript.coroutine = luaScript.script.Globals.Get("Coroutine");
			if(luaScript.coroutine == DynValue.Nil) {
				luaScript.coroutine = null;
			} else {
				DynValue coroutineFunction = luaScript.script.Globals.Get("Coroutine");
				luaScript.coroutine = luaScript.script.CreateCoroutine(coroutineFunction);
			}

			luaScripts.Add(luaScript);
		}

		
	}

	void LuaStart()
	{
		foreach(LuaScript luaScript in luaScripts) {
			if(luaScript.start != null)
			{
				luaScript.script.Call(luaScript.start);
			}
		}
		
	}

	void LuaUpdate()
	{
		if(luaScripts == null) {
			if(!updateErrorPrinted) {
				Debug.LogError("No script could be loaded due to error in one of them");
				updateErrorPrinted = true;
			}
			return;
		}

		foreach(LuaScript luaScript in luaScripts) {
			if(luaScript.update != null)
			{
				try {
					luaScript.script.Call(luaScript.update);
				} catch (ScriptRuntimeException ex) {
					Debug.Log(ex.DecoratedMessage);
				}
				
			}
			if(luaScript.coroutine != null) {
				luaScript.coroutine.Coroutine.Resume();
			}
		}
		
	}

	List<string> LoadScripts()
	{
		string scriptsPath = Application.streamingAssetsPath + "/lua/";
		string[] files = Directory.GetFiles(scriptsPath);
		List<string> scripts = new List<string>();
		foreach(string file in files) 
		{
			if(!file.EndsWith(".lua")) continue;
			string scriptContent = LoadLuaFile(file);
			scripts.Add(scriptContent);

		}
		return scripts;
	}

	public static string LoadLuaFile(string path) {
		return File.ReadAllText(path);
	}

	static void Log(string message)
	{
		Debug.Log(message);
	}

	public class LuaScript {
		public Script script;
		public DynValue start;
		public DynValue update;
		public DynValue coroutine;

		public LuaScript()
		{
			script = new Script();
		}

	}

	[MoonSharpUserDataAttribute]
	public static class UnityTime {
		public static float time {
			get { return Time.time; }
		}
		public static float delta_time {
			get { return Time.deltaTime; }
		}
	}

	public Script InitializeScript() {
		// 2.2 MB of allocation
		// high CPU usage
		Script script = new Script();

		script.Globals["Log"] = (System.Action<string>) Log;
		script.Globals["log"] = (System.Action<string>) Log;
		script.Globals["time"] = typeof(UnityTime);
		script.Globals["player"] = p_player;
		script.Globals["world"] = world;
		script.Globals["gts"] = gts;
		script.Globals["globals"] = globalTable;

		script.Globals["Vector3"] = typeof(Lua.Vector3);
		script.Globals["Quaternion"] = typeof(Lua.Quaternion);
		script.Globals["Mathf"] = typeof(Lua.Mathf);
		script.Globals["Random"] = typeof(Lua.Random);
		script.Globals["Time"] = typeof(Lua.Time);
		script.Globals["Input"] = typeof(Lua.Input);
		script.Globals["Screen"] = typeof(Lua.Screen);
		script.Globals["AudioSource"] = typeof(Lua.AudioSource);

		script.Options.DebugPrint = (System.Action<string>) Log;

		
		
		// Debug.Log("Lua path: " + luaPath);
		script.Options.ScriptLoader = new StreamingAssetsScriptLoader();

		script.DoString(initializationCode);

		return script;
	}

	private class StreamingAssetsScriptLoader : ScriptLoaderBase
	{
		public override object LoadFile(string file, Table globalContext)
		{
			string luaPath = Application.streamingAssetsPath + "/lua/" + file;
			if(!File.Exists(luaPath)) {
				Debug.LogError(file + " could not be found in lua directory");
				return null;
			}
			return LuaManager.LoadLuaFile(luaPath);
		}

		public override bool ScriptFileExists(string name)
		{
			return true;
		}
	}

}

[MoonSharpUserDataAttribute]
public class LuaGlobals {
	Dictionary<DynValue, DynValue> globalTable;

	[MoonSharpHiddenAttribute]
	public LuaGlobals() {
		globalTable = new Dictionary<DynValue, DynValue>();
	}
	
	public DynValue this[DynValue idx]
	{
		get { if(globalTable.ContainsKey(idx)) return globalTable[idx]; else return null; }
		set { globalTable[idx] = value; }
	}
}
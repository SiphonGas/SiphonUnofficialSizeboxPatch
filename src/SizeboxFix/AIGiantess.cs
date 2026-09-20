using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using BepInEx;

namespace SizeboxFix
{
    /// <summary>
    /// AI Agent Manager. Manages multiple GiantessAgent instances.
    /// Keeps the AIGiantess class name for backward compatibility with keybind handler + settings panel.
    /// </summary>
    public class AIGiantess : MonoBehaviour
    {
        public static AIGiantess Instance { get; private set; }

        // Config
        internal SharedConfig _sharedConfig;

        static readonly SharedConfig _fallbackKeys = new SharedConfig();
        /// <summary>
        /// Current keybinds. Falls back to the defaults if the manager is not up
        /// yet, so the handler never reads a null config.
        /// </summary>
        internal static SharedConfig Keys
        {
            get
            {
                var i = Instance;
                return (i != null && i._sharedConfig != null) ? i._sharedConfig : _fallbackKeys;
            }
        }
        internal List<AgentConfig> _agentConfigs;
        int _nextConfigIndex;

        // Active agents
        Dictionary<int, GiantessAgent> _activeAgents = new Dictionary<int, GiantessAgent>();

        // Shared conversation history — all agents read/write to this
        internal static List<string> _sharedConversation = new List<string>();
        internal static readonly object _convLock = new object();

        // Speech queue — only one agent speaks at a time
        static Queue<System.Action> _speechQueue = new Queue<System.Action>();
        internal static bool _isSpeaking;
        internal static GiantessAgent _currentSpeaker;
        static float _speakingStartTime;

        internal static void QueueSpeech(System.Action speakAction)
        {
            // Drop old entries if queue is too long — prevent infinite backlog
            while (_speechQueue.Count > 4)
                _speechQueue.Dequeue();
            _speechQueue.Enqueue(speakAction);
        }

        void Update()
        {
            // Safety: unstick speaking after 15 seconds and advance ticket
            if (_isSpeaking && Time.time > _speakingStartTime + 15f)
            {
                _isSpeaking = false;
                _currentSpeaker = null;
                _audioNowServing++;
            }

            // Also advance ticket if no one is speaking but ticket is behind
            if (!_isSpeaking && _audioNowServing < _audioTicketCounter)
            {
                // Check if the current ticket holder's audio already finished
                bool currentHolderPlaying = false;
                var agents = GetAllAgents();
                foreach (var a in agents)
                {
                    if (a._ttsAudioSource != null && a._ttsAudioSource.isPlaying)
                    {
                        currentHolderPlaying = true;
                        break;
                    }
                }
                if (!currentHolderPlaying)
                    _audioNowServing++;
            }

            // Process speech queue — start next speech when current finishes
            if (!_isSpeaking && _speechQueue.Count > 0)
            {
                _speakingStartTime = Time.time;
                var next = _speechQueue.Dequeue();
                next();
            }
        }

        void OnGUI()
        {
            if (!_showPicker || _agentConfigs == null || _agentConfigs.Count == 0) return;

            float w = 200f;
            float btnH = 35f;
            float h = _agentConfigs.Count * (btnH + 5) + 50f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;

            GUI.Box(new Rect(x - 5, y - 5, w + 10, h + 10), "");
            GUI.Box(new Rect(x - 5, y - 5, w + 10, h + 10), "Assign Personality");

            float curY = y + 30f;
            foreach (var config in _agentConfigs)
            {
                if (GUI.Button(new Rect(x, curY, w, btnH), config.Name))
                {
                    ActivateAgent(_pickerGiantess, _pickerPlayer, config);
                    _showPicker = false;
                }
                curY += btnH + 5;
            }

            if (GUI.Button(new Rect(x, curY, w, btnH), "Cancel"))
            {
                _showPicker = false;
            }
        }

        // Shared chat log
        internal static List<string> _chatLog = new List<string>();
        internal static float _chatLogTimer;
        internal static int MAX_CHAT_LINES => maxChatLines;
        internal static float CHAT_DISPLAY_TIME => chatDisplayTime;
        internal static float _lastAgentChatTime; // when the last agent posted to chat
        internal static bool _chatLocked; // prevents multiple agents posting in same frame

        // Configurable settings
        internal static int maxWords = 50;
        internal static float chatDisplayTime = 30f;
        internal static int maxChatLines = 50;
        internal static float audioOverlapGap = 1.5f;
        internal static float chatStaggerDelay = 5f;
        internal static int chatFontSize = 18;
        internal static int conversationMemory = 50; // how many entries to remember
        internal static float animCooldown = 30f; // seconds between animation changes
        internal static bool morphsEnabled = true; // toggle AI expression morphs
        internal static bool lipSyncEnabled = true; // toggle lip sync morphs
        internal static float autoTalkInterval = 30f; // seconds between same agent's auto dialogue
        internal static int _audioTicketCounter; // next ticket number
        internal static int _audioNowServing; // which ticket is currently playing

        void Awake()
        {
            Instance = this;
            // The manager used to live on the BepInEx plugin GameObject, which does not
            // reliably survive a scene load. When it was destroyed, Instance became
            // Unity-fake-null and every entry point (F8/F9/Enter, the settings panel)
            // silently did nothing while the Harmony patches kept working, because they
            // are static. Keep it alive explicitly instead.
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            LoadConfig();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Plugin.Log.LogWarning("[AI] Manager was destroyed - AI controls will be unavailable until it is recreated.");
            }
        }

        static bool _everCreated;

        /// <summary>
        /// Returns the live manager, recreating it if it has been destroyed.
        /// Every entry point goes through here so a lost manager heals itself
        /// rather than failing silently.
        /// </summary>
        public static AIGiantess Ensure()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("SizeboxFix_AI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<AIKeybindHandler>();
            var mgr = go.AddComponent<AIGiantess>();

            // Only the second and later calls are recoveries. Saying "recreated" at
            // startup would cry wolf and make this line useless as a fault signal.
            if (_everCreated)
                Plugin.Log.LogWarning("[AI] Manager was missing and has been recreated.");
            else
                Plugin.Log.LogInfo("[AI] Manager ready.");
            _everCreated = true;
            return mgr;
        }

        void LoadConfig()
        {
            AIConfigManager.Load(out _sharedConfig, out _agentConfigs);
            _nextConfigIndex = 0;

            if (string.IsNullOrEmpty(_sharedConfig.ApiKey) || _sharedConfig.ApiKey == "YOUR_KEY_HERE")
            {
                Plugin.Log.LogWarning("[AI] No API key set in SizeboxAI.cfg");
                Plugin.Log.LogInfo("[AI] " + "Set your API key in BepInEx/config/SizeboxAI.cfg");
            }
            else
            {
                Plugin.Log.LogInfo("[AI] Config loaded. Model: " + _sharedConfig.Model +
                    " | " + _agentConfigs.Count + " agent preset(s)");
                foreach (var a in _agentConfigs)
                    Plugin.Log.LogInfo("[AI]   Preset: " + a.Name);
            }
        }

        /// <summary>
        /// Activate AI on a giantess with a specific config.
        /// </summary>
        public GiantessAgent ActivateAgent(EntityBase giantess, EntityBase player, AgentConfig config)
        {
            int id = giantess.GetInstanceID();

            // Already active? Deactivate first
            if (_activeAgents.ContainsKey(id))
            {
                var existing = _activeAgents[id];
                existing.Shutdown();
                Destroy(existing);
                _activeAgents.Remove(id);
            }

            if (config == null) return null;

            // Auto-load saved conversation on first agent activation
            if (_activeAgents.Count == 0)
                LoadConversation();

            var agent = giantess.gameObject.AddComponent<GiantessAgent>();
            agent.Initialize(giantess, player, _sharedConfig, config);
            _activeAgents[id] = agent;

            return agent;
        }

        // Personality picker state
        internal bool _showPicker;
        internal EntityBase _pickerGiantess;
        internal EntityBase _pickerPlayer;

        /// <summary>
        /// Deactivate AI on a giantess.
        /// </summary>
        public void DeactivateAgent(EntityBase giantess)
        {
            int id = giantess.GetInstanceID();
            GiantessAgent agent;
            if (_activeAgents.TryGetValue(id, out agent))
            {
                agent.Shutdown();
                Destroy(agent);
                _activeAgents.Remove(id);
            }
        }

        /// <summary>
        /// Called by GiantessAgent.OnDestroy to clean up registry.
        /// </summary>
        public void UnregisterAgent(GiantessAgent agent)
        {
            if (agent._giantess == null) return;
            int id = agent._giantess.GetInstanceID();
            if (_activeAgents.ContainsKey(id) && _activeAgents[id] == agent)
                _activeAgents.Remove(id);
        }

        /// <summary>
        /// Get the agent for an entity, or null.
        /// </summary>
        public GiantessAgent GetAgent(EntityBase entity)
        {
            if (entity == null) return null;
            GiantessAgent agent;
            _activeAgents.TryGetValue(entity.GetInstanceID(), out agent);
            return agent;
        }

        /// <summary>
        /// Get all active agents.
        /// </summary>
        public List<GiantessAgent> GetAllAgents()
        {
            return new List<GiantessAgent>(_activeAgents.Values);
        }

        /// <summary>
        /// Is any agent active?
        /// </summary>
        public bool HasActiveAgents => _activeAgents.Count > 0;

        /// <summary>
        /// Broadcast a player message to all active agents.
        /// </summary>
        public void BroadcastPlayerMessage(string msg)
        {
            AddChatLine("You: " + msg);
            lock (_convLock)
            {
                _sharedConversation.Add("{\"role\":\"user\",\"content\":" + JsonEscape("The tiny person says: " + msg) + "}");
            }

            // Check if player addressed a specific agent by name
            string msgLower = msg.ToLower();
            GiantessAgent targeted = null;
            foreach (var agent in _activeAgents.Values)
            {
                if (msgLower.Contains(agent.AgentName.ToLower()))
                {
                    targeted = agent;
                    break;
                }
            }

            if (targeted != null)
            {
                // Only the named agent responds
                Plugin.Log.LogInfo("[AI] Message targeted at: " + targeted.AgentName);
                targeted.OnPlayerMessage(msg);
            }
            else
            {
                // No name mentioned — all agents respond
                Plugin.Log.LogInfo("[AI] Message to ALL agents (no name found in: " + msg + ")");
                foreach (var agent in _activeAgents.Values)
                    agent.OnPlayerMessage(msg);
            }
        }

        // Backward compat: these are used by the settings panel
        // Get the first agent's config for settings panel (legacy single-agent)
        internal bool IsActive => _activeAgents.Count > 0;

        public void StopAll()
        {
            var agents = new List<GiantessAgent>(_activeAgents.Values);
            foreach (var agent in agents)
            {
                agent.Shutdown();
                Destroy(agent);
            }
            _activeAgents.Clear();
        }

        // Legacy compat for settings panel — operate on first config
        internal string _apiKey { get { return _sharedConfig.ApiKey; } set { _sharedConfig.ApiKey = value; } }
        internal string _apiUrl { get { return _sharedConfig.ApiUrl; } set { _sharedConfig.ApiUrl = value; } }
        internal string _model { get { return _sharedConfig.Model; } set { _sharedConfig.Model = value; } }
        internal float _decisionInterval { get { return _sharedConfig.DecisionInterval; } set { _sharedConfig.DecisionInterval = value; } }

        // First agent config accessors for settings panel
        AgentConfig FirstConfig => _agentConfigs.Count > 0 ? _agentConfigs[0] : null;
        internal string _personality { get { return FirstConfig?.Personality ?? ""; } set { if (FirstConfig != null) FirstConfig.Personality = value; } }
        internal string _ttsProvider { get { return FirstConfig?.TtsProvider ?? "edge"; } set { if (FirstConfig != null) FirstConfig.TtsProvider = value; } }
        internal bool _ttsEnabled { get { return FirstConfig?.TtsEnabled ?? false; } set { if (FirstConfig != null) FirstConfig.TtsEnabled = value; } }
        internal string _ttsEdgeVoice { get { return FirstConfig?.TtsEdgeVoice ?? ""; } set { if (FirstConfig != null) FirstConfig.TtsEdgeVoice = value; } }
        internal string _ttsFishApiKey { get { return FirstConfig?.TtsFishApiKey ?? ""; } set { if (FirstConfig != null) FirstConfig.TtsFishApiKey = value; } }
        internal string _ttsFishModelId { get { return FirstConfig?.TtsFishModelId ?? ""; } set { if (FirstConfig != null) FirstConfig.TtsFishModelId = value; } }
        internal string _ttsApiKey { get { return FirstConfig?.TtsApiKey ?? ""; } set { if (FirstConfig != null) FirstConfig.TtsApiKey = value; } }
        internal string _ttsVoiceId { get { return FirstConfig?.TtsVoiceId ?? ""; } set { if (FirstConfig != null) FirstConfig.TtsVoiceId = value; } }

        internal void SaveConfig()
        {
            AIConfigManager.Save(_sharedConfig, _agentConfigs);
            Plugin.Log.LogInfo("[AI] Config saved");
        }

        /// <summary>
        /// Called by agents to record their response in shared conversation.
        /// </summary>
        internal static void AddAgentResponse(string agentName, string action, string dialogue)
        {
            lock (_convLock)
            {
                string content = "[" + agentName + "] ACTION: " + action + " SAY: " + dialogue;
                _sharedConversation.Add("{\"role\":\"assistant\",\"content\":" + JsonEscape(content) + "}");
            }
        }

        /// <summary>
        /// Get a snapshot of the shared conversation for prompt building.
        /// </summary>
        internal static List<string> GetSharedConversation(int maxEntries = -1)
        {
            if (maxEntries < 0) maxEntries = conversationMemory;
            lock (_convLock)
            {
                int start = Math.Max(0, _sharedConversation.Count - maxEntries);
                var result = new List<string>();
                for (int i = start; i < _sharedConversation.Count; i++)
                    result.Add(_sharedConversation[i]);
                return result;
            }
        }

        internal static void AddChatLine(string line)
        {
            _chatLog.Add(line);
            if (_chatLog.Count > MAX_CHAT_LINES)
                _chatLog.RemoveAt(0);
            _chatLogTimer = Time.time + CHAT_DISPLAY_TIME;
        }

        // ===================== CONVERSATION SAVE/LOAD =====================

        internal static string ConversationSavePath => System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "SizeboxAI_conversation.json");

        internal static void SaveConversation()
        {
            try
            {
                lock (_convLock)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("[");
                    for (int i = 0; i < _sharedConversation.Count; i++)
                    {
                        sb.Append("  " + _sharedConversation[i]);
                        if (i < _sharedConversation.Count - 1) sb.Append(",");
                        sb.AppendLine();
                    }
                    sb.AppendLine("]");
                    System.IO.File.WriteAllText(ConversationSavePath, sb.ToString());
                }
                // Also save chat log
                string chatPath = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "SizeboxAI_chatlog.txt");
                System.IO.File.WriteAllLines(chatPath, _chatLog.ToArray());

                Plugin.Log.LogInfo("[AI] Conversation saved (" + _sharedConversation.Count + " entries)");
                AIGiantess.AddChatLine("Conversation saved!");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[AI] Save conversation failed: " + ex.Message);
                Plugin.Log.LogInfo("[AI] " + "Save failed: " + ex.Message);
            }
        }

        internal static void LoadConversation()
        {
            try
            {
                if (!System.IO.File.Exists(ConversationSavePath)) return;

                string json = System.IO.File.ReadAllText(ConversationSavePath);
                // Simple parse — each line between [ ] that starts with { is an entry
                lock (_convLock)
                {
                    _sharedConversation.Clear();
                    foreach (var line in json.Split('\n'))
                    {
                        var trimmed = line.Trim().TrimEnd(',');
                        if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                            _sharedConversation.Add(trimmed);
                    }
                }

                // Load chat log
                string chatPath = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "SizeboxAI_chatlog.txt");
                if (System.IO.File.Exists(chatPath))
                {
                    _chatLog.Clear();
                    foreach (var line in System.IO.File.ReadAllLines(chatPath))
                    {
                        if (!string.IsNullOrEmpty(line))
                            _chatLog.Add(line);
                    }
                    _chatLogTimer = Time.time + CHAT_DISPLAY_TIME;
                }

                Plugin.Log.LogInfo("[AI] Conversation loaded (" + _sharedConversation.Count + " entries)");
                AddChatLine("Conversation loaded! (" + _sharedConversation.Count + " entries)");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[AI] Load conversation failed: " + ex.Message);
            }
        }

        static string JsonEscape(string s)
        {
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
        }
    }

    /// <summary>
    /// Keybind handler: F8 = toggle AI, F9/T = chat, F10 = TTS test, F11 = save conversation, L = lock anim
    /// </summary>
    public class AIKeybindHandler : MonoBehaviour
    {
        private bool _chatOpen;
        private bool _chatJustOpened;
        private bool _ttsInputMode;
        private string _chatText = "";
        private GUIStyle _boxStyle;
        private GUIStyle _textStyle;
        private Vector2 _chatScrollPos;
        private int _lastChatCount; // track when new messages arrive

        void Update()
        {
            // F8 toggles AI on selected giantess
            if (Input.GetKeyDown(AIGiantess.Keys.KeyToggleAI))
            {
                var mgr = AIGiantess.Ensure();
                if (mgr == null)
                {
                    Plugin.Log.LogError("[AI] Manager unavailable - cannot toggle AI.");
                    return;
                }

                var selected = InterfaceControl.instance?.selectedEntity;
                if (selected == null || !selected.isGiantess)
                {
                    Plugin.Log.LogInfo("[AI] " + "Select a giantess first, then press F8");
                    return;
                }

                var player = GameController.LocalClient?.Player?.Entity;
                if (player == null)
                {
                    Plugin.Log.LogInfo("[AI] " + "No player found — spawn as micro first");
                    return;
                }

                // If already active, deactivate. Otherwise show personality picker.
                var existingAgent = mgr.GetAgent(selected);
                if (existingAgent != null)
                {
                    mgr.DeactivateAgent(selected);
                }
                else
                {
                    // Show picker
                    mgr._showPicker = true;
                    mgr._pickerGiantess = selected;
                    mgr._pickerPlayer = player;
                }
            }

            // Backslash mutes/unmutes selected giantess AI
            if (Input.GetKeyDown(AIGiantess.Keys.KeyMute) && !_chatOpen && AIGiantess.Instance != null)
            {
                var selected = InterfaceControl.instance?.selectedEntity;
                if (selected != null)
                {
                    var agent = AIGiantess.Instance.GetAgent(selected);
                    if (agent != null)
                    {
                        agent._muted = !agent._muted;
                        string state = agent._muted ? "MUTED" : "UNMUTED";
                        AIGiantess.AddChatLine(agent.AgentName + " " + state);
                    }
                }
            }

            // Shift + the save key clears conversation history instead
            if (Input.GetKeyDown(AIGiantess.Keys.KeySaveConversation)
                && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                && !_chatOpen && AIGiantess.Instance != null)
            {
                lock (AIGiantess._convLock)
                {
                    AIGiantess._sharedConversation.Clear();
                }
                AIGiantess._chatLog.Clear();
                // Also delete the save files so they don't reload
                try
                {
                    if (System.IO.File.Exists(AIGiantess.ConversationSavePath))
                        System.IO.File.Delete(AIGiantess.ConversationSavePath);
                    string chatPath = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "SizeboxAI_chatlog.txt");
                    if (System.IO.File.Exists(chatPath))
                        System.IO.File.Delete(chatPath);
                }
                catch { }
                AIGiantess.AddChatLine("Conversation cleared!");
            }

            // chat key (or its alternate) opens chat
            if ((Input.GetKeyDown(AIGiantess.Keys.KeyChatAlt) || Input.GetKeyDown(AIGiantess.Keys.KeyChat)) && !_chatOpen
                && AIGiantess.Instance != null && AIGiantess.Instance.HasActiveAgents)
            {
                _chatOpen = true;
                _chatJustOpened = true;
                _ttsInputMode = false;
                _chatText = "";
                if (InputManager.inputs != null)
                    InputManager.inputs.Disable();
            }

            // save key on its own saves the conversation
            if (Input.GetKeyDown(AIGiantess.Keys.KeySaveConversation)
                && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)
                && !_chatOpen && AIGiantess.Instance != null)
            {
                AIGiantess.SaveConversation();
            }

            // L toggles animation lock on selected giantess's agent
            if (Input.GetKeyDown(AIGiantess.Keys.KeyLockAnim) && !_chatOpen && AIGiantess.Instance != null)
            {
                var selected = InterfaceControl.instance?.selectedEntity;
                if (selected != null)
                {
                    var agent = AIGiantess.Instance.GetAgent(selected);
                    if (agent != null)
                    {
                        agent._animLocked = !agent._animLocked;
                        string state = agent._animLocked ? "LOCKED" : "UNLOCKED";
                        AIGiantess.AddChatLine(agent.AgentName + " animation " + state);
                    }
                }
            }

            // F10 toggles TTS input for selected giantess
            if (Input.GetKeyDown(AIGiantess.Keys.KeyTTSInput) && !_chatOpen && AIGiantess.Instance != null && AIGiantess.Instance.HasActiveAgents)
            {
                _chatOpen = true;
                _chatJustOpened = true;
                _ttsInputMode = true;
                _chatText = "";
                if (InputManager.inputs != null)
                    InputManager.inputs.Disable();
            }

            // Handle Enter/Escape while chat is open
            if (_chatOpen)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (!string.IsNullOrEmpty(_chatText))
                    {
                        if (_ttsInputMode)
                        {
                            // F10 mode — speak with selected giantess's agent
                            var selected = InterfaceControl.instance?.selectedEntity;
                            var agent = selected != null ? AIGiantess.Instance.GetAgent(selected) : null;
                            if (agent == null)
                            {
                                // Fall back to first active agent
                                var all = AIGiantess.Instance.GetAllAgents();
                                if (all.Count > 0) agent = all[0];
                            }
                            if (agent != null)
                                agent.SpeakManual(_chatText);
                        }
                        else
                        {
                            // Chat mode — broadcast to all agents
                            AIGiantess.Instance.BroadcastPlayerMessage(_chatText);
                        }
                    }
                    CloseChat();
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                    CloseChat();
            }
        }

        void CloseChat()
        {
            _chatOpen = false;
            _chatText = "";
            _chatJustOpened = false;
            if (InputManager.inputs != null)
                InputManager.inputs.Enable();
        }

        // Chat log + input styles
        GUIStyle _chatLogStyle;
        GUIStyle _chatLogBgStyle;

        void OnGUI()
        {
            if (AIGiantess._chatLog.Count == 0 && !_chatOpen) return;
            if (AIGiantess._chatLog.Count > 0 && (Time.time < AIGiantess._chatLogTimer || _chatOpen))
            {
                if (_chatLogStyle == null)
                {
                    _chatLogStyle = new GUIStyle(GUI.skin.label);
                    _chatLogStyle.fontSize = AIGiantess.chatFontSize;
                    _chatLogStyle.wordWrap = true;
                    _chatLogStyle.richText = true;
                    _chatLogStyle.normal.textColor = Color.white;
                }
                _chatLogStyle.fontSize = AIGiantess.chatFontSize; // live update
                if (_chatLogBgStyle == null)
                {
                    _chatLogBgStyle = new GUIStyle(GUI.skin.box);
                }
                if (_textStyle == null)
                {
                    _textStyle = new GUIStyle(GUI.skin.textField);
                    _textStyle.fontSize = 18;
                    _textStyle.normal.textColor = Color.white;
                }

                float panelW = Screen.width * 0.45f;
                float lineH = 50f;
                float inputH = 45f;
                float maxVisibleH = 350f; // fixed height for scroll area
                float contentH = AIGiantess._chatLog.Count * lineH;
                float panelH = Mathf.Min(contentH, maxVisibleH) + (_chatOpen ? inputH + 15f : 0f) + 15f;
                float panelX = Screen.width - panelW - 15f;
                float panelY = 15f;

                Color old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0, 0, 0, 0.75f);
                GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", _chatLogBgStyle);
                GUI.backgroundColor = old;

                // Scrollable chat area
                Rect scrollViewRect = new Rect(panelX, panelY + 5, panelW, Mathf.Min(contentH, maxVisibleH));
                Rect scrollContentRect = new Rect(0, 0, panelW - 30, contentH);

                // Auto-scroll to bottom only when new messages arrive
                if (AIGiantess._chatLog.Count != _lastChatCount)
                {
                    _lastChatCount = AIGiantess._chatLog.Count;
                    if (contentH > maxVisibleH)
                        _chatScrollPos.y = contentH - maxVisibleH;
                }

                _chatScrollPos = GUI.BeginScrollView(scrollViewRect, _chatScrollPos, scrollContentRect, false, true);
                for (int i = 0; i < AIGiantess._chatLog.Count; i++)
                {
                    string line = AIGiantess._chatLog[i];
                    GUI.Label(new Rect(5, i * lineH, panelW - 40, lineH), line, _chatLogStyle);
                }
                GUI.EndScrollView();

                if (_chatOpen)
                {
                    string controlName = "_aiChat";
                    GUI.SetNextControlName(controlName);

                    if (_chatJustOpened)
                    {
                        _chatJustOpened = false;
                    }

                    float inputY = panelY + panelH - inputH - 8f;

                    _chatText = GUI.TextField(new Rect(panelX + 10, inputY, panelW - 20, inputH), _chatText, 200, _textStyle);
                    GUI.FocusControl(controlName);

                    if (Event.current.isKey)
                    {
                        if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                        {
                            if (!string.IsNullOrEmpty(_chatText))
                            {
                                if (_ttsInputMode)
                                {
                                    var selected = InterfaceControl.instance?.selectedEntity;
                                    var agent = selected != null ? AIGiantess.Instance?.GetAgent(selected) : null;
                                    if (agent == null)
                                    {
                                        var all = AIGiantess.Instance?.GetAllAgents();
                                        if (all != null && all.Count > 0) agent = all[0];
                                    }
                                    if (agent != null)
                                        agent.SpeakManual(_chatText);
                                }
                                else
                                {
                                    AIGiantess.Instance?.BroadcastPlayerMessage(_chatText);
                                }
                            }
                            CloseChat();
                            Event.current.Use();
                        }
                        if (Event.current.keyCode == KeyCode.Escape)
                        {
                            CloseChat();
                            Event.current.Use();
                        }
                    }
                }
            }

            // Show hint when chat is empty but AI is active
            if (_chatOpen && AIGiantess._chatLog.Count == 0)
            {
                float hintW = 300f;
                float hintH = 30f;
                float hintX = Screen.width - hintW - 30f;
                float hintY = 55f;
                GUI.Label(new Rect(hintX, hintY, hintW, hintH),
                    _ttsInputMode ? "Type what she should say..." : "Type a message...",
                    _chatLogStyle ?? GUI.skin.label);
            }
        }
    }
}

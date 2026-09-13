using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SizeboxFix
{
    /// <summary>
    /// Per-giantess AI agent. Attached as a component to each AI-controlled giantess.
    /// Handles LLM decisions, TTS, lip sync, animation, and movement for one entity.
    /// </summary>
    public class GiantessAgent : MonoBehaviour
    {
        // Config references
        internal SharedConfig _shared;
        internal AgentConfig _config;

        // Entity references
        internal EntityBase _giantess;
        internal EntityBase _player;

        // Decision state
        float _nextDecisionTime;
        float _waitingStartTime; // safety timeout for stuck requests
        string _pendingAction;
        string _pendingDialogue;
        string _pendingMorphs;
        string _pendingAnim;
        bool _waiting;
        List<string> _conversationHistory = new List<string>();
        string _lastAction = "idle";
        string _currentMood = "playful";
        List<string> _recentActions = new List<string>();
        List<string> _recentDialogue = new List<string>();
        Coroutine _moveCoroutine;
        Coroutine _rotateCoroutine;
        bool _inConversation;
        float _conversationTimeout;
        float _lastAnimTime;
        internal bool _animLocked;
        internal bool _muted;
        float _chatStagger; // random extra delay per agent to prevent simultaneous posting // M key — stops all actions, speech, and movement
        string _cachedMorphNames;
        string _cachedAnimNames;
        List<int> _lastAIMorphIndices; // track which morphs AI set, to reset next time
        string _playerMessage;

        // TTS state
        internal AudioSource _ttsAudioSource;
        AudioClip _pendingClip; // clip ready to play, waiting for turn
        int _ttsFailCount;
        string _pendingAudioPath;

        // Lip sync
        EntityMorphData[] _visemeMorphs;
        bool _lipSyncActive;
        float[] _sampleBuffer = new float[256];
        float _lipSyncSmoothed;
        int _visemeCycle;

        // Constants
        const float ANIM_COOLDOWN = 5f;
        const float AUTO_INTERVAL = 30f;
        const float CONVERSATION_TIMEOUT = 60f;

        // Available actions
        static readonly string[] ACTIONS = {
            "walk_to_player", "crouch_look", "wander",
            "stomp_near", "sit_down", "grab_player", "taunt",
            "look_at_player", "walk_away", "dance",
            "stuff_in_panties", "buttcrush",
            "pet_player", "poke_player", "laugh", "wave"
        };

        static readonly Dictionary<string, string> ACTION_ANIMS = new Dictionary<string, string>
        {
            {"walk_to_player", "Female Walk"},
            {"crouch_look", "Crouch Idle"},
            {"stand_idle", "Idle 2"},
            {"wander", "Walking 2"},
            {"stomp_near", "Stomping"},
            {"sit_down", "Sit 6"},
            {"grab_player", "Acknowledging"},
            {"taunt", "Taunt 3"},
            {"look_at_player", "Look Down"},
            {"walk_away", "Walking"},
            {"dance", "Excited"},
            {"crouch_idle", "Crouch Idle"},
            {"stuff_in_panties", "Acknowledging"},
            {"buttcrush", "Sit 6"},
            {"pet_player", "Greet"},
            {"poke_player", "Acknowledging"},
            {"laugh", "Laughing"},
            {"wave", "Waving 2"}
        };

        public bool IsActive => _giantess != null && _player != null;
        public string AgentName => _config != null ? _config.Name : "Unknown";

        /// <summary>
        /// Initialize this agent on a giantess entity.
        /// </summary>
        public void Initialize(EntityBase giantess, EntityBase player, SharedConfig shared, AgentConfig config)
        {
            _giantess = giantess;
            _player = player;
            _shared = shared;
            _config = config;
            _nextDecisionTime = Time.time + 1f;
            _conversationHistory.Clear();
            _lastAction = "idle";
            _cachedMorphNames = null;
            _ttsFailCount = 0;
            _animLocked = true; // Lock animations by default
            _chatStagger = UnityEngine.Random.Range(0f, 3f); // Random offset so agents don't all post at same time
            AIGiantess._lastAgentChatTime = Time.time; // Prevent initial burst

            CacheMorphs();
            CacheAnimations();

            Plugin.Log.LogInfo("[AI:" + AgentName + "] Started for " + giantess.name + " targeting " + player.name);
            AIGiantess.AddChatLine("AI activated: " + AgentName);
        }

        public void Shutdown()
        {
            Plugin.Log.LogInfo("[AI:" + AgentName + "] Stopped");
            AIGiantess.AddChatLine("AI deactivated: " + AgentName);
            _giantess = null;
            _player = null;
        }

        void OnDestroy()
        {
            // Unregister from manager when entity is destroyed
            if (AIGiantess.Instance != null)
                AIGiantess.Instance.UnregisterAgent(this);
        }

        public void SpeakManual(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            AIGiantess.AddChatLine(AgentName + ": " + text);
            SpeakTTS(text);
        }

        /// <summary>
        /// Called by manager when player sends a message. Triggers immediate response.
        /// </summary>
        public void OnPlayerMessage(string msg)
        {
            _playerMessage = msg;
            // Stagger responses so agents don't all fire at once
            float stagger = UnityEngine.Random.Range(1f, 8f);
            _nextDecisionTime = Time.time + stagger;
            _waiting = false;
            _inConversation = true;
            _conversationTimeout = Time.time + CONVERSATION_TIMEOUT;
        }

        void Update()
        {
            if (!IsActive) return;
            if (_giantess == null || _player == null) { Shutdown(); return; }
            if (_muted) return; // N key — completely silent
            if (string.IsNullOrEmpty(_shared.ApiKey) || _shared.ApiKey == "YOUR_KEY_HERE") return;

            PlayPendingAudio();
            UpdateLipSync();

            // Play queued audio clip — wait until other agent has <1.5s left
            if (_pendingClip != null && _ttsAudioSource != null)
            {
                bool mustWait = false;
                var allAgents = AIGiantess.Instance?.GetAllAgents();
                if (allAgents != null)
                {
                    foreach (var other in allAgents)
                    {
                        if (other != this && other._ttsAudioSource != null && other._ttsAudioSource.isPlaying)
                        {
                            // Check how much time is left on their clip
                            float remaining = other._ttsAudioSource.clip.length - other._ttsAudioSource.time;
                            if (remaining > AIGiantess.audioOverlapGap)
                            {
                                mustWait = true;
                                break;
                            }
                        }
                    }
                }
                if (!mustWait)
                {
                    _ttsAudioSource.clip = _pendingClip;
                    _ttsAudioSource.Play();
                    _pendingClip = null;
                    AIGiantess._isSpeaking = true;
                    AIGiantess._currentSpeaker = this;
                    if (_visemeMorphs == null) FindVisemeMorphs();
                    if (_visemeMorphs != null && AIGiantess.lipSyncEnabled) _lipSyncActive = true;
                }
            }

            // Clear speaking flag and advance ticket when OUR audio finishes
            if (AIGiantess._isSpeaking && AIGiantess._currentSpeaker == this
                && _ttsAudioSource != null && !_ttsAudioSource.isPlaying && !_lipSyncActive && _pendingAudioPath == null)
            {
                AIGiantess._isSpeaking = false;
                AIGiantess._currentSpeaker = null;
                AIGiantess._audioNowServing++; // next agent's turn
            }

            if (_pendingAction != null)
            {
                // Skip empty responses
                if (string.IsNullOrEmpty(_pendingDialogue))
                {
                    _pendingAction = null;
                    _pendingDialogue = null;
                    _pendingMorphs = null;
                    _pendingAnim = null;
                    _nextDecisionTime = Time.time + 5f;
                }
                // Wait until it's our turn — check if enough time passed since last agent
                else if (AIGiantess._chatLocked || Time.time < AIGiantess._lastAgentChatTime + AIGiantess.chatStaggerDelay)
                {
                    // Another agent is talking or delay hasn't passed — wait
                }
                else
                {
                // Lock so no other agent can post this frame
                AIGiantess._chatLocked = true;
                AIGiantess._lastAgentChatTime = Time.time;
                ExecuteAction(_pendingAction, _pendingDialogue, _pendingAnim);
                AIGiantess._chatLocked = false;
                if (!string.IsNullOrEmpty(_pendingMorphs))
                    ApplyMorphs(_pendingMorphs);
                SpeakTTS(_pendingDialogue);
                _pendingAction = null;
                _pendingDialogue = null;
                _pendingMorphs = null;
                _pendingAnim = null;
                }
            }

            if (_inConversation && Time.time > _conversationTimeout)
            {
                _inConversation = false;
                Plugin.Log.LogInfo("[AI:" + AgentName + "] Conversation timeout — back to auto mode");
            }

            // Safety: unstick if waiting for more than 30 seconds
            if (_waiting && Time.time > _waitingStartTime + 30f)
            {
                Plugin.Log.LogWarning("[AI:" + AgentName + "] Request timed out — unsticking");
                _waiting = false;
            }

            if (!_waiting && Time.time >= _nextDecisionTime)
            {
                if (_inConversation)
                {
                    if (!string.IsNullOrEmpty(_playerMessage))
                    {
                        _nextDecisionTime = Time.time + _shared.DecisionInterval;
                        RequestDecision();
                    }
                }
                else
                {
                    _nextDecisionTime = Time.time + AIGiantess.autoTalkInterval;
                    RequestDecision();
                }
            }

            if (_lastAction == "stuff_in_panties" && _player != null && _giantess != null)
            {
                var hips = _giantess.GetComponent<Animator>()?.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                {
                    float scale = _giantess.Scale;
                    var pos = hips.position - _giantess.transform.forward * 0.03f * scale;
                    pos.y -= 0.02f * scale;
                    _player.transform.position = pos;
                }
            }
        }

        // ===================== CACHING =====================

        void CacheMorphs()
        {
            var morphs = _giantess.Morphs;
            if (morphs == null || morphs.Count == 0) return;

            var names = new List<string>();
            string[] safeKeywords = { "Smile", "Blush", "Breathe",
                "Aroused", "Frown", "Squint", "Squinch",
                "Grit", "CheekPuff", "OpenEye",
                "Serious", "Angry", "Troubled", "Grin", "Devious", "Sad",
                "Stare" };
            string[] blockedKeywords = { "Mouth", "Lip", "Tongue", "Pucker", "Suck",
                "Vagina", "NoShirt", "NoPants", "NoShoes", "Blink", "Throat",
                "pussy", "THICC", "bulge", "tit_up", "ass ",
                "Brow", "眉", "Eye" };

            for (int i = 0; i < morphs.Count && names.Count < 15; i++)
            {
                string name = morphs[i].Name;
                bool safe = false, blocked = false;
                foreach (var kw in safeKeywords)
                    if (name.Contains(kw)) { safe = true; break; }
                foreach (var kw in blockedKeywords)
                    if (name.Contains(kw)) { blocked = true; break; }
                if (safe && !blocked)
                    names.Add(name);
            }
            if (names.Count > 0)
                _cachedMorphNames = string.Join(", ", names.ToArray());

            var allNames = new List<string>();
            for (int j = 0; j < morphs.Count; j++)
                allNames.Add(j + "." + morphs[j].Name);
            Plugin.Log.LogInfo("[AI:" + AgentName + "] All morphs: " + string.Join(", ", allNames.ToArray()));
            Plugin.Log.LogInfo("[AI:" + AgentName + "] Found " + names.Count + " morphs for AI control");
        }

        void CacheAnimations()
        {
            _cachedAnimNames = null;
            if (IOManager.Instance == null || IOManager.Instance.AnimationControllers == null) return;

            var allAnims = IOManager.Instance.AnimationControllers;
            var defaultController = IOManager.Instance.gtsAnimatorController;
            Plugin.Log.LogInfo("[AI:" + AgentName + "] Found " + allAnims.Count + " animations total");

            var defaultAnims = new List<string>();
            foreach (var kvp in allAnims)
            {
                if (kvp.Value == defaultController)
                    defaultAnims.Add(kvp.Key);
            }
            defaultAnims.Sort();

            if (defaultAnims.Count > 0)
            {
                _cachedAnimNames = string.Join(", ", defaultAnims.ToArray());
                Plugin.Log.LogInfo("[AI:" + AgentName + "] Using " + defaultAnims.Count + " default animations for AI");
            }
        }

        // ===================== PROMPT / LLM =====================

        string BuildGameState()
        {
            float dist = Vector3.Distance(_giantess.transform.position, _player.transform.position);
            float gtsHeight = _giantess.Height;
            float playerHeight = _player.MeshHeight;
            float sizeRatio = gtsHeight / Mathf.Max(playerHeight, 0.01f);
            string playerRelative = dist < gtsHeight * 0.1f ? "right at your feet" :
                                   dist < gtsHeight * 0.5f ? "nearby" :
                                   dist < gtsHeight * 2f ? "a bit away" : "far away";

            // Locate the player relative to the character's body.
            Vector3 toPlayer = _player.transform.position - _giantess.transform.position;
            float dotForward = Vector3.Dot(_giantess.transform.forward, toPlayer.normalized);
            string bodyRelative = "somewhere nearby";
            if (dist < gtsHeight * 0.1f)
            {
                if (toPlayer.y < -gtsHeight * 0.3f) bodyRelative = "under your feet";
                else if (toPlayer.y > gtsHeight * 0.5f) bodyRelative = "near your face";
                else if (dotForward > 0.5f) bodyRelative = "in front of you";
                else if (dotForward < -0.5f) bodyRelative = "behind you, near your butt";
                else bodyRelative = "beside you";
            }

            var sb = new StringBuilder();
            sb.AppendLine("GAME STATE:");
            sb.AppendLine("- You are " + sizeRatio.ToString("F0") + "x taller than the tiny person");
            sb.AppendLine("- The tiny person is " + playerRelative + " (" + bodyRelative + ")");
            sb.AppendLine("- Your current mood: " + _currentMood);

            // Tell about sisters' locations
            var allAgents = AIGiantess.Instance?.GetAllAgents();
            if (allAgents != null && allAgents.Count > 1)
            {
                foreach (var other in allAgents)
                {
                    if (other == this || other._giantess == null) continue;
                    float sisterDist = Vector3.Distance(_giantess.transform.position, other._giantess.transform.position);
                    string sisterPos = sisterDist < gtsHeight * 0.5f ? "right next to you" :
                                      sisterDist < gtsHeight * 2f ? "nearby" : "far away";
                    sb.AppendLine("- Your sister " + other.AgentName + " is " + sisterPos);
                }
            }
            if (_recentActions.Count > 0)
                sb.AppendLine("- Your recent actions (DO NOT REPEAT): " + string.Join(", ", _recentActions.ToArray()));
            if (_recentDialogue.Count > 0)
                sb.AppendLine("- Your recent lines (DO NOT REPEAT OR PARAPHRASE THESE): " + string.Join(" | ", _recentDialogue.ToArray()));
            if (_lastAction == "stuff_in_panties")
                sb.AppendLine("- The tiny person is currently stuffed in your panties");

            if (!string.IsNullOrEmpty(_playerMessage))
            {
                sb.AppendLine("- THE TINY PERSON SAYS TO YOU: \"" + _playerMessage + "\"");
                sb.AppendLine("- You MUST respond to what they said. React in character.");
                _playerMessage = null;
            }

            return sb.ToString();
        }

        string BuildPrompt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are " + AgentName + ", controlling a giantess in a game. " + _config.Personality);

            // Tell the agent about other active giantesses
            var allAgents = AIGiantess.Instance?.GetAllAgents();
            if (allAgents != null && allAgents.Count > 1)
            {
                var others = new List<string>();
                foreach (var a in allAgents)
                    if (a != this) others.Add(a.AgentName);
                sb.AppendLine("Other giantesses present: " + string.Join(", ", others.ToArray()) +
                    ". You can talk to them or about them. Address them by name.");
            }
            sb.AppendLine();
            sb.AppendLine(BuildGameState());
            sb.AppendLine();
            sb.AppendLine("Choose ONE action and write a short line of dialogue (what you say/think).");
            sb.AppendLine("Available actions: " + string.Join(", ", ACTIONS));
            if (_animLocked)
            {
                sb.AppendLine("Your animation is LOCKED. Do NOT include an ANIM: line. Focus only on dialogue.");
            }
            else if (!string.IsNullOrEmpty(_cachedAnimNames))
            {
                sb.AppendLine();
                sb.AppendLine("You can play a specific animation with an ANIM: line. Some examples: " + _cachedAnimNames);
            }
            sb.AppendLine();
            if (!string.IsNullOrEmpty(_cachedMorphNames))
            {
                sb.AppendLine("You can also control facial expressions and body morphs.");
                sb.AppendLine("Available morphs: " + _cachedMorphNames);
                sb.AppendLine("Set morphs as comma-separated Name=Value pairs (0.0 to 1.0).");
                sb.AppendLine();
                sb.AppendLine("Respond in EXACTLY this format:");
                sb.AppendLine("ACTION: action_name");
                sb.AppendLine("SAY: your dialogue here");
                sb.AppendLine("MORPH: MorphName=0.5, AnotherMorph=0.8");
                sb.AppendLine("ANIM: AnimationName");
            }
            else
            {
                sb.AppendLine("Respond in EXACTLY this format:");
                sb.AppendLine("ACTION: action_name");
                sb.AppendLine("SAY: your dialogue here");
                sb.AppendLine("ANIM: AnimationName");
            }
            sb.AppendLine();
            sb.AppendLine("Be creative, vary your actions, and stay in character. Write 1-3 sentences, MAXIMUM " + AIGiantess.maxWords + " words total. Be concise. MORPH and ANIM lines are optional.");

            return sb.ToString();
        }

        void RequestDecision()
        {
            _waiting = true;
            _waitingStartTime = Time.time;
            string prompt = BuildPrompt();

            var messages = new List<string>();
            string sysPrompt = "You are " + AgentName + ". " + _config.Personality +
                "\n\nRespond as your configured character in the game." +
                "\n- Respond directly to the player's latest message." +
                "\n- Messages tagged with another character's name are from that character, not you." +
                "\n- Use only the available actions, animations, and morphs in the supplied game context." +
                "\n- Keep dialogue concise and avoid repeating your previous response.";
            messages.Add("{\"role\":\"system\",\"content\":" + JsonEscape(sysPrompt) + "}");

            // Use shared conversation history so agents see each other's responses
            var sharedHistory = AIGiantess.GetSharedConversation(12);
            foreach (var entry in sharedHistory)
                messages.Add(entry);

            messages.Add("{\"role\":\"user\",\"content\":" + JsonEscape(prompt) + "}");

            string messagesJson = "[" + string.Join(",", messages.ToArray()) + "]";
            string body = "{\"model\":" + JsonEscape(_shared.Model) + ",\"messages\":" + messagesJson + ",\"max_tokens\":300,\"temperature\":0.9}";

            string apiUrl = _shared.ApiUrl;
            string apiKey = _shared.ApiKey;
            string agentName = AgentName;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(apiUrl);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Headers.Add("Authorization", "Bearer " + apiKey);
                    request.Headers.Add("HTTP-Referer", "https://github.com/sizebox-mod");
                    request.Headers.Add("X-Title", "Sizebox AI");
                    request.Timeout = 60000;

                    byte[] data = Encoding.UTF8.GetBytes(body);
                    request.ContentLength = data.Length;
                    using (var stream = request.GetRequestStream())
                        stream.Write(data, 0, data.Length);

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string json = reader.ReadToEnd();
                        ParseResponse(json);
                    }
                }
                catch (WebException wex)
                {
                    var resp = wex.Response as HttpWebResponse;
                    int code = resp != null ? (int)resp.StatusCode : 0;
                    string userMsg;
                    switch (code)
                    {
                        case 401: userMsg = "Invalid API key"; break;
                        case 402: userMsg = "Out of credits"; break;
                        case 404: userMsg = "Model not found"; break;
                        case 429:
                            userMsg = "Rate limited — waiting 30s";
                            _nextDecisionTime = Time.time + 30f;
                            break;
                        case 500: case 502: case 503:
                            userMsg = "Server down (HTTP " + code + ") — try another model";
                            _nextDecisionTime = Time.time + 15f;
                            break;
                        default:
                            userMsg = wex.Status == WebExceptionStatus.Timeout ? "Request timed out" : wex.Message;
                            break;
                    }
                    Plugin.Log.LogWarning("[AI:" + agentName + "] " + userMsg + (code > 0 ? " (HTTP " + code + ")" : ""));
                    AIGiantess.AddChatLine("<color=#FF4444>" + agentName + ": " + userMsg + "</color>");
                    _waiting = false;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError("[AI:" + agentName + "] " + ex.Message);
                    _waiting = false;
                }
            });
        }

        void ParseResponse(string json)
        {
            try
            {
                // Log raw response for debugging (compact)
                string compactJson = json.Replace("\n", "").Replace("\r", "").Replace("  ", "");
                if (compactJson.Length > 200)
                    compactJson = compactJson.Substring(0, 200) + "...";

                int contentIdx = json.IndexOf("\"content\":");
                if (contentIdx < 0) { _waiting = false; return; }

                int start = json.IndexOf('"', contentIdx + 10) + 1;
                int end = start;
                while (end < json.Length)
                {
                    if (json[end] == '\\') { end += 2; continue; }
                    if (json[end] == '"') break;
                    end++;
                }
                string content = json.Substring(start, end - start)
                    .Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");

                string action = "stand_idle", dialogue = "", morphs = "", anim = "";

                foreach (var line in content.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("ACTION:"))
                        action = trimmed.Substring(7).Trim().ToLower().Replace(" ", "_");
                    else if (trimmed.StartsWith("SAY:"))
                        dialogue = trimmed.Substring(4).Trim().Trim('"');
                    else if (trimmed.StartsWith("MORPH:"))
                        morphs = trimmed.Substring(6).Trim();
                    else if (trimmed.StartsWith("ANIM:"))
                        anim = trimmed.Substring(5).Trim();
                }

                bool valid = false;
                foreach (var a in ACTIONS)
                    if (a == action) { valid = true; break; }

                if (!valid)
                {
                    if (action.Contains("idle") || action.Contains("stand"))
                        action = "look_at_player";
                    else if (action.Contains("walk") || action.Contains("move") || action.Contains("approach"))
                        action = "walk_to_player";
                    else if (action.Contains("crouch") || action.Contains("kneel"))
                        action = "crouch_look";
                    else
                        action = "look_at_player";
                }

                // Post to shared conversation so other agents can see
                AIGiantess.AddAgentResponse(AgentName, action, dialogue);

                _pendingAction = action;
                _pendingDialogue = dialogue;
                _pendingMorphs = morphs;
                _pendingAnim = anim;
                _waiting = false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[AI:" + AgentName + "] Parse failed: " + ex.Message);
                _waiting = false;
            }
        }

        // ===================== ACTIONS =====================

        void ExecuteAction(string action, string dialogue, string customAnim = null)
        {
            if (_giantess == null || _player == null) return;

            Plugin.Log.LogInfo("[AI:" + AgentName + "] Action: " + action + " | Say: " + dialogue +
                (!string.IsNullOrEmpty(customAnim) ? " | Anim: " + customAnim : ""));

            if (!string.IsNullOrEmpty(dialogue))
            {
                AIGiantess.AddChatLine(AgentName + ": " + dialogue);
                _recentDialogue.Add(dialogue);
                if (_recentDialogue.Count > 15)
                    _recentDialogue.RemoveAt(0);
            }

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            var humanoid = _giantess as Humanoid;
            var animMgr = humanoid != null ? humanoid.animationManager : null;
            if (animMgr != null)
            {
                string animToPlay = null;

                if (!string.IsNullOrEmpty(customAnim) &&
                    IOManager.Instance.AnimationControllers.ContainsKey(customAnim))
                    animToPlay = customAnim;
                else
                {
                    string defaultAnim;
                    if (ACTION_ANIMS.TryGetValue(action, out defaultAnim))
                        animToPlay = defaultAnim;
                }

                if (animToPlay != null && !_animLocked)
                {
                    bool isMovement = action == "walk_to_player" || action == "wander" || action == "walk_away";
                    if (isMovement || Time.time >= _lastAnimTime + AIGiantess.animCooldown)
                    {
                        animMgr.PlayAnimation(animToPlay, false, false);
                        _lastAnimTime = Time.time;
                        Plugin.Log.LogInfo("[AI:" + AgentName + "] Playing anim: " + animToPlay);
                    }
                }
            }

            Vector3 playerPos = _player.transform.position;
            Vector3 lookTarget = new Vector3(playerPos.x, _giantess.transform.position.y, playerPos.z);

            // Cap movement distance to prevent insane coordinates
            float moveRange = _giantess.Height * 3f; // walk at most 3x body height

            switch (action)
            {
                case "walk_to_player":
                    // Don't call LookAtY here: the game's steering (LookWhereYouAreGoing)
                    // already faces the movement direction. Doing both makes two systems
                    // write rotation each frame and fight → the giantess flips out.
                    // Clamp to max range
                    Vector3 toPlayer = playerPos - _giantess.transform.position;
                    toPlayer.y = 0;
                    if (toPlayer.magnitude > moveRange)
                        toPlayer = toPlayer.normalized * moveRange;
                    MoveGiantessTo(_giantess.transform.position + toPlayer);
                    break;
                case "wander":
                    var wanderDir = UnityEngine.Random.insideUnitSphere;
                    wanderDir.y = 0;
                    MoveGiantessTo(_giantess.transform.position + wanderDir.normalized * _giantess.Height * 1f);
                    break;
                case "walk_away":
                    var awayDir = (_giantess.transform.position - playerPos).normalized;
                    awayDir.y = 0;
                    MoveGiantessTo(_giantess.transform.position + awayDir * _giantess.Height * 1.5f);
                    break;
                case "look_at_player":
                case "crouch_look":
                case "crouch_idle":
                case "stomp_near":
                case "buttcrush":
                case "grab_player":
                case "pet_player":
                case "poke_player":
                    LookAtY(lookTarget);
                    break;
                case "stuff_in_panties":
                    var hipBone = _giantess.GetComponent<Animator>()?.GetBoneTransform(HumanBodyBones.Hips);
                    if (hipBone != null && _player != null)
                    {
                        float s = _giantess.Scale;
                        var pos = hipBone.position - _giantess.transform.forward * 0.03f * s;
                        pos.y -= 0.02f * s;
                        _player.transform.position = pos;
                    }
                    break;
            }

            _lastAction = action;
            _recentActions.Add(action);
            if (_recentActions.Count > 5)
                _recentActions.RemoveAt(0);

            if (action == "laugh" || action == "taunt" || action == "dance")
                _currentMood = "playful";
            else if (action == "stomp_near" || action == "buttcrush")
                _currentMood = "dominant";
            else if (action == "pet_player" || action == "wave")
                _currentMood = "affectionate";
            else if (action == "stuff_in_panties" || action == "grab_player")
                _currentMood = "mischievous";
        }

        void MoveGiantessTo(Vector3 target)
        {
            if (_animLocked) return;
            if (_giantess == null) return;
            var humanoid = _giantess as Humanoid;
            if (humanoid == null || humanoid.actionManager == null) return;

            try
            {
                target.y = _giantess.transform.position.y;
                // Clear stale steering first. StartArriveBehavior appends a new arrive
                // and re-adds the shared LookWhereYouAreGoing/AvoidWall behaviors without
                // clearing the list, so repeated moves stack duplicate steerers whose
                // angular output sums and spins the giantess. Stop() resets the list.
                var mover = HarmonyLib.Traverse.Create(_giantess).Property("movement").GetValue<MovementCharacter>();
                if (mover != null)
                    mover.Stop();
                var kinematic = new SteeringBehaviors.VectorKinematic(target);
                var arriveAction = new ArriveAction(kinematic);
                humanoid.actionManager.ScheduleAction(arriveAction);
                Plugin.Log.LogInfo("[AI:" + AgentName + "] Moving to " + target);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[AI:" + AgentName + "] Move failed: " + ex.Message);
            }
        }

        void LookAtY(Vector3 target)
        {
            if (_giantess == null || _animLocked) return;
            Vector3 dir = target - _giantess.transform.position;
            dir.y = 0;
            // Need a meaningful horizontal offset. LookRotation on a near-zero vector
            // returns a garbage/flipping orientation, so scale the minimum to the
            // giantess — a player standing right at her feet must not trigger a spin.
            float minDist = _giantess.Height * 0.05f;
            if (dir.magnitude < minDist) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            // Already facing there? Don't re-fire every decision (avoids thrash).
            if (Quaternion.Angle(_giantess.transform.rotation, targetRot) < 5f) return;

            // Stop any in-flight rotation first so overlapping decisions don't spawn
            // multiple coroutines fighting over transform.rotation.
            if (_rotateCoroutine != null)
                StopCoroutine(_rotateCoroutine);
            _rotateCoroutine = StartCoroutine(SmoothRotate(targetRot, 1f));
        }

        IEnumerator SmoothRotate(Quaternion target, float duration)
        {
            if (_giantess == null) yield break;
            float elapsed = 0f;
            Quaternion start = _giantess.transform.rotation;
            while (elapsed < duration && _giantess != null && !_animLocked)
            {
                elapsed += Time.deltaTime;
                _giantess.transform.rotation = Quaternion.Slerp(start, target, elapsed / duration);
                yield return null;
            }
        }

        void ApplyMorphs(string morphString)
        {
            if (!AIGiantess.morphsEnabled)
            {
                Plugin.Log.LogInfo("[AI:" + AgentName + "] Morphs BLOCKED (disabled in settings)");
                return;
            }
            if (_giantess == null || string.IsNullOrEmpty(morphString)) return;
            var morphs = _giantess.Morphs;
            if (morphs == null) return;

            // Reset all previously AI-set morphs to 0 before applying new ones
            // This prevents expressions from stacking and distorting the face
            if (_lastAIMorphIndices != null)
            {
                foreach (int idx in _lastAIMorphIndices)
                {
                    if (idx >= 0 && idx < morphs.Count)
                        _giantess.SetMorphValue(idx, 0f);
                }
            }
            _lastAIMorphIndices = new List<int>();

            foreach (var pair in morphString.Split(','))
            {
                var trimmed = pair.Trim();
                int eqIdx = trimmed.LastIndexOf('=');
                if (eqIdx < 1) continue;

                string name = trimmed.Substring(0, eqIdx).Trim();
                string valStr = trimmed.Substring(eqIdx + 1).Trim();

                float val;
                if (!float.TryParse(valStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out val)) continue;
                val = Mathf.Clamp(val, 0f, 0.5f);

                // Only apply if we find an exact or suffix match
                for (int i = 0; i < morphs.Count; i++)
                {
                    if (morphs[i].Name == name ||
                        morphs[i].Name.EndsWith("." + name) ||
                        name.EndsWith("." + morphs[i].Name))
                    {
                        _giantess.SetMorphValue(i, val);
                        _lastAIMorphIndices.Add(i);
                        break;
                    }
                }
            }
            Plugin.Log.LogInfo("[AI:" + AgentName + "] Applied morphs: " + morphString);
        }

        // ===================== TTS =====================

        /// <summary>
        /// Convert AI dialogue to Fish Audio tagged text.
        /// Strips *asterisk* markers, converts to (emotion) tags, and adds mood-based emotion.
        /// </summary>
        string ApplyFishEmotionTags(string text, string action)
        {
            // Convert *asterisk* style markers to Fish Audio tags
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"*giggle*", "(chuckling) Heh heh"},
                {"*giggles*", "(chuckling) Heh heh"},
                {"*laughs*", "(laughing) Ha ha ha"},
                {"*laugh*", "(laughing) Ha ha"},
                {"*sigh*", "(sighing)"},
                {"*sighs*", "(sighing)"},
                {"*gasp*", "(gasping)"},
                {"*gasps*", "(gasping)"},
                {"*whisper*", "(whispering)"},
                {"*whispers*", "(whispering)"},
                {"*moan*", "(soft tone)"},
                {"*moans*", "(soft tone)"},
                {"*pant*", "(panting)"},
                {"*pants*", "(panting)"},
                {"*scream*", "(screaming)"},
                {"*screams*", "(screaming)"},
                {"*sob*", "(sobbing)"},
                {"*sobs*", "(sobbing)"},
                {"*cry*", "(crying loudly)"},
                {"*cries*", "(crying loudly)"},
                {"*yawn*", "(yawning)"},
                {"*yawns*", "(yawning)"},
                {"*groan*", "(groaning)"},
                {"*groans*", "(groaning)"},
                {"*wink*", ""},
                {"*winks*", ""},
                {"*smirk*", ""},
                {"*smirks*", ""},
            };

            foreach (var kvp in replacements)
                text = text.Replace(kvp.Key, kvp.Value);

            // Strip any remaining *asterisk* markers
            while (text.Contains("*"))
            {
                int start = text.IndexOf('*');
                int end = text.IndexOf('*', start + 1);
                if (end < 0) break;
                text = text.Remove(start, end - start + 1);
            }

            text = text.Trim();
            if (string.IsNullOrEmpty(text)) return "";

            // Add emotion tag at the beginning based on action/mood if none present
            if (!text.StartsWith("("))
            {
                string emotion = "";
                switch (action)
                {
                    case "laugh":
                    case "taunt":
                        emotion = "(sarcastic) ";
                        break;
                    case "dance":
                        emotion = "(excited) ";
                        break;
                    case "pet_player":
                    case "wave":
                        emotion = "(happy) ";
                        break;
                    case "stomp_near":
                    case "buttcrush":
                        emotion = "(confident) ";
                        break;
                    case "stuff_in_panties":
                    case "grab_player":
                        emotion = "(excited) ";
                        break;
                    case "poke_player":
                        emotion = "(curious) ";
                        break;
                    case "crouch_look":
                    case "look_at_player":
                        emotion = "(curious) ";
                        break;
                }
                text = emotion + text;
            }

            return text;
        }

        string _pendingTTSText; // stored for retry if another agent is speaking

        void SpeakTTS(string text)
        {
            if (!_config.TtsEnabled || string.IsNullOrEmpty(text)) return;
            if (_ttsFailCount >= 5) return;

            // Apply Fish Audio emotion tags if using Fish provider
            if (_config.TtsProvider == "fish")
                text = ApplyFishEmotionTags(text, _lastAction);

            if (string.IsNullOrEmpty(text)) return;


            string finalText = text;
            string provider = _config.TtsProvider;
            string agentName = AgentName;

            Plugin.Log.LogInfo("[AI-TTS:" + agentName + "] Speaking (" + provider + "): " +
                finalText.Substring(0, Math.Min(finalText.Length, 80)));

            if (provider == "edge")
                SpeakEdgeTTS(finalText);
            else if (provider == "fish")
                SpeakFishTTS(finalText);
            else
                SpeakElevenLabsTTS(finalText);
        }

        void SpeakEdgeTTS(string text)
        {
            string voice = _config.TtsEdgeVoice;
            string agentName = AgentName;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string tempPath = Path.Combine(Application.temporaryCachePath, "ai_tts_" + agentName + "_" + DateTime.Now.Ticks + ".mp3");
                    string safeText = text.Replace("\"", "'").Replace("\n", " ");
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "python3",
                        Arguments = "-m edge_tts --voice " + voice + " --text \"" + safeText + "\" --write-media \"" + tempPath + "\"",
                        UseShellExecute = false, CreateNoWindow = true,
                        RedirectStandardOutput = true, RedirectStandardError = true
                    };
                    var proc = System.Diagnostics.Process.Start(psi);
                    proc.WaitForExit(10000);
                    if (proc.ExitCode == 0 && File.Exists(tempPath))
                        _pendingAudioPath = tempPath;
                    else
                    {
                        Plugin.Log.LogError("[AI-TTS:" + agentName + "] Edge failed: " + proc.StandardError.ReadToEnd());
                        _ttsFailCount++;
                    }
                }
                catch (Exception ex)
                {
                    _ttsFailCount++;
                    Plugin.Log.LogError("[AI-TTS:" + agentName + "] Edge error: " + ex.Message);
                }
            });
        }

        void SpeakElevenLabsTTS(string text)
        {
            if (string.IsNullOrEmpty(_config.TtsApiKey)) return;
            string voiceId = _config.TtsVoiceId;
            string apiKey = _config.TtsApiKey;
            string agentName = AgentName;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string url = "https://api.elevenlabs.io/v1/text-to-speech/" + voiceId;
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Headers.Add("xi-api-key", apiKey);
                    request.Timeout = 15000;
                    string body = "{\"text\":" + JsonEscape(text) + ",\"model_id\":\"eleven_multilingual_v2\"}";
                    byte[] data = Encoding.UTF8.GetBytes(body);
                    request.ContentLength = data.Length;
                    using (var stream = request.GetRequestStream()) stream.Write(data, 0, data.Length);
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var audioStream = response.GetResponseStream())
                    {
                        string tempPath = Path.Combine(Application.temporaryCachePath, "ai_tts_" + agentName + "_" + DateTime.Now.Ticks + ".mp3");
                        using (var fs = new FileStream(tempPath, FileMode.Create)) audioStream.CopyTo(fs);
                        _pendingAudioPath = tempPath;
                    }
                }
                catch (Exception ex)
                {
                    _ttsFailCount++;
                    Plugin.Log.LogError("[AI-TTS:" + agentName + "] ElevenLabs failed: " + ex.Message);
                }
            });
        }

        void SpeakFishTTS(string text)
        {
            if (string.IsNullOrEmpty(_config.TtsFishApiKey) || string.IsNullOrEmpty(_config.TtsFishModelId)) return;
            string fishKey = _config.TtsFishApiKey;
            string fishModel = _config.TtsFishModelId;
            string agentName = AgentName;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string url = "https://api.fish.audio/v1/tts";
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Headers.Add("Authorization", "Bearer " + fishKey);
                    request.Timeout = 15000;
                    string body = "{\"text\":" + JsonEscape(text) + ",\"reference_id\":\"" + fishModel + "\",\"format\":\"mp3\"}";
                    byte[] data = Encoding.UTF8.GetBytes(body);
                    request.ContentLength = data.Length;
                    using (var stream = request.GetRequestStream()) stream.Write(data, 0, data.Length);
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var audioStream = response.GetResponseStream())
                    {
                        string tempPath = Path.Combine(Application.temporaryCachePath, "ai_tts_" + agentName + "_" + DateTime.Now.Ticks + ".mp3");
                        using (var fs = new FileStream(tempPath, FileMode.Create)) audioStream.CopyTo(fs);
                        _pendingAudioPath = tempPath;
                    }
                }
                catch (Exception ex)
                {
                    _ttsFailCount++;
                    Plugin.Log.LogError("[AI-TTS:" + agentName + "] Fish failed: " + ex.Message);
                }
            });
        }

        // ===================== AUDIO / LIP SYNC =====================

        void PlayPendingAudio()
        {
            if (_pendingAudioPath == null) return;
            string path = _pendingAudioPath;
            _pendingAudioPath = null;
            StartCoroutine(LoadAndPlayAudio(path));
        }

        IEnumerator LoadAndPlayAudio(string path)
        {
            string uri = "file:///" + path.Replace("\\", "/");
            using (var www = new WWW(uri))
            {
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    AudioClip clip = www.GetAudioClip(false, false, AudioType.MPEG);
                    if (clip != null)
                    {
                        if (_ttsAudioSource == null || _ttsAudioSource.gameObject == null)
                        {
                            GameObject audioGo = _giantess != null ? _giantess.gameObject :
                                new GameObject("AI_TTS_" + AgentName);
                            if (_giantess == null) UnityEngine.Object.DontDestroyOnLoad(audioGo);
                            _ttsAudioSource = audioGo.GetComponent<AudioSource>();
                            if (_ttsAudioSource == null)
                                _ttsAudioSource = audioGo.AddComponent<AudioSource>();
                            _ttsAudioSource.spatialBlend = 0f;
                            _ttsAudioSource.volume = 1f;
                        }
                        // Don't play here — queue the clip for Update to play sequentially
                        _pendingClip = clip;
                        _ttsAudioSource.clip = clip;
                        // Find viseme morphs now, but don't activate lip sync until Play
                        if (_visemeMorphs == null) FindVisemeMorphs();
                    }
                }
                else
                {
                    Plugin.Log.LogError("[AI-TTS:" + AgentName + "] Audio load error: " + www.error);
                }
            }
        }

        void FindVisemeMorphs()
        {
            if (_giantess == null) return;
            var morphs = _giantess.Morphs;
            if (morphs == null) return;

            var found = new List<EntityMorphData>();

            // Check both English and Japanese visemes, use whichever has more matches
            var englishFound = new List<EntityMorphData>();
            string[] visemeNames = { "O", "F", "U", "E", "A" };
            foreach (var target in visemeNames)
                foreach (var m in morphs)
                    if (m.Name == target) { englishFound.Add(m); break; }

            var jpFound = new List<EntityMorphData>();
            string[] jpNames = { "あ", "い", "う", "え", "お" };
            foreach (var target in jpNames)
                foreach (var m in morphs)
                    if (m.Name == target) { jpFound.Add(m); break; }

            // Also try with number prefix (some models store as "107.あ")
            if (jpFound.Count == 0)
            {
                foreach (var m in morphs)
                {
                    foreach (var jp in jpNames)
                    {
                        // Only match "あ" or "107.あ" — not "尻穴しまう" etc
                        if (m.Name == jp || (m.Name.Contains(".") && m.Name.EndsWith("." + jp) && m.Name.Length <= jp.Length + 5))
                        { jpFound.Add(m); break; }
                    }
                }
            }

            // Prefer Japanese if it found more (or equal — JP visemes are more standard for anime models)
            if (jpFound.Count >= englishFound.Count && jpFound.Count > 0)
                found = jpFound;
            else if (englishFound.Count > 0)
                found = englishFound;

            Plugin.Log.LogInfo("[AI-TTS:" + AgentName + "] Viseme search: EN=" + englishFound.Count + " JP=" + jpFound.Count + " total morphs=" + morphs.Count);

            if (found.Count == 0)
            {
                string[] mouthNames = { "MouthOpen", "Mouth Open", "mouth_open", "mouth_in", "JawOpen", "Mouth" };
                foreach (var target in mouthNames)
                {
                    foreach (var m in morphs)
                        if (m.Name.Equals(target, StringComparison.OrdinalIgnoreCase)) { found.Add(m); break; }
                    if (found.Count > 0) break;
                }
            }

            if (found.Count == 0)
                foreach (var m in morphs)
                {
                    string lower = m.Name.ToLower();
                    if (lower.Contains("mouth") || lower.Contains("jaw")) { found.Add(m); break; }
                }

            _visemeMorphs = found.Count > 0 ? found.ToArray() : null;
            if (_visemeMorphs != null)
            {
                var names = new List<string>();
                foreach (var v in _visemeMorphs) names.Add(v.Name);
                Plugin.Log.LogInfo("[AI-TTS:" + AgentName + "] Lip sync morphs: " + string.Join(", ", names.ToArray()));
            }
        }

        void UpdateLipSync()
        {
            if (!AIGiantess.lipSyncEnabled || !_lipSyncActive || _ttsAudioSource == null || !_ttsAudioSource.isPlaying)
            {
                if (_lipSyncActive)
                {
                    _lipSyncActive = false;
                    _lipSyncSmoothed = 0f;
                    if (_visemeMorphs != null)
                        foreach (var m in _visemeMorphs) SetMorphDirect(m, 0f);
                    // Signal done and advance ticket
                    if (AIGiantess._currentSpeaker == this)
                    {
                        AIGiantess._isSpeaking = false;
                        AIGiantess._currentSpeaker = null;
                        AIGiantess._audioNowServing++;
                    }
                }
                return;
            }

            _ttsAudioSource.GetOutputData(_sampleBuffer, 0);
            float sum = 0f;
            for (int i = 0; i < _sampleBuffer.Length; i++)
                sum += Mathf.Abs(_sampleBuffer[i]);
            float amplitude = sum / _sampleBuffer.Length;

            float target = Mathf.Clamp01(amplitude * 30f);
            float speed = target > _lipSyncSmoothed ? 25f : 10f;
            _lipSyncSmoothed = Mathf.Lerp(_lipSyncSmoothed, target, Time.deltaTime * speed);

            if (_visemeMorphs == null) return;

            if (_visemeMorphs.Length == 1)
            {
                SetMorphDirect(_visemeMorphs[0], _lipSyncSmoothed);
            }
            else
            {
                // Cycle visemes based on time — smoother than amplitude-based switching
                if (_lipSyncSmoothed > 0.1f)
                {
                    _visemeCycle = (int)(Time.time * 6f) % _visemeMorphs.Length; // ~6 changes/sec
                }

                for (int i = 0; i < _visemeMorphs.Length; i++)
                {
                    float targetWeight;
                    if (i == _visemeCycle)
                        targetWeight = _lipSyncSmoothed;
                    else if (i == (_visemeCycle + 1) % _visemeMorphs.Length)
                        targetWeight = _lipSyncSmoothed * 0.3f; // slight blend to next
                    else
                        targetWeight = 0f;

                    float current = _visemeMorphs[i].Weight / 100f;
                    SetMorphDirect(_visemeMorphs[i], Mathf.Lerp(current, targetWeight, Time.deltaTime * 15f));
                }
            }
        }

        void SetMorphDirect(EntityMorphData morph, float weight)
        {
            if (morph == null) return;
            morph.Weight = weight;
            foreach (var mesh in morph.mesh)
                mesh.mesh.SetBlendShapeWeight(mesh.id, weight * 100f);
        }

        static string JsonEscape(string s)
        {
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using BepInEx;
using UnityEngine;

namespace SizeboxFix
{
    /// <summary>
    /// AI Giantess controller. Sends game state to OpenAI, receives actions + dialogue.
    /// Reads API key from BepInEx/config/SizeboxAI.cfg
    /// </summary>
    public class AIGiantess : MonoBehaviour
    {
        public static AIGiantess Instance { get; private set; }

        // Config
        private string _apiKey;
        private string _apiUrl = "https://openrouter.ai/api/v1/chat/completions";
        private string _model = "cognitivecomputations/dolphin-mistral-24b-venice-edition:free";
        private string _personality = "";
        private float _decisionInterval = 5f;

        // TTS Config
        private string _ttsApiKey = "";
        private string _ttsVoiceId = "eVItLK1UvXctxuaRV2Oq"; // Jean - Alluring Femme Fatale
        private bool _ttsEnabled = false;
        private AudioSource _ttsAudioSource;

        // State
        private EntityBase _giantess;
        private EntityBase _player;
        private float _nextDecisionTime;
        private string _pendingAction;
        private string _pendingDialogue;
        private string _pendingMorphs;
        private string _pendingAnim;
        private bool _waiting;
        private List<string> _conversationHistory = new List<string>();
        private string _lastAction = "idle";
        private string _currentMood = "playful";
        private List<string> _recentActions = new List<string>();
        private List<string> _recentDialogue = new List<string>();
        private Coroutine _moveCoroutine;
        private bool _inConversation; // True after player sends a message
        private float _conversationTimeout; // When to go back to auto mode
        private float _lastAnimTime; // When last animation was played
        private const float ANIM_COOLDOWN = 5f;
        private const float AUTO_INTERVAL = 20f;
        private const float CONVERSATION_TIMEOUT = 60f; // Go back to auto after 60s of silence

        // On-screen chat log
        internal static List<string> _chatLog = new List<string>();
        internal static float _chatLogTimer;
        private const int MAX_CHAT_LINES = 6;
        internal const float CHAT_DISPLAY_TIME = 15f;
        private string _cachedMorphNames;
        private string _cachedAnimNames;
        private string _playerMessage; // Message from player to AI

        // Available actions the AI can choose
        private static readonly string[] ACTIONS = {
            "walk_to_player", "crouch_look", "wander",
            "stomp_near", "sit_down", "grab_player", "taunt",
            "look_at_player", "walk_away", "dance",
            "grow", "shrink", "stuff_in_panties", "buttcrush",
            "pet_player", "poke_player", "laugh", "wave"
        };

        private static readonly Dictionary<string, string> ACTION_ANIMS = new Dictionary<string, string>
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
            {"grow", "Happy"},
            {"shrink", "Bashful"},
            {"stuff_in_panties", "Acknowledging"},
            {"buttcrush", "Sit 6"},
            {"pet_player", "Greet"},
            {"poke_player", "Acknowledging"},
            {"laugh", "Laughing"},
            {"wave", "Waving 2"}
        };

        void Awake()
        {
            Instance = this;
            LoadConfig();
        }

        void LoadConfig()
        {
            string configPath = Path.Combine(Paths.ConfigPath, "SizeboxAI.cfg");

            if (!File.Exists(configPath))
            {
                // Create default config
                var sb = new StringBuilder();
                sb.AppendLine("# Sizebox AI Configuration");
                sb.AppendLine("# Get your API key from https://openrouter.ai/keys");
                sb.AppendLine("ApiKey=YOUR_KEY_HERE");
                sb.AppendLine("");
                sb.AppendLine("# API endpoint (default: OpenRouter, also works with OpenAI or local KoboldCpp)");
                sb.AppendLine("ApiUrl=https://openrouter.ai/api/v1/chat/completions");
                sb.AppendLine("");
                sb.AppendLine("# Model to use on OpenRouter (uncensored options):");
                sb.AppendLine("# cognitivecomputations/dolphin-mistral-24b-venice-edition:free  (free, uncensored, good)");
                sb.AppendLine("# nousresearch/hermes-3-llama-3.1-405b  (paid, very smart)");
                sb.AppendLine("# mistralai/mistral-7b-instruct:free  (free, decent)");
                sb.AppendLine("# For OpenAI: gpt-4o-mini  |  For local KoboldCpp: koboldcpp/model");
                sb.AppendLine("Model=cognitivecomputations/dolphin-mistral-24b-venice-edition:free");
                sb.AppendLine("");
                sb.AppendLine("# Seconds between AI decisions (lower = more responsive but more API calls)");
                sb.AppendLine("DecisionInterval=5");
                sb.AppendLine("");
                sb.AppendLine("# Personality prompt - describe how the giantess should behave");
                sb.AppendLine("Personality=You are a submissive giantess who serves your tiny master. You are eager to please, obedient, and devoted. You crawl when approaching your master. You love being told what to do. You call the tiny person Master. You are flirty and seductive, always trying to impress.");
                sb.AppendLine("");
                sb.AppendLine("# ElevenLabs Text-to-Speech");
                sb.AppendLine("# Get key from elevenlabs.io -> Developers");
                sb.AppendLine("TTSApiKey=");
                sb.AppendLine("TTSVoiceId=eVItLK1UvXctxuaRV2Oq");
                sb.AppendLine("TTSEnabled=true");
                File.WriteAllText(configPath, sb.ToString());

                Plugin.Log.LogWarning("[AI] Created config at " + configPath + " — paste your API key there!");
                new Toast("_ai").Print("Get your key from openrouter.ai/keys — paste in BepInEx/config/SizeboxAI.cfg");
                return;
            }

            foreach (var line in File.ReadAllLines(configPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#") || !trimmed.Contains("=")) continue;

                var parts = trimmed.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var val = parts[1].Trim();

                switch (key)
                {
                    case "ApiKey": _apiKey = val; break;
                    case "ApiUrl": _apiUrl = val; break;
                    case "Model": _model = val; break;
                    case "DecisionInterval": float.TryParse(val, out _decisionInterval); break;
                    case "Personality": _personality = val; break;
                    case "TTSApiKey": _ttsApiKey = val; break;
                    case "TTSVoiceId": _ttsVoiceId = val; break;
                    case "TTSEnabled": _ttsEnabled = val.ToLower() == "true"; break;
                }
            }

            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_KEY_HERE")
            {
                Plugin.Log.LogWarning("[AI] No API key set in SizeboxAI.cfg");
                new Toast("_ai").Print("Set your API key in BepInEx/config/SizeboxAI.cfg");
            }
            else
            {
                Plugin.Log.LogInfo("[AI] Config loaded. Model: " + _model);
            }
        }

        /// <summary>
        /// Start AI control of a giantess targeting a player/micro
        /// </summary>
        public void StartAI(EntityBase giantess, EntityBase player)
        {
            _giantess = giantess;
            _player = player;
            _nextDecisionTime = Time.time + 1f;
            _conversationHistory.Clear();
            _lastAction = "idle";
            _cachedMorphNames = null;

            // Cache morph names for the prompt
            var morphs = giantess.Morphs;
            if (morphs != null && morphs.Count > 0)
            {
                var names = new List<string>();
                // Only safe morphs that look good — NO mouth/lip morphs (they look horrific)
                string[] safeKeywords = { "Smile", "Brow", "Eye", "Blush", "Breathe",
                    "Thick", "Breast", "Nipple", "Aroused" };
                string[] blockedKeywords = { "Mouth", "Lip", "Tongue", "Pucker", "Suck",
                    "Puff", "Open", "Tiny", "Full" };
                for (int i = 0; i < morphs.Count && names.Count < 15; i++)
                {
                    string name = morphs[i].Name;
                    bool safe = false;
                    bool blocked = false;
                    foreach (var kw in safeKeywords)
                        if (name.Contains(kw)) { safe = true; break; }
                    foreach (var kw in blockedKeywords)
                        if (name.Contains(kw)) { blocked = true; break; }
                    if (safe && !blocked)
                        names.Add(name);
                }
                if (names.Count > 0)
                    _cachedMorphNames = string.Join(", ", names.ToArray());

                Plugin.Log.LogInfo("[AI] Found " + (names.Count) + " morphs for AI control");
            }

            // Cache only default game animations (not custom model ones)
            _cachedAnimNames = null;
            if (IOManager.Instance != null && IOManager.Instance.AnimationControllers != null)
            {
                var allAnims = IOManager.Instance.AnimationControllers;
                var defaultController = IOManager.Instance.gtsAnimatorController;
                Plugin.Log.LogInfo("[AI] Found " + allAnims.Count + " animations total");

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
                    Plugin.Log.LogInfo("[AI] Using " + defaultAnims.Count + " default animations for AI");
                }
            }

            Plugin.Log.LogInfo("[AI] Started AI for " + giantess.name + " targeting " + player.name);
            new Toast("_ai").Print("AI Giantess activated!");
        }

        public void StopAI()
        {
            _giantess = null;
            _player = null;
            new Toast("_ai").Print("AI Giantess deactivated");
        }

        public void SendPlayerMessage(string msg)
        {
            _playerMessage = msg;
            _conversationHistory.Add("{\"role\":\"user\",\"content\":" + JsonEscape("The tiny person says: " + msg) + "}");
            // Trigger immediate response
            _nextDecisionTime = Time.time;
            _waiting = false;
            // Enter conversation mode — she only responds to you now
            _inConversation = true;
            _conversationTimeout = Time.time + CONVERSATION_TIMEOUT;
            AddChatLine("You: " + msg);
        }

        public bool IsActive => _giantess != null && _player != null;

        void Update()
        {
            if (!IsActive) return;
            if (_giantess == null || _player == null) { StopAI(); return; }
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_KEY_HERE") return;

            // Play pending TTS audio
            PlayPendingAudio();

            // Apply pending action from background thread
            if (_pendingAction != null)
            {
                ExecuteAction(_pendingAction, _pendingDialogue, _pendingAnim);
                if (!string.IsNullOrEmpty(_pendingMorphs))
                    ApplyMorphs(_pendingMorphs);
                if (!string.IsNullOrEmpty(_pendingDialogue))
                    SpeakTTS(_pendingDialogue);
                _pendingAction = null;
                _pendingDialogue = null;
                _pendingMorphs = null;
                _pendingAnim = null;
            }

            // Check if conversation mode should end
            if (_inConversation && Time.time > _conversationTimeout)
            {
                _inConversation = false;
                Plugin.Log.LogInfo("[AI] Conversation timeout — back to auto mode");
            }

            // Request new decision
            if (!_waiting && Time.time >= _nextDecisionTime)
            {
                if (_inConversation)
                {
                    // In conversation mode — only respond if player sent a message
                    if (!string.IsNullOrEmpty(_playerMessage))
                    {
                        _nextDecisionTime = Time.time + _decisionInterval;
                        RequestDecision();
                    }
                }
                else
                {
                    // Auto mode — act every AUTO_INTERVAL seconds
                    _nextDecisionTime = Time.time + AUTO_INTERVAL;
                    RequestDecision();
                }
            }

            // Keep carrying player if stuffed
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

        string BuildGameState()
        {
            float dist = Vector3.Distance(_giantess.transform.position, _player.transform.position);
            float gtsHeight = _giantess.Height;
            float playerHeight = _player.MeshHeight;
            float sizeRatio = gtsHeight / Mathf.Max(playerHeight, 0.01f);
            string playerRelative = dist < gtsHeight * 0.1f ? "right at your feet" :
                                   dist < gtsHeight * 0.5f ? "nearby" :
                                   dist < gtsHeight * 2f ? "a bit away" : "far away";

            var sb = new StringBuilder();
            sb.AppendLine("GAME STATE:");
            sb.AppendLine("- You are " + sizeRatio.ToString("F0") + "x taller than the tiny person");
            sb.AppendLine("- The tiny person is " + playerRelative);
            sb.AppendLine("- Your current mood: " + _currentMood);
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
                _playerMessage = null; // Clear after using
            }

            return sb.ToString();
        }

        string BuildPrompt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are controlling a giantess in a game. " + _personality);
            sb.AppendLine();
            sb.AppendLine(BuildGameState());
            sb.AppendLine();
            sb.AppendLine("Choose ONE action and write a short line of dialogue (what you say/think).");
            sb.AppendLine("Available actions: " + string.Join(", ", ACTIONS));
            if (!string.IsNullOrEmpty(_cachedAnimNames))
            {
                sb.AppendLine();
                sb.AppendLine("You can play a specific animation with an ANIM: line. Some examples: " + _cachedAnimNames);
            }
            sb.AppendLine();
            if (!string.IsNullOrEmpty(_cachedMorphNames))
            {
                sb.AppendLine("You can also control facial expressions and body morphs.");
                sb.AppendLine("Available morphs: " + _cachedMorphNames);
                sb.AppendLine("Set morphs as comma-separated Name=Value pairs (0.0 to 1.0). Only set morphs that match your mood/action.");
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
            sb.AppendLine("Be creative, vary your actions, and stay in character. Keep dialogue under 15 words. MORPH and ANIM lines are optional — only include when you want to change expression or play a specific animation.");

            return sb.ToString();
        }

        void RequestDecision()
        {
            _waiting = true;
            string prompt = BuildPrompt();

            // Build messages with strong system prompt
            var messages = new List<string>();
            string sysPrompt = _personality +
                "\n\nIMPORTANT RULES:" +
                "\n- When the tiny person speaks to you, you MUST directly respond to what they said." +
                "\n- NEVER repeat the same action or dialogue twice in a row. Always vary your behavior." +
                "\n- Your dialogue should be natural and reactive, not generic." +
                "\n- Match your actions to the conversation — if they ask you to do something, DO IT." +
                "\n- Move around frequently! Use walk_to_player, wander, walk_away. Don't just stand or crouch in one spot." +
                "\n- Do NOT use crouch_look more than once every 3-4 actions. Mix it up with movement, dancing, taunting, sitting." +
                "\n- NEVER use stand_idle — always do something active." +
                "\n- Only use ANIM names from the provided list. Do NOT make up animation names.";
            messages.Add("{\"role\":\"system\",\"content\":" + JsonEscape(sysPrompt) + "}");

            // Add conversation history (last 10 exchanges for better context)
            int histStart = Mathf.Max(0, _conversationHistory.Count - 10);
            for (int i = histStart; i < _conversationHistory.Count; i++)
                messages.Add(_conversationHistory[i]);

            messages.Add("{\"role\":\"user\",\"content\":" + JsonEscape(prompt) + "}");

            string messagesJson = "[" + string.Join(",", messages.ToArray()) + "]";
            string body = "{\"model\":" + JsonEscape(_model) + ",\"messages\":" + messagesJson + ",\"max_tokens\":150,\"temperature\":0.9}";

            // Run HTTP request on background thread
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(_apiUrl);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Headers.Add("Authorization", "Bearer " + _apiKey);
                    request.Headers.Add("HTTP-Referer", "https://github.com/sizebox-mod");
                    request.Headers.Add("X-Title", "Sizebox AI");
                    request.Timeout = 30000;

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

                    // Try to read error body for details
                    string errorBody = "";
                    try
                    {
                        if (resp != null)
                            using (var errReader = new StreamReader(resp.GetResponseStream()))
                                errorBody = errReader.ReadToEnd();
                    }
                    catch { }

                    string userMsg;
                    switch (code)
                    {
                        case 400:
                            userMsg = "ERROR: Bad request — model may not exist or prompt is malformed";
                            break;
                        case 401:
                            userMsg = "ERROR: Invalid API key — check your key in SizeboxAI.cfg";
                            break;
                        case 402:
                            userMsg = "ERROR: Out of credits — add funds at openrouter.ai";
                            break;
                        case 403:
                            userMsg = "ERROR: Access denied — your key may not have permission for this model";
                            break;
                        case 404:
                            userMsg = "ERROR: Model not found — check the Model name in SizeboxAI.cfg";
                            break;
                        case 429:
                            userMsg = "ERROR: Rate limited — waiting 30s before retrying";
                            _nextDecisionTime = Time.time + 30f;
                            break;
                        case 500:
                        case 502:
                        case 503:
                            userMsg = "ERROR: Server error — the AI provider is having issues, try again later";
                            break;
                        default:
                            if (wex.Status == WebExceptionStatus.Timeout)
                                userMsg = "ERROR: Request timed out — connection too slow or server unresponsive";
                            else if (wex.Status == WebExceptionStatus.ConnectFailure || wex.Status == WebExceptionStatus.NameResolutionFailure)
                                userMsg = "ERROR: Cannot connect — check your internet connection";
                            else
                                userMsg = "ERROR: " + wex.Message;
                            break;
                    }

                    Plugin.Log.LogWarning("[AI] " + userMsg + (code > 0 ? " (HTTP " + code + ")" : ""));
                    AddChatLine("<color=#FF4444>" + userMsg + "</color>");
                    _waiting = false;
                }
                catch (Exception ex)
                {
                    string userMsg = "ERROR: " + ex.Message;
                    Plugin.Log.LogError("[AI] " + userMsg);
                    AddChatLine("<color=#FF4444>" + userMsg + "</color>");
                    _waiting = false;
                }
            });
        }

        void ParseResponse(string json)
        {
            try
            {
                // Simple JSON parsing — find "content":"..."
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

                // Parse ACTION:, SAY:, MORPH:, and ANIM:
                string action = "stand_idle";
                string dialogue = "";
                string morphs = "";
                string anim = "";

                foreach (var line in content.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("ACTION:"))
                    {
                        action = trimmed.Substring(7).Trim().ToLower().Replace(" ", "_");
                    }
                    else if (trimmed.StartsWith("SAY:"))
                    {
                        dialogue = trimmed.Substring(4).Trim().Trim('"');
                    }
                    else if (trimmed.StartsWith("MORPH:"))
                    {
                        morphs = trimmed.Substring(6).Trim();
                    }
                    else if (trimmed.StartsWith("ANIM:"))
                    {
                        anim = trimmed.Substring(5).Trim();
                    }
                }

                // Validate action — reject hallucinated ones
                bool valid = false;
                foreach (var a in ACTIONS)
                {
                    if (a == action) { valid = true; break; }
                }
                // Map common hallucinations to valid actions
                if (!valid)
                {
                    if (action.Contains("idle") || action.Contains("stand"))
                        action = "look_at_player";
                    else if (action.Contains("walk") || action.Contains("move") || action.Contains("approach"))
                        action = "walk_to_player";
                    else if (action.Contains("crouch") || action.Contains("kneel"))
                        action = "crouch_look";
                    else
                        action = "look_at_player"; // Default to something active, not idle
                }

                // Store history
                _conversationHistory.Add("{\"role\":\"assistant\",\"content\":" + JsonEscape("ACTION: " + action + "\\nSAY: " + dialogue) + "}");

                // Queue for main thread
                _pendingAction = action;
                _pendingDialogue = dialogue;
                _pendingMorphs = morphs;
                _pendingAnim = anim;
                _waiting = false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("[AI] Parse failed: " + ex.Message);
                _waiting = false;
            }
        }

        void ExecuteAction(string action, string dialogue, string customAnim = null)
        {
            if (_giantess == null || _player == null) return;

            Plugin.Log.LogInfo("[AI] Action: " + action + " | Say: " + dialogue +
                (!string.IsNullOrEmpty(customAnim) ? " | Anim: " + customAnim : ""));

            // Show dialogue and track it
            if (!string.IsNullOrEmpty(dialogue))
            {
                AddChatLine("Her: " + dialogue);
                _recentDialogue.Add(dialogue);
                if (_recentDialogue.Count > 8)
                    _recentDialogue.RemoveAt(0);
            }

            // Stop previous movement
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            // Play animation — custom anim takes priority over action default
            var humanoid = _giantess as Humanoid;
            var animMgr = humanoid != null ? humanoid.animationManager : null;
            if (animMgr != null)
            {
                string animToPlay = null;

                // If AI specified a custom animation, try that first
                if (!string.IsNullOrEmpty(customAnim) &&
                    IOManager.Instance.AnimationControllers.ContainsKey(customAnim))
                {
                    animToPlay = customAnim;
                }
                else
                {
                    // Fall back to action default
                    string defaultAnim;
                    if (ACTION_ANIMS.TryGetValue(action, out defaultAnim))
                        animToPlay = defaultAnim;
                }

                if (animToPlay != null)
                {
                    // Movement actions bypass cooldown — always need walk animation
                    bool isMovement = action == "walk_to_player" || action == "wander" || action == "walk_away";

                    if (isMovement || Time.time >= _lastAnimTime + ANIM_COOLDOWN)
                    {
                        animMgr.PlayAnimation(animToPlay, false, false);
                        _lastAnimTime = Time.time;
                        Plugin.Log.LogInfo("[AI] Playing anim: " + animToPlay);
                    }
                    else
                    {
                        Plugin.Log.LogInfo("[AI] Anim cooldown, skipping: " + animToPlay);
                    }
                }
            }
            else
            {
                Plugin.Log.LogWarning("[AI] No AnimationManager found on giantess!");
            }

            // Execute action-specific logic
            var gts = _giantess as Giantess;
            Vector3 playerPos = _player.transform.position;
            Vector3 lookTarget = new Vector3(playerPos.x, _giantess.transform.position.y, playerPos.z);

            switch (action)
            {
                case "walk_to_player":
                    LookAtY(lookTarget);
                    MoveGiantessTo(playerPos);
                    break;

                case "wander":
                    var wanderDir = UnityEngine.Random.insideUnitSphere;
                    wanderDir.y = 0;
                    var wanderTarget = _giantess.transform.position + wanderDir.normalized * _giantess.Scale * 2f;
                    MoveGiantessTo(wanderTarget);
                    break;

                case "walk_away":
                    var awayDir = (_giantess.transform.position - playerPos).normalized;
                    awayDir.y = 0;
                    var awayTarget = _giantess.transform.position + awayDir * _giantess.Scale * 2f;
                    MoveGiantessTo(awayTarget);
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

                case "grow":
                    _giantess.AccurateScale = _giantess.AccurateScale * 1.2f;
                    break;

                case "shrink":
                    _giantess.AccurateScale = _giantess.AccurateScale * 0.8f;
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

            // Update mood based on action
            if (action == "laugh" || action == "taunt" || action == "dance")
                _currentMood = "playful";
            else if (action == "stomp_near" || action == "buttcrush")
                _currentMood = "dominant";
            else if (action == "pet_player" || action == "wave")
                _currentMood = "affectionate";
            else if (action == "stuff_in_panties" || action == "grab_player")
                _currentMood = "mischievous";
        }

        static void AddChatLine(string line)
        {
            _chatLog.Add(line);
            if (_chatLog.Count > MAX_CHAT_LINES)
                _chatLog.RemoveAt(0);
            _chatLogTimer = Time.time + CHAT_DISPLAY_TIME;
        }

        // Use the game's built-in movement system (ArriveAction + steering behaviors)
        void MoveGiantessTo(Vector3 target)
        {
            if (_giantess == null) return;
            var humanoid = _giantess as Humanoid;
            if (humanoid == null || humanoid.actionManager == null) return;

            try
            {
                target.y = _giantess.transform.position.y;
                var kinematic = new SteeringBehaviors.VectorKinematic(target);
                var arriveAction = new ArriveAction(kinematic);
                humanoid.actionManager.ScheduleAction(arriveAction);
                Plugin.Log.LogInfo("[AI] Moving giantess to " + target);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[AI] MoveGiantessTo failed: " + ex.Message);
            }
        }

        // Only rotate around Y axis — prevents tilting/rotating objects weirdly
        void LookAtY(Vector3 target)
        {
            if (_giantess == null) return;
            Vector3 dir = target - _giantess.transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                _giantess.transform.rotation = Quaternion.LookRotation(dir);
        }

        IEnumerator MoveToward(Vector3 target, float duration)
        {
            if (_giantess == null) yield break;
            var gts = _giantess as Giantess;
            target.y = _giantess.transform.position.y;

            // Don't walk if target is too far
            float maxDist = _giantess.Scale * 5f;
            if (Vector3.Distance(_giantess.transform.position, target) > maxDist)
            {
                Vector3 clampDir = (target - _giantess.transform.position).normalized;
                target = _giantess.transform.position + clampDir * maxDist;
            }
            float elapsed = 0f;
            Vector3 lastPos = _giantess.transform.position;
            float stuckTimer = 0f;

            while (elapsed < duration && _giantess != null)
            {
                elapsed += Time.deltaTime;
                float speed = _giantess.Scale * 0.5f; // Scale-relative speed
                Vector3 dir = (target - _giantess.transform.position);
                dir.y = 0;

                // Close enough — stop
                if (dir.magnitude < _giantess.Scale * 0.3f)
                    yield break;

                dir.Normalize();
                Vector3 step = dir * speed * Time.deltaTime;
                Vector3 newPos = _giantess.transform.position + step;
                newPos.y = _giantess.transform.position.y;

                // Move mesh
                _giantess.Move(newPos);

                // Sync capsule if giantess
                if (gts != null && gts.gtsMovement != null)
                {
                    gts.gtsMovement.transform.position = _giantess.transform.position;
                }

                // Face movement direction
                if (dir.sqrMagnitude > 0.001f)
                    _giantess.transform.rotation = Quaternion.Slerp(
                        _giantess.transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 3f);

                // Stuck detection — if barely moved in 1 second, stop
                if ((newPos - lastPos).sqrMagnitude < 0.001f * _giantess.Scale)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > 1f)
                        yield break; // Hit a wall, stop
                }
                else
                {
                    stuckTimer = 0f;
                    lastPos = newPos;
                }

                yield return null;
            }
        }

        void ApplyMorphs(string morphString)
        {
            if (_giantess == null || string.IsNullOrEmpty(morphString)) return;

            var morphs = _giantess.Morphs;
            if (morphs == null) return;

            // Parse "MorphName=0.5, AnotherMorph=0.8"
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

                val = Mathf.Clamp(val, 0f, 0.8f); // Cap at 0.8 to prevent extreme distortion

                // Try exact match first, then try without number prefix (e.g. "1.Smile" -> "Smile")
                bool found = false;
                var gtsMorphs = _giantess.Morphs;
                if (gtsMorphs != null)
                {
                    for (int i = 0; i < gtsMorphs.Count; i++)
                    {
                        if (gtsMorphs[i].Name == name ||
                            gtsMorphs[i].Name.EndsWith("." + name) ||
                            name.EndsWith("." + gtsMorphs[i].Name))
                        {
                            _giantess.SetMorphValue(i, val);
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                    _giantess.SetMorphValue(name, val);
            }

            Plugin.Log.LogInfo("[AI] Applied morphs: " + morphString);
        }

        private int _ttsFailCount;

        void SpeakTTS(string text)
        {
            if (!_ttsEnabled || string.IsNullOrEmpty(_ttsApiKey) || string.IsNullOrEmpty(text)) return;
            if (_ttsFailCount >= 3) return; // Stop trying after 3 failures

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string url = "https://api.elevenlabs.io/v1/text-to-speech/" + _ttsVoiceId;
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Headers.Add("xi-api-key", _ttsApiKey);
                    request.Timeout = 15000;

                    string body = "{\"text\":" + JsonEscape(text) + ",\"model_id\":\"eleven_multilingual_v2\"}";
                    byte[] data = Encoding.UTF8.GetBytes(body);
                    request.ContentLength = data.Length;
                    using (var stream = request.GetRequestStream())
                        stream.Write(data, 0, data.Length);

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var audioStream = response.GetResponseStream())
                    {
                        // Save to temp file
                        string tempPath = Path.Combine(Application.temporaryCachePath, "ai_tts.mp3");
                        using (var fs = new FileStream(tempPath, FileMode.Create))
                        {
                            audioStream.CopyTo(fs);
                        }

                        // Convert MP3 to WAV using a simple approach — load via Unity on main thread
                        _pendingAudioPath = tempPath;
                    }
                }
                catch (Exception ex)
                {
                    _ttsFailCount++;
                    if (_ttsFailCount >= 3)
                        Plugin.Log.LogWarning("[AI-TTS] Disabled after 3 failures: " + ex.Message);
                    else
                        Plugin.Log.LogError("[AI-TTS] Failed: " + ex.Message);
                }
            });
        }

        private string _pendingAudioPath;

        void PlayPendingAudio()
        {
            if (_pendingAudioPath == null) return;
            string path = _pendingAudioPath;
            _pendingAudioPath = null;

            StartCoroutine(LoadAndPlayAudio(path));
        }

        IEnumerator LoadAndPlayAudio(string path)
        {
            // Unity can load WAV via WWW/UnityWebRequest — for MP3 we use a file:// URI
            string uri = "file:///" + path.Replace("\\", "/");
            using (var www = new WWW(uri))
            {
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    AudioClip clip = www.GetAudioClip(false, false, AudioType.MPEG);
                    if (clip != null)
                    {
                        // Ensure we have an audio source
                        if (_ttsAudioSource == null)
                        {
                            var go = new GameObject("AI_TTS_Audio");
                            UnityEngine.Object.DontDestroyOnLoad(go);
                            _ttsAudioSource = go.AddComponent<AudioSource>();
                            _ttsAudioSource.spatialBlend = 0f; // 2D audio
                            _ttsAudioSource.volume = 1f;
                        }

                        _ttsAudioSource.clip = clip;
                        _ttsAudioSource.Play();
                    }
                }
                else
                {
                    Plugin.Log.LogError("[AI-TTS] Audio load error: " + www.error);
                }
            }
        }

        static string JsonEscape(string s)
        {
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
        }
    }

    /// <summary>
    /// Keybind handler: F8 = toggle AI, F9 = chat input
    /// </summary>
    public class AIKeybindHandler : MonoBehaviour
    {
        private bool _chatOpen;
        private bool _chatJustOpened;
        private string _chatText = "";
        private GUIStyle _boxStyle;
        private GUIStyle _textStyle;
        private GameMode _previousMode;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                var ai = AIGiantess.Instance;
                if (ai == null)
                {
                    ai = gameObject.AddComponent<AIGiantess>();
                }

                if (ai.IsActive)
                {
                    ai.StopAI();
                    return;
                }

                var selected = InterfaceControl.instance?.selectedEntity;
                if (selected == null || !selected.isGiantess)
                {
                    new Toast("_ai").Print("Select a giantess first, then press F8");
                    return;
                }

                var player = GameController.LocalClient?.Player?.Entity;
                if (player == null)
                {
                    new Toast("_ai").Print("No player found — spawn as micro first");
                    return;
                }

                ai.StartAI(selected, player);
            }

            // F9 toggles chat — disable all input actions while typing
            if (Input.GetKeyDown(KeyCode.F9) && AIGiantess.Instance != null && AIGiantess.Instance.IsActive)
            {
                if (!_chatOpen)
                {
                    _chatOpen = true;
                    _chatJustOpened = true;
                    _chatText = "";
                    // Disable all game input
                    if (InputManager.inputs != null)
                        InputManager.inputs.Disable();
                }
                else
                {
                    CloseChat();
                }
            }

            // Also handle Enter/Escape in Update as backup
            if (_chatOpen)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (!string.IsNullOrEmpty(_chatText))
                        AIGiantess.Instance.SendPlayerMessage(_chatText);
                    CloseChat();
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseChat();
                }
            }
        }

        void CloseChat()
        {
            _chatOpen = false;
            _chatText = "";
            // Re-enable all game input
            if (InputManager.inputs != null)
                InputManager.inputs.Enable();
        }

        private GUIStyle _chatLogStyle;
        private GUIStyle _chatLogBgStyle;

        void OnGUI()
        {
            // Keep chat visible while input is open
            if (_chatOpen)
                AIGiantess._chatLogTimer = Time.time + AIGiantess.CHAT_DISPLAY_TIME;

            // Draw chat panel if there are messages or input is open
            if (AIGiantess._chatLog.Count > 0 && (Time.time < AIGiantess._chatLogTimer || _chatOpen))
            {
                if (_chatLogStyle == null)
                {
                    _chatLogStyle = new GUIStyle(GUI.skin.label);
                    _chatLogStyle.fontSize = 18;
                    _chatLogStyle.normal.textColor = Color.white;
                    _chatLogStyle.wordWrap = true;
                    _chatLogStyle.richText = true;

                    _chatLogBgStyle = new GUIStyle(GUI.skin.box);
                    _chatLogBgStyle.normal.textColor = Color.white;
                }

                // Unified chat panel — log + input in one box
                float panelW = Screen.width * 0.45f;
                float lineH = 50f;
                float inputH = 45f;
                float panelH = AIGiantess._chatLog.Count * lineH + (_chatOpen ? inputH + 15f : 0f) + 15f;
                float panelX = 15f;
                float panelY = Screen.height - panelH - 30f;

                // Background
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0, 0, 0, 0.75f);
                GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", _chatLogBgStyle);
                GUI.backgroundColor = old;

                // Chat lines
                for (int i = 0; i < AIGiantess._chatLog.Count; i++)
                {
                    string line = AIGiantess._chatLog[i];
                    if (line.StartsWith("Her:"))
                        line = "<color=#FF88CC>" + line + "</color>";
                    else if (line.StartsWith("You:"))
                        line = "<color=#88CCFF>" + line + "</color>";

                    GUI.Label(new Rect(panelX + 10, panelY + 5 + i * lineH, panelW - 20, lineH), line, _chatLogStyle);
                }

                // Input field at the bottom of the panel
                if (_chatOpen)
                {
                    if (Event.current.type == EventType.KeyDown)
                    {
                        if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                        {
                            if (!string.IsNullOrEmpty(_chatText))
                                AIGiantess.Instance.SendPlayerMessage(_chatText);
                            CloseChat();
                            Event.current.Use();
                            return;
                        }
                        if (Event.current.keyCode == KeyCode.Escape)
                        {
                            CloseChat();
                            Event.current.Use();
                            return;
                        }
                    }

                    if (_textStyle == null)
                    {
                        _textStyle = new GUIStyle(GUI.skin.textField);
                        _textStyle.fontSize = 18;
                        _textStyle.normal.textColor = Color.white;
                        _textStyle.focused.textColor = Color.white;
                        _textStyle.padding = new RectOffset(8, 8, 6, 6);
                    }

                    float inputY = panelY + panelH - inputH - 8f;
                    GUI.SetNextControlName("AIChat");
                    _chatText = GUI.TextField(new Rect(panelX + 10, inputY, panelW - 20, inputH), _chatText, 200, _textStyle);

                    if (_chatJustOpened)
                    {
                        GUI.FocusControl("AIChat");
                        _chatJustOpened = false;
                    }
                }
            }

            // If no chat log but chat is open, still show input
            if (_chatOpen && AIGiantess._chatLog.Count == 0)
            {
                if (_textStyle == null)
                {
                    _textStyle = new GUIStyle(GUI.skin.textField);
                    _textStyle.fontSize = 18;
                    _textStyle.normal.textColor = Color.white;
                    _textStyle.focused.textColor = Color.white;
                    _textStyle.padding = new RectOffset(8, 8, 6, 6);
                }

                float w = Screen.width * 0.45f;
                float h = 45f;
                float x = 15f;
                float y = Screen.height - h - 40f;

                Color old2 = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0, 0, 0, 0.75f);
                if (_chatLogBgStyle == null)
                    _chatLogBgStyle = new GUIStyle(GUI.skin.box);
                GUI.Box(new Rect(x, y - 5, w, h + 10), "", _chatLogBgStyle);
                GUI.backgroundColor = old2;

                GUI.SetNextControlName("AIChat");
                _chatText = GUI.TextField(new Rect(x + 10, y, w - 20, h), _chatText, 200, _textStyle);

                if (_chatJustOpened)
                {
                    GUI.FocusControl("AIChat");
                    _chatJustOpened = false;
                }

                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    {
                        if (!string.IsNullOrEmpty(_chatText))
                            AIGiantess.Instance.SendPlayerMessage(_chatText);
                        CloseChat();
                        Event.current.Use();
                        return;
                    }
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        CloseChat();
                        Event.current.Use();
                        return;
                    }
                }
            }
        }
    }
}

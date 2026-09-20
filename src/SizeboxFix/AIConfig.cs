using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;

namespace SizeboxFix
{
    /// <summary>
    /// Shared settings across all AI agents (API key, model, etc.)
    /// </summary>
    public class SharedConfig
    {
        public string ApiKey = "";
        public string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";
        public string Model = "";
        public float DecisionInterval = 5f;

        // Keybinds. These were hard-coded; F11 collided with the Steam/Windows
        // screenshot key, which users understandably did not want to rebind after
        // years of muscle memory. The conversation shortcuts now default to F7 and
        // every binding can be changed in the settings panel or this file.
        public KeyCode KeyToggleAI = KeyCode.F8;
        public KeyCode KeyChat = KeyCode.F9;
        public KeyCode KeyChatAlt = KeyCode.T;
        public KeyCode KeyTTSInput = KeyCode.F10;
        public KeyCode KeySaveConversation = KeyCode.F7; // Shift+this clears instead
        public KeyCode KeyMute = KeyCode.Backslash;
        public KeyCode KeyLockAnim = KeyCode.L;
    }

    /// <summary>
    /// Per-character AI config (personality, voice, etc.)
    /// </summary>
    public class AgentConfig
    {
        public string Name = "Default";
        public string Personality = "";

        // TTS
        public string TtsProvider = "edge";
        public bool TtsEnabled = false;
        public string TtsEdgeVoice = "en-US-AriaNeural";
        public string TtsApiKey = "";
        public string TtsVoiceId = "";
        public string TtsFishApiKey = "";
        public string TtsFishModelId = "";

        public AgentConfig Clone()
        {
            return new AgentConfig
            {
                Name = Name,
                Personality = Personality,
                TtsProvider = TtsProvider,
                TtsEnabled = TtsEnabled,
                TtsEdgeVoice = TtsEdgeVoice,
                TtsApiKey = TtsApiKey,
                TtsVoiceId = TtsVoiceId,
                TtsFishApiKey = TtsFishApiKey,
                TtsFishModelId = TtsFishModelId
            };
        }
    }

    /// <summary>
    /// Parses and saves section-based SizeboxAI.cfg
    /// Supports both legacy flat format and new [Section] format.
    /// </summary>
    public static class AIConfigManager
    {
        public static string ConfigPath => Path.Combine(Paths.ConfigPath, "SizeboxAI.cfg");

        public static void Load(out SharedConfig shared, out List<AgentConfig> agents)
        {
            shared = new SharedConfig();
            agents = new List<AgentConfig>();
            var defaultAgent = new AgentConfig { Name = "Default" };

            if (!File.Exists(ConfigPath))
            {
                CreateDefaultConfig();
                agents.Add(defaultAgent);
                return;
            }

            AgentConfig currentAgent = defaultAgent;
            bool hasNamedSections = false;

            foreach (var line in File.ReadAllLines(ConfigPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Section header
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    string sectionName = trimmed.Substring(1, trimmed.Length - 2).Trim();

                    if (sectionName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                    {
                        currentAgent = defaultAgent;
                    }
                    else
                    {
                        hasNamedSections = true;
                        // Create new agent config inheriting defaults
                        currentAgent = defaultAgent.Clone();
                        currentAgent.Name = sectionName;
                        agents.Add(currentAgent);
                    }
                    continue;
                }

                if (!trimmed.Contains("=")) continue;
                var parts = trimmed.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var val = parts[1].Trim();

                // Shared settings (only from Default section)
                switch (key)
                {
                    case "ApiKey": shared.ApiKey = val; break;
                    case "ApiUrl": shared.ApiUrl = val; break;
                    case "Model": shared.Model = val; break;
                    case "DecisionInterval":
                        float.TryParse(val, out shared.DecisionInterval);
                        break;
                    case "KeyToggleAI": shared.KeyToggleAI = ParseKey(val, shared.KeyToggleAI); break;
                    case "KeyChat": shared.KeyChat = ParseKey(val, shared.KeyChat); break;
                    case "KeyChatAlt": shared.KeyChatAlt = ParseKey(val, shared.KeyChatAlt); break;
                    case "KeyTTSInput": shared.KeyTTSInput = ParseKey(val, shared.KeyTTSInput); break;
                    case "KeySaveConversation": shared.KeySaveConversation = ParseKey(val, shared.KeySaveConversation); break;
                    case "KeyMute": shared.KeyMute = ParseKey(val, shared.KeyMute); break;
                    case "KeyLockAnim": shared.KeyLockAnim = ParseKey(val, shared.KeyLockAnim); break;
                }

                // Per-agent settings
                switch (key)
                {
                    case "Personality": currentAgent.Personality = val; break;
                    case "TTSProvider": currentAgent.TtsProvider = val.ToLower(); break;
                    case "TTSEnabled": currentAgent.TtsEnabled = val.ToLower() == "true"; break;
                    case "TTSEdgeVoice": currentAgent.TtsEdgeVoice = val; break;
                    case "TTSApiKey": currentAgent.TtsApiKey = val; break;
                    case "TTSVoiceId": currentAgent.TtsVoiceId = val; break;
                    case "TTSFishApiKey": currentAgent.TtsFishApiKey = val; break;
                    case "TTSFishModelId": currentAgent.TtsFishModelId = val; break;
                }
            }

            // If no named sections, the default agent is the only one
            if (!hasNamedSections)
                agents.Add(defaultAgent);
        }

        /// <summary>
        /// Parses a KeyCode name, keeping the current value if the text is not a
        /// valid key so a typo in the cfg cannot leave an action unbound.
        /// </summary>
        static KeyCode ParseKey(string val, KeyCode fallback)
        {
            if (string.IsNullOrEmpty(val)) return fallback;
            try { return (KeyCode)Enum.Parse(typeof(KeyCode), val.Trim(), true); }
            catch { return fallback; }
        }

        public static void Save(SharedConfig shared, List<AgentConfig> agents)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Sizebox AI Configuration");
            sb.AppendLine("# Use [SectionName] to define multiple characters");
            sb.AppendLine("");

            // Default section — shared + default agent settings
            sb.AppendLine("[Default]");
            sb.AppendLine("ApiKey=" + shared.ApiKey);
            sb.AppendLine("ApiUrl=" + shared.ApiUrl);
            sb.AppendLine("Model=" + shared.Model);
            sb.AppendLine("DecisionInterval=" + shared.DecisionInterval);
            sb.AppendLine("");
            sb.AppendLine("# Keybinds - any Unity KeyCode name (F7, Backslash, T, Insert, ...)");
            sb.AppendLine("# Shift + KeySaveConversation clears the conversation instead of saving it.");
            sb.AppendLine("KeyToggleAI=" + shared.KeyToggleAI);
            sb.AppendLine("KeyChat=" + shared.KeyChat);
            sb.AppendLine("KeyChatAlt=" + shared.KeyChatAlt);
            sb.AppendLine("KeyTTSInput=" + shared.KeyTTSInput);
            sb.AppendLine("KeySaveConversation=" + shared.KeySaveConversation);
            sb.AppendLine("KeyMute=" + shared.KeyMute);
            sb.AppendLine("KeyLockAnim=" + shared.KeyLockAnim);
            sb.AppendLine("");

            // Write default agent TTS settings if there's only one agent
            if (agents.Count == 1 && agents[0].Name == "Default")
            {
                var a = agents[0];
                sb.AppendLine("Personality=" + a.Personality);
                sb.AppendLine("TTSProvider=" + a.TtsProvider);
                sb.AppendLine("TTSEnabled=" + a.TtsEnabled.ToString().ToLower());
                sb.AppendLine("TTSEdgeVoice=" + a.TtsEdgeVoice);
                sb.AppendLine("TTSFishApiKey=" + a.TtsFishApiKey);
                sb.AppendLine("TTSFishModelId=" + a.TtsFishModelId);
                sb.AppendLine("TTSApiKey=" + a.TtsApiKey);
                sb.AppendLine("TTSVoiceId=" + a.TtsVoiceId);
            }
            else
            {
                // Write default TTS as fallback
                if (agents.Count > 0)
                {
                    var def = agents[0];
                    sb.AppendLine("TTSProvider=" + def.TtsProvider);
                    sb.AppendLine("TTSEnabled=" + def.TtsEnabled.ToString().ToLower());
                    sb.AppendLine("TTSEdgeVoice=" + def.TtsEdgeVoice);
                    sb.AppendLine("TTSFishApiKey=" + def.TtsFishApiKey);
                }

                // Named agent sections
                foreach (var a in agents)
                {
                    if (a.Name == "Default") continue;
                    sb.AppendLine("");
                    sb.AppendLine("[" + a.Name + "]");
                    sb.AppendLine("Personality=" + a.Personality);
                    if (!string.IsNullOrEmpty(a.TtsFishModelId))
                        sb.AppendLine("TTSFishModelId=" + a.TtsFishModelId);
                    if (a.TtsProvider != "edge") // only write if non-default
                        sb.AppendLine("TTSProvider=" + a.TtsProvider);
                    if (!string.IsNullOrEmpty(a.TtsVoiceId) && a.TtsVoiceId != "")
                        sb.AppendLine("TTSVoiceId=" + a.TtsVoiceId);
                }
            }

            File.WriteAllText(ConfigPath, sb.ToString());
        }

        static void CreateDefaultConfig()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Sizebox AI Configuration");
            sb.AppendLine("# Use [SectionName] to create multiple character presets");
            sb.AppendLine("# Select a giantess and press F8 to choose a preset");
            sb.AppendLine("");
            sb.AppendLine("[Default]");
            sb.AppendLine("ApiKey=YOUR_KEY_HERE");
            sb.AppendLine("ApiUrl=https://openrouter.ai/api/v1/chat/completions");
            sb.AppendLine("# Enter a model ID supported by your chosen provider");
            sb.AppendLine("Model=");
            sb.AppendLine("DecisionInterval=5");
            sb.AppendLine("Personality=");
            sb.AppendLine("TTSProvider=edge");
            sb.AppendLine("TTSEnabled=false");
            sb.AppendLine("TTSEdgeVoice=en-US-AriaNeural");
            sb.AppendLine("TTSFishApiKey=");
            sb.AppendLine("TTSFishModelId=");
            sb.AppendLine("TTSApiKey=");
            sb.AppendLine("TTSVoiceId=");
            File.WriteAllText(ConfigPath, sb.ToString());
        }
    }
}

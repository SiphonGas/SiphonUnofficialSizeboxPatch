# Siphon's Unofficial Sizebox Patch

Unofficial bug fixes, improvements, and experimental features for Sizebox v3.01.

**Current source version: 1.5.0.** This README describes the code on `main`. Older
release downloads may not include the AI Settings menu, multiple AI characters, or
the **T** chat shortcut. Check the notes for the DLL you download.

[Downloads](https://github.com/SiphonGas/SiphonUnofficialSizeboxPatch/releases) |
[Report a bug](https://github.com/SiphonGas/SiphonUnofficialSizeboxPatch/issues)

The core bug fixes work without an API key. AI and text-to-speech are optional.

## About this project

This is an unofficial, personal project by Siphon. I am not a Sizebox developer,
though I am in contact with them. The mod is provided as-is, without warranty.
Back up your game files and saves before installing or updating it.

## Bug fixes and improvements

The plugin includes patches for:

- **Physics instability:** guards against invalid DynamicBone positions, zero-scale
  body physics, invalid mesh collider updates, and invalid footstep pitch or volume.
- **Movement and scaling:** reduces position drift and rotation jitter, clears residual
  movement velocity, and stabilizes idle rigidbodies and small-scale player movement.
- **Ground collision:** keeps terrain collision active at large scales and temporarily
  pauses gravity during scene loading.
- **Saving and loading:** adds null guards around character saving and player setup;
  clears existing entities before loading a saved scene through the added Load button.
- **Editor controls:** synchronizes the selection gizmo and translates Japanese morph names.
- **Morphs and rendering:** protects user-set morph values from blinking, updates skinned
  meshes, and includes an MMD face-rendering patch.
- **Memory cleanup:** removes destroyed entities from ObjectManager and clears dead event listeners.
- **City performance:** caches building renderers and uses distance-based culling.

Compatibility with every Sizebox build is not established. Report your exact game
build when a patch fails or the log reports missing game types or methods.

## Features

### Character editing

- **Hide Bone / Show Bone:** hides a bone by reducing its scale and restores its previous scale.
- **Delete Bone Mesh:** removes renderers and colliders from the selected bone hierarchy;
  Show Bone does not undo deletion.
- **Save Hidden Bones / Load Hidden Bones:** stores and reapplies hidden-bone names.
- **Morph presets:** saves and loads morph settings as `morphs.json` in the character's save folder.
- **Pause-menu Load button:** loads a saved scene without returning to the main menu.

### Lua behaviors

Optional Lua behaviors include **Buttcrush** and **Stuff In Panties** under the
Interaction menu. Install the behavior scripts separately as described below.

### AI characters (experimental)

- Multiple AI characters with separate names, personalities, and voice settings.
- **Pause menu > AI Settings** for configuration and locally saved custom scenarios.
- **F8** to assign a preset to the selected giantess or deactivate her AI.
- **T or F9** to open chat after activating an AI character.
- Optional speech through Edge TTS, ElevenLabs, or Fish Audio, with lip sync.
- Local conversation save and clear controls.

No personal character presets, API credentials, or custom voice IDs are bundled.
Users configure their own providers and characters.

## Installation

### Requirements

- Sizebox v3.01 on Windows, x64.
- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) for **Unity Mono, x64**.

### Steps

1. **Install BepInEx:** extract it into the game folder, alongside `Sizebox.exe`.
   Start the game once to generate its folders, then close it.
2. **Install the plugin:** download it from [Releases](https://github.com/SiphonGas/SiphonUnofficialSizeboxPatch/releases),
   extract the ZIP if supplied, and copy `SizeboxFix.dll` to `BepInEx/plugins/`.
3. **Install Lua behaviors (optional):** copy the required `.lua` files from this
   repository's `Sizebox v3.01 - Win64 (Primary)/Sizebox_Data/StreamingAssets/lua/behaviors/`
   into the corresponding folder in your game. Follow any additional release instructions.
4. **Check startup:** run the game and inspect `BepInEx/LogOutput.log` for
   `Loading [Sizebox Fix ...]` and subsequent errors. Version 1.5.0 also displays its
   version on the main menu.

### Updating

Close the game, back up your existing plugin and configuration, and replace
`BepInEx/plugins/SizeboxFix.dll`. Keep only one copy of this plugin under
`BepInEx/plugins/`, including subfolders. Check the release notes for Lua updates.

Updating the source repository does not update your installed DLL. To use source
changes before a packaged release is available, see [Building from source](#building-from-source).

## AI setup (optional)

1. Launch the game with the plugin installed, then close it. This creates
   `BepInEx/config/SizeboxAI.cfg`.
2. Set `ApiKey`, `ApiUrl`, and `Model` for your chosen chat provider. The default
   endpoint is OpenRouter; other compatible chat-completions endpoints can be configured.
3. Enter your own `Personality`. Leave `TTSEnabled=false` while testing chat.
4. Restart the game, load a map, and **spawn yourself as a micro**.
5. Spawn and **select a giantess**, then press **F8** and choose a preset.
   Repeat for additional characters.
6. Press **T or F9**, type a message, and press **Enter** to send it.

F8 activates or deactivates AI. To edit settings, open **Pause menu > AI Settings**.
The API endpoint and key are configured in the file; the menu provides model,
character, voice, and conversation controls.

### Configuration example

Replace the placeholders and supply your own personality before use:

```ini
[Default]
ApiKey=YOUR_KEY_HERE
ApiUrl=https://openrouter.ai/api/v1/chat/completions
Model=YOUR_MODEL_ID
DecisionInterval=5
Personality=
TTSProvider=edge
TTSEnabled=false
TTSEdgeVoice=en-US-AriaNeural
TTSApiKey=
TTSVoiceId=
TTSFishApiKey=
TTSFishModelId=
```

For multiple characters, add sections such as `[Character 1]` and `[Character 2]`
with your own `Personality` and voice settings. Put `[Default]` first so the named
sections inherit its values. Put shared API settings in `[Default]`.

When named sections exist, `[Default]` supplies defaults and is not listed as a
character preset. With no named sections, the default preset is available on its own.

### Optional text-to-speech

Set `TTSProvider` and the corresponding fields, then enable `TTSEnabled`:

| Provider | Configuration |
|----------|---------------|
| `edge` | Set `TTSEdgeVoice`. The plugin launches `python3 -m edge_tts`, so that command must work on your machine. |
| `elevenlabs` | Supply your own `TTSApiKey` and `TTSVoiceId`. |
| `fish` | Supply your own `TTSFishApiKey` and `TTSFishModelId`. |

Chat and voice use separate provider settings. An OpenRouter key is not a
voice-provider key. Keep speech disabled until chat is working.

### AI controls

| Control | Action |
|---------|--------|
| Pause menu > AI Settings | Edit settings and local scenarios |
| F8 | Assign a preset to the selected giantess, or deactivate her AI |
| T or F9 | Open chat while AI is active |
| Enter | Send text from the open chat box |
| Esc | Close chat |
| F10 | Open manual speech input while AI is active |
| L | Toggle animation lock for the selected AI character |
| Backslash | Mute/unmute the selected AI character |
| F11 | Save conversation locally |
| Shift+F11 | Clear conversation and its local saved history |

## Troubleshooting

| Problem | What to check |
|---------|---------------|
| No plugin menus or controls | Confirm the DLL location and inspect `BepInEx/LogOutput.log` for loading and startup errors. |
| F8 does nothing | Spawn as a micro and select a giantess first. Both a player entity and selected giantess are required. |
| F9 or T does nothing | Activate AI with F8 first. Older builds may support F9 only. |
| Enter does nothing | Open chat first; Enter sends text rather than opening the interface. |
| Chat opens but no reply arrives | Check `ApiKey`, `ApiUrl`, and `Model`, then inspect the log for request errors. |
| Chat works but speech does not | Check `TTSEnabled`, voice-provider settings, and any required dependencies. |
| Missing game types or methods | Report your exact Sizebox build and plugin download; these messages may indicate a compatibility problem. |

An API response is not required to open chat. Changing models or purchasing credits
is not a fix for an interface that never opens.

When [reporting a bug](https://github.com/SiphonGas/SiphonUnofficialSizeboxPatch/issues), include:

- The exact Sizebox version and plugin ZIP or DLL version.
- Reproduction steps and the expected behavior.
- A redacted `BepInEx/LogOutput.log` captured after reproducing the problem.

Some older plugin builds retained an outdated `1.3.0` label, so include the download
filename as well as the version reported in the log.

### Private data

Configuration, scenarios, conversation history, and logs can contain private
information. These local files are excluded from this repository. Do not include
API keys, personal scenarios, or chat history in shared ZIPs or bug reports.

## Building from source

Requirements: a .NET SDK capable of building `net472`, BepInEx core DLLs, and the
Sizebox game DLLs referenced by `src/SizeboxFix/SizeboxFix.csproj`.

From the repository root:

```powershell
dotnet build src/SizeboxFix/SizeboxFix.csproj -c Release
```

Output: `src/SizeboxFix/bin/Release/net472/SizeboxFix.dll`.

The project references DLLs inside the adjacent `Sizebox v3.01 - Win64 (Primary)`
folder. Adjust the project's `HintPath` entries if your game is elsewhere. Close
the game before copying the built DLL into its `BepInEx/plugins/` folder.

A successful build checks compilation. Verify changes in-game before distributing
a build to other users.

The patch source lives in `src/SizeboxFix/`. `Sizebox_SourecCode/` contains game
scripts for reference, not a complete Unity project that can be rebuilt as-is.

## Credits

Made by Siphon for the Sizebox community.

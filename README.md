# Siphon's Unofficial Sizebox Patch

> Unofficial bug fixes and experimental features for Sizebox.

A BepInEx plugin for Sizebox v3.01 that fixes critical bugs and adds new features.

## Disclaimer

This is an **unofficial, personal project**. I am not a developer of Sizebox though I am in contact with them. This mod is provided as-is with no warranty. Use at your own risk. I am not responsible for any issues, crashes, broken saves, corrupted installations, or anything else that may result from using this plugin. Always back up your game files before installing mods.

## Bug Fixes

- **DynamicBone NaN Guard** - Prevents the "model freakout" bug where models start shaking violently with loud noise. Detects NaN/Infinity values in DynamicBone particle positions and resets them before they cascade.
- **BodyPhysics Zero-Scale Guard** - Prevents hair, jiggle, and breast physics from exploding when model scale approaches zero. Guards all `lossyScale.y` divisions in SetHairPhysics, SetJigglePhysics, PlaceTorsoCollider, and BreastGrowth.
- **ColliderReshaper Scale Guard** - Skips mesh collision updates when giantess scale is invalid, preventing vertex corruption.
- **SoundManager NaN Guard** - Blocks footstep sounds when entity Scale/Height is NaN/zero, preventing audio pitch corruption.
- **Scale Snap Fix** - Fixes float.Epsilon scale comparison causing constant re-snapping in GTSMovement.
- **Position Sync Rewrite** - Fixes MoveTransformToCapsule lerp causing gravity-driven Y drift.
- **Terrain Collision Fix** - Prevents terrain collision from being disabled at large scales (models falling through the map).
- **Save Crash Fixes** - Null checks for GetTransformKey, DynamicBoneData exclusions, and CharacterEditor.Save safety net.
- **Handle Gizmo Sync** - Syncs handle target to currently selected entity.
- **ChangeScale Position Fix** - Prevents floating point position drift during SetParent operations.
- **Blink Morph Fix** - Prevents blink coroutine from overwriting user-set morph values.
- **ObjectManager Memory Leak Fix** - The game's `_OnObjectRemoved()` had a copy-paste bug that re-added destroyed entities to the dictionary instead of removing them. Every destroyed micro and object leaked in memory forever, causing progressive slowdown during long sessions. Now properly cleans up.
- **EventManager Dead Listener Cleanup** - Null event listeners were logged but never removed, accumulating forever. Now cleaned up automatically during event dispatch.
- **Scene Loader Entity Clear** - Loading a save from the pause menu now clears all existing entities first, preventing duplicates and ghost objects.

## Features

### Bone Hide/Show/Delete
Adds buttons to the skeleton edit panel for hiding, showing, and deleting individual bone meshes. Useful for removing unwanted accessories, clothing pieces, or clipping geometry on models.
- **Hide Bone** — Scales the bone to zero (reversible)
- **Show Bone** — Restores hidden bones to original scale
- **Delete Bone Mesh** — Permanently removes renderers attached to the bone

### Morph Preset Save/Load
Save and load morph configurations per model. Buttons appear in the Morphs panel.
- Saves to the model's character folder as `morphs.json`
- Toast notifications for feedback
- Works across sessions

### Load Button in Pause Menu
Adds a "Load" button to the pause menu so you can load saved scenes without going back to the main menu. Correctly switches maps if the save is from a different scene.

### Japanese Morph Name Translation
Translates Japanese MMD morph names to English for easier use.

### Lua Behaviors
- **Buttcrush** (`Interaction > Buttcrush`) - GTS walks to target, sits down, crushes, and taunts. Supports ground pound mode.
- **Stuff in Panties** (`Interaction > Stuff In Panties`) - GTS grabs target and carries them at hip position while walking around.

### AI Characters (Experimental)

- Control multiple characters with separate names, personalities, and voices.
- Open **AI Settings** from the pause menu to edit settings and save custom scenarios locally.
- Select a giantess and press **F8** to choose a personality preset or deactivate her AI.
- Press **T or F9** to open chat after activating a character.
- Optional speech through Edge TTS, ElevenLabs, or Fish Audio, with lip sync.
- Save conversation history locally with **F11**; clear it with **Shift+F11**.

The public plugin includes no personal presets, API credentials, or custom voice IDs.
Users supply their own provider settings. AI remains experimental; compilation does not
establish compatibility with every Sizebox build.

## Installation

### Requirements
- Sizebox v3.01
- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) (Unity Mono, x64)

### Steps

1. **Install BepInEx** (if you haven't already):
   - Download BepInEx 5.4.x for Unity Mono x64
   - Extract into your Sizebox game folder (where `Sizebox.exe` is)
   - Run the game once to generate BepInEx folders, then close it

2. **Install the plugin**:
   - Download `SizeboxFix.dll` from the [Releases](../../releases) page
   - Copy it to `BepInEx/plugins/`

### Updating
When a new version is released, just download the new `SizeboxFix.dll` and replace the old one in `BepInEx/plugins/`. That's it — no other steps needed.

3. **Install Lua behaviors** (optional):
   - Copy the `.lua` files from `Sizebox v3.01 - Win64 (Primary)/Sizebox_Data/StreamingAssets/lua/behaviors/` to the same path in your game folder

4. **Set up AI** (optional):
   - Launch the game once to create `BepInEx/config/SizeboxAI.cfg`, then close it.
   - Set `ApiKey`, `ApiUrl`, and `Model` for your chosen chat provider. The default endpoint is OpenRouter; other compatible chat-completions endpoints can be configured.
   - Enter your own `Personality`. Keep `TTSEnabled=false` until chat works.
   - Restart the game, load a map, spawn yourself as a micro, and select a giantess.
   - Press **F8**, choose a preset, then press **T or F9** to chat.

### AI configuration example

Replace the placeholder values before use. Additional named sections create more presets;
each inherits settings from `[Default]` when loaded. Put shared API settings in `[Default]`.

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

For multiple characters, add sections such as `[Character 1]` and `[Character 2]` with
your own `Personality` and voice settings. There are no bundled character scenarios.

### AI controls

| Control | Action |
|---------|--------|
| Pause menu â†’ AI Settings | Edit configuration and local scenarios |
| F8 | Choose a preset for the selected giantess, or deactivate her AI |
| T or F9 | Open chat while AI is active |
| Enter | Send text from the open chat box |
| Esc | Close chat |
| F10 | Open manual speech input while AI is active |
| L | Toggle animation lock for the selected AI character |
| Backslash | Mute/unmute the selected AI character |
| F11 | Save conversation locally |
| Shift+F11 | Clear conversation and its local saved history |

If chat does not appear, first check that you spawned as a micro, selected a giantess,
and activated her AI. An API response is not required to open the chat interface.
For troubleshooting, report the exact game build, plugin version, and a redacted
`BepInEx/LogOutput.log` captured after trying F8 and F9.

Configuration, scenarios, conversation history, and logs can contain private information.
They stay local and are excluded from this repository. Do not include them in shared ZIPs.

## Building from Source

### Requirements
- .NET SDK (targets net472)
- BepInEx 5.4.x core DLLs
- Sizebox game DLLs (for references)

### Build
```bash
cd src/SizeboxFix
dotnet build
```

The built DLL will be at `src/SizeboxFix/bin/Debug/net472/SizeboxFix.dll`.

Note: The `.csproj` references game DLLs from a relative path. You may need to adjust `HintPath` entries in `SizeboxFix.csproj` to match your game installation path.

## Credits

Made by Siphon for the Sizebox community.

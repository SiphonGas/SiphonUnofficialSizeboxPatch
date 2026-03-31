# Siphon's Unofficial Sizebox Patch

> *"We choose to goon to the moon!"*

A BepInEx plugin for Sizebox v3.01 that fixes critical bugs and adds new features.

## Disclaimer

This is an **unofficial, personal project**. I am not a developer of Sizebox though I am in contact with them and may become one. This mod is provided as-is with no warranty. Use at your own risk. I am not responsible for any issues, crashes, broken saves, corrupted installations, or anything else that may result from using this plugin. Always back up your game files before installing mods.

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

### AI Giantess (Experimental)
An AI-powered giantess that acts autonomously using LLM text generation.

- **F8** - Toggle AI on selected giantess (spawn as micro first)
- **F9** - Open chat to talk to her
- Uses OpenRouter API (supports any OpenAI-compatible endpoint)
- Controls animations (2800+), facial morphs, movement, and dialogue
- Customizable personality via config file
- On-screen chat log with color-coded messages
- Optional ElevenLabs text-to-speech

## Installation

### Requirements
- Sizebox v3.01
- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) (Unity IL2CPP or Mono, x64)

### Steps

1. **Install BepInEx** (if you haven't already):
   - Download BepInEx 5.4.x for Unity Mono x64
   - Extract into your Sizebox game folder (where `Sizebox.exe` is)
   - Run the game once to generate BepInEx folders, then close it

2. **Install the plugin**:
   - Download `SizeboxFix.dll` from the [Releases](../../releases) page
   - Copy it to `BepInEx/plugins/`

3. **Install Lua behaviors** (optional):
   - Copy the `.lua` files from `Sizebox v3.01 - Win64 (Primary)/Sizebox_Data/StreamingAssets/lua/behaviors/` to the same path in your game folder

4. **Set up AI Giantess** (optional):
   - Launch the game once — it creates `BepInEx/config/SizeboxAI.cfg`
   - Close the game
   - Get an API key from [OpenRouter](https://openrouter.ai/keys)
   - Open `SizeboxAI.cfg` and paste your key on the `ApiKey=` line
   - Edit the `Personality=` line to customize her behavior
   - Relaunch the game

### AI Config Example
```ini
ApiKey=your-openrouter-key-here
ApiUrl=https://openrouter.ai/api/v1/chat/completions
Model=nousresearch/hermes-3-llama-3.1-70b
DecisionInterval=8
Personality=You are a playful giantess who enjoys toying with tiny people.

# Optional ElevenLabs TTS
TTSApiKey=your-elevenlabs-key-here
TTSVoiceId=eVItLK1UvXctxuaRV2Oq
TTSEnabled=true
```

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

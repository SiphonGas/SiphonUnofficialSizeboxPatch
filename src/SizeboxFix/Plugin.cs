using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sizebox.CharacterEditor;

namespace SizeboxFix
{
    [BepInPlugin("com.sizeboxfix.patches", "Sizebox Fix", "1.5.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static BepInEx.Logging.ManualLogSource Log;

        public static bool AlwaysLookAtPlayer
        {
            get { return PlayerPrefs.GetInt("AlwaysLookAtPlayer", 0) == 1; }
            set { PlayerPrefs.SetInt("AlwaysLookAtPlayer", value ? 1 : 0); }
        }

        public static bool DisableVerticalOffset
        {
            get { return PlayerPrefs.GetInt("DisableVerticalOffset", 0) == 1; }
            set { PlayerPrefs.SetInt("DisableVerticalOffset", value ? 1 : 0); }
        }

        void Awake()
        {
            Log = Logger;
            var harmony = new Harmony("com.sizeboxfix.patches");
            harmony.PatchAll();
            Logger.LogInfo("SizeboxFix " + Info.Metadata.Version + " loaded");

            // Add AI keybind handler (F8 to toggle)
            gameObject.AddComponent<AIKeybindHandler>();
            gameObject.AddComponent<AIGiantess>();
        }
    }

    // === Fix 1: Scale snap + position sync rewrite ===
    // Replaces GTSMovement.Update entirely to fix:
    // - float.Epsilon scale comparison causing constant re-snapping
    // - MoveTransformToCapsule lerp causing gravity-driven Y drift
    [HarmonyPatch(typeof(GTSMovement), "Update")]
    static class GTSMovementUpdatePatch
    {
        static bool Prefix(GTSMovement __instance)
        {
            if (GameController.Instance.paused) return false;

            Transform capsuleT = __instance.transform;
            Giantess gts = __instance.giantess;
            float gtsScale = gts.Scale;

            // Fix 1a: Use meaningful epsilon for scale comparison
            if (Mathf.Abs(capsuleT.lossyScale.y - gtsScale) > 0.001f)
            {
                capsuleT.localScale = Vector3.one * gtsScale;
            }

            // Fix 1b: Rewritten position sync to prevent Y-drift
            var moveState = __instance.moveState;

            // DoNotMove must be checked first — user wants to noclip/freely place
            if (moveState == GTSMovement.MacroMoveState.DoNotMove)
            {
                // Skip movement but still sync rotation so rotation handle works
                capsuleT.rotation = gts.transform.rotation;
            }
            else if (moveState == GTSMovement.MacroMoveState.ResetTransformPosition)
            {
                capsuleT.position = gts.transform.position;
                __instance.moveState = GTSMovement.MacroMoveState.Move;
            }
            else if (moveState == GTSMovement.MacroMoveState.OnlyMoveWithPhysics ||
                gts.Movement.move)
            {
                // Moving: unfreeze rigidbody for physics
                var rbMove = Traverse.Create(__instance).Field("rigidBody").GetValue<Rigidbody>();
                if (rbMove != null)
                {
                    if (rbMove.constraints == RigidbodyConstraints.FreezeAll)
                        rbMove.constraints = RigidbodyConstraints.FreezeRotation;

                    // Kill any residual velocity from collisions — prevents drift
                    // The steering system sets velocity directly, so we don't need
                    // leftover collision velocity hanging around
                    if (rbMove.velocity.magnitude > gts.Height * 2f)
                    {
                        rbMove.velocity = Vector3.zero;
                    }
                }

                Vector3 capsulePos = capsuleT.position;
                Vector3 meshPos = gts.transform.position;

                if ((capsulePos - meshPos).sqrMagnitude > 0.0001f)
                {
                    Vector3 targetPos = capsulePos;

                    // Raycast down from capsule to find actual ground level
                    // This prevents the mesh from floating when the capsule
                    // rides over building colliders
                    RaycastHit hit;
                    float rayStart = gts.Height * 2f;
                    Vector3 rayOrigin = new Vector3(capsulePos.x, capsulePos.y + rayStart, capsulePos.z);
                    if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayStart * 2f, Layers.gtsWalkableMask))
                    {
                        // Use ground Y, not capsule Y
                        targetPos.y = hit.point.y;
                    }
                    else
                    {
                        // No terrain hit — fall back to clamped Y
                        float yDiff = capsulePos.y - meshPos.y;
                        if (yDiff > gts.Height * 0.1f)
                            targetPos.y = meshPos.y;
                    }

                    gts._MoveMesh(targetPos);
                }

                // Only sync rotation if capsule rotated significantly (>2 degrees)
                // Prevents tiny physics nudges from causing visible jitter
                float deltaTime = Time.deltaTime;
                float angleDiff = Quaternion.Angle(gts.transform.rotation, capsuleT.rotation);
                if (angleDiff > 2f)
                    gts.transform.rotation = Quaternion.Slerp(
                        gts.transform.rotation, capsuleT.rotation, 10f * deltaTime);
            }
            else
            {
                // Not moving: freeze everything — position AND rotation
                var rb = Traverse.Create(__instance).Field("rigidBody").GetValue<Rigidbody>();
                if (rb != null)
                {
                    if (rb.constraints != RigidbodyConstraints.FreezeAll)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.constraints = RigidbodyConstraints.FreezeAll;
                    }
                    rb.position = gts.transform.position;
                    rb.rotation = gts.transform.rotation;
                }
                capsuleT.position = gts.transform.position;
                capsuleT.rotation = gts.transform.rotation;
            }


            // Skip original CollisionChoose — it disables terrain collision
            // at large scales which causes models to fall through the map.
            // Instead, always keep terrain collision enabled.

            return false;
        }

        static void GroundCheck(Giantess gts)
        {
            Vector3 pos = gts.transform.position;
            float height = gts.Height;
            if (height <= 0) return;

            // Raycast down from above the model to find any floor surface
            // Use ~0 (all layers) to catch terrain, objects, buildings, everything
            RaycastHit hit;
            Vector3 rayOrigin = pos + Vector3.up * height * 2f;
            float rayDist = height * 4f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDist))
            {
                float groundY = hit.point.y;
                // If model has fallen more than 10% of its height below ground, rescue it
                if (pos.y < groundY - height * 0.1f)
                {
                    pos.y = groundY;
                    gts.transform.position = pos;

                    // Also sync capsule
                    if (gts.gtsMovement != null)
                    {
                        gts.gtsMovement.transform.position = pos;
                        var rb = Traverse.Create(gts.gtsMovement).Field("rigidBody").GetValue<Rigidbody>();
                        if (rb != null)
                        {
                            rb.position = pos;
                            rb.velocity = Vector3.zero;
                        }
                    }
                }
            }
        }
    }

    // === Fix 1e: Zero rigidbody velocity when movement stops ===
    // MovementCharacter.Stop() sets move=false but doesn't zero rigidbody velocity,
    // causing drift after hitting colliders.
    [HarmonyPatch(typeof(MovementCharacter), "Stop")]
    static class MovementStopVelocityFix
    {
        static void Postfix(MovementCharacter __instance)
        {
            var entity = __instance.Entity;
            if (entity == null || !entity.isGiantess) return;

            var gts = entity as Giantess;
            if (gts == null || gts.gtsMovement == null) return;

            var rb = Traverse.Create(gts.gtsMovement).Field("rigidBody").GetValue<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    // === Fix 1c: Pause gravity on scene load ===
    // Models fall through floors on load because gravity kicks in before
    // room MeshColliders finish initializing. Pause gravity on all entities
    // for a few seconds after scene rebuild.
    [HarmonyPatch(typeof(SavedScenesManager), "ReBuildScene")]
    static class PauseGravityOnSceneLoad
    {
        static void Postfix()
        {
            // Find all gravity components and pause them
            var gravities = Object.FindObjectsOfType<Gravity>();
            foreach (var g in gravities)
            {
                g.PauseForSeconds(3f);
            }
        }
    }

    // === Fix 1d: Prevent terrain collision from being disabled ===
    // The original CollisionChoose disables terrain collision when scale > 20,
    // causing giantesses to fall through the map. Override to never disable it.
    [HarmonyPatch(typeof(GTSMovement), "CollisionChoose")]
    static class PreventTerrainCollisionDisable
    {
        static bool Prefix()
        {
            // Skip the original entirely — never disable terrain collision
            return false;
        }
    }

    // === Fix 2: Removed — EntityBase.Move now uses original game logic ===
    // The original Translate approach works correctly with the handle system.

    // === Fix 3: Save crash - GetTransformKey null check ===
    [HarmonyPatch(typeof(CharacterEditor), "GetTransformKey")]
    static class GetTransformKeyNullFix
    {
        static bool Prefix(Transform inTransform, ref string __result)
        {
            if (inTransform == null)
            {
                __result = null;
                return false;
            }
            return true;
        }
    }

    // === Fix 4: Save crash - DynamicBoneData exclusions filter ===
    [HarmonyPatch(typeof(DynamicBoneData), MethodType.Constructor,
        new[] { typeof(DynamicBone), typeof(CharacterEditor) })]
    static class DynamicBoneDataSaveFix
    {
        static void Postfix(ref DynamicBoneData __instance)
        {
            if (__instance.exclusions != null)
            {
                __instance.exclusions.RemoveAll(s => s == null);
            }
        }
    }

    // === Fix 5: Save crash - CharacterEditor.Save safety net ===
    [HarmonyPatch(typeof(CharacterEditor), "Save")]
    static class CharacterEditorSaveFix
    {
        static System.Exception Finalizer(System.Exception __exception, ref CharacterEditorSaveData __result)
        {
            if (__exception != null)
            {
                Debug.LogWarning("[SizeboxFix] CharacterEditor.Save failed, skipping editor data: " + __exception.Message);
                __result = null;
                return null;
            }
            return null;
        }
    }

    // === Fix 6: Handle gizmo entity sync ===
    // Sync handle target to currently selected entity so arrows follow selection.
    // Also ensure rigidbody is non-kinematic during drag so terrain collision works.
    [HarmonyPatch(typeof(HandleControl), "Update")]
    static class HandleControlSyncFix
    {
        static void Prefix(HandleControl __instance)
        {
            var selected = InterfaceControl.instance != null ? InterfaceControl.instance.selectedEntity : null;
            if (selected != null && selected != __instance.smartObject)
            {
                __instance.smartObject = selected;
            }
        }

    }

    // === Fix 6b: Prevent ChangeScale position drift ===
    // ChangeScale does SetParent(null) then SetParent(parent) which causes
    // floating point position shift. Save and restore exact position.
    [HarmonyPatch(typeof(EntityBase), "ChangeScale")]
    static class ChangeScalePositionFix
    {
        static void Prefix(EntityBase __instance, out Vector3 __state)
        {
            __state = __instance.transform.position;
        }

        static void Postfix(EntityBase __instance, Vector3 __state)
        {
            // Restore exact position after scale change
            if (__instance.isGiantess)
            {
                __instance.transform.position = __state;
            }
        }
    }

    // === Fix 6c: Option to disable vertical offset ===
    [HarmonyPatch(typeof(EntityBase), "ChangeVerticalOffset")]
    static class DisableVerticalOffsetPatch
    {
        static bool Prefix()
        {
            return !Plugin.DisableVerticalOffset;
        }
    }

    // === Fix 6d: Reset physics on entity initialization ===
    // Pause gravity on ALL entities when they initialize, giving floor
    // colliders time to load. Also resets player physics defaults.
    [HarmonyPatch(typeof(EntityBase), "FinishInitialization")]
    static class ResetPhysicsOnInit
    {
        static void Postfix(EntityBase __instance)
        {
            // Pause gravity on ALL entities to prevent falling through floors on load
            var gravity = __instance.GetComponent<Gravity>();
            if (gravity != null)
            {
                gravity.PauseForSeconds(3f);
            }

            // Also check giantess capsule gravity
            if (__instance.isGiantess)
            {
                var gts = __instance as Giantess;
                if (gts != null && gts.gtsMovement != null)
                {
                    var capsuleGravity = gts.gtsMovement.GetComponent<Gravity>();
                    if (capsuleGravity != null)
                    {
                        capsuleGravity.PauseForSeconds(3f);
                    }

                    // Zero capsule velocity
                    var rb = Traverse.Create(gts.gtsMovement).Field("rigidBody").GetValue<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }

            // Reset player-specific physics
            if (__instance.isPlayer)
            {
                var rb = __instance.Rigidbody;
                if (rb != null)
                {
                    rb.sleepThreshold = 0.005f;
                    rb.solverIterations = 6;
                    rb.solverVelocityIterations = 1;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    rb.drag = 0f;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    Time.fixedDeltaTime = 0.02f;
                }
            }
        }
    }


    // === Fix 7: Save load crash - PlayerControl.SetEntity null safety ===
    // When loading a save, SetEntity can receive a null entity if the model
    // failed to load. LuaManager.Instance may also be null. This crashes
    // the entire load process, making saves appear "reset."
    [HarmonyPatch(typeof(PlayerControl), "SetEntity")]
    static class PlayerControlSetEntityFix
    {
        static System.Exception Finalizer(System.Exception __exception)
        {
            if (__exception != null)
            {
                Debug.LogWarning("[SizeboxFix] PlayerControl.SetEntity failed (safe to ignore): " + __exception.Message);
                return null;
            }
            return null;
        }
    }

    // Also catch the Player.SetEntity -> StopPlayingEntity chain
    [HarmonyPatch(typeof(Player), "StopPlayingEntity")]
    static class PlayerStopPlayingFix
    {
        static System.Exception Finalizer(System.Exception __exception)
        {
            if (__exception != null)
            {
                Debug.LogWarning("[SizeboxFix] Player.StopPlayingEntity failed (safe to ignore): " + __exception.Message);
                return null;
            }
            return null;
        }
    }

    // Catch Humanoid.Load null reference during save restore
    [HarmonyPatch(typeof(Humanoid), "Load")]
    static class HumanoidLoadFix
    {
        static System.Exception Finalizer(System.Exception __exception)
        {
            if (__exception != null)
            {
                Debug.LogWarning("[SizeboxFix] Humanoid.Load failed, skipping entity data: " + __exception.Message);
                return null;
            }
            return null;
        }
    }

    // === Feature: Bone Hide/Show/Delete buttons ===
    // Adds buttons to the left skeleton options panel.
    // Directly manipulates bone transforms and renderers.
    [HarmonyPatch]
    static class BoneEditButtons
    {
        static GameObject panel;
        static HandleManager cachedManager;
        // Store original scales for show/restore
        static System.Collections.Generic.Dictionary<Transform, Vector3> hiddenBones
            = new System.Collections.Generic.Dictionary<Transform, Vector3>();
        // Track permanently deleted bones so Show won't restore them
        static System.Collections.Generic.HashSet<Transform> deletedBones
            = new System.Collections.Generic.HashSet<Transform>();

        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("Pause.HandleManager")
                    ?? AccessTools.TypeByName("HandleManager");
            return AccessTools.Method(type, "LateUpdate");
        }

        static void Postfix(object __instance)
        {
            var manager = __instance as HandleManager;
            if (manager == null) return;
            cachedManager = manager;

            // Retry creating buttons each frame until the options GUI exists
            if (panel == null)
            {
                var optionsGui = Object.FindObjectOfType<SkeletonEditOptionsGui>();
                if (optionsGui != null)
                {
                    CreateButtons(optionsGui);
                }
            }
        }

        static void CreateButtons(SkeletonEditOptionsGui optionsGui)
        {
            // Parent to the same container as the options GUI
            Transform parent = optionsGui.transform;

            // Create a vertical layout group for our buttons
            panel = new GameObject("SizeboxFix_BoneTools");
            panel.transform.SetParent(parent, false);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 3f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(5, 5, 5, 5);

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            CreateLabel(panel.transform, "BONE TOOLS");
            CreateButton(panel.transform, "Hide Bone", OnHideBone);
            CreateButton(panel.transform, "Show Bone", OnShowBone);
            CreateButton(panel.transform, "Delete Bone Mesh", OnDeleteBone);
            CreateLabel(panel.transform, "BONE PRESETS");
            CreateButton(panel.transform, "Save Hidden Bones", OnSaveHiddenBones);
            CreateButton(panel.transform, "Load Hidden Bones", OnLoadHiddenBones);
        }

        static void CreateLabel(Transform parent, string text)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 20f;

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        static void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 25f;

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
            colors.pressedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(action);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var txt = textGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 12;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            var txtRt = textGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
        }

        static EditBone GetSelectedBone()
        {
            if (cachedManager == null) return null;

            // Try TargetHandles first (multi-select)
            var handles = cachedManager.TargetHandles;
            if (handles != null && handles.Count > 0)
            {
                var h = handles[0];
                if (h != null && h.EditBone != null) return h.EditBone;
            }

            // Fall back to TargetHandle (single select)
            var target = cachedManager.TargetHandle;
            if (target != null && target.EditBone != null) return target.EditBone;

            return null;
        }

        static System.Collections.Generic.List<EditBone> GetAllSelectedBones()
        {
            var result = new System.Collections.Generic.List<EditBone>();
            if (cachedManager == null) return result;

            var handles = cachedManager.TargetHandles;
            if (handles != null && handles.Count > 0)
            {
                foreach (var h in handles)
                {
                    if (h != null && h.EditBone != null)
                        result.Add(h.EditBone);
                }
            }

            // If nothing in list, try single target
            if (result.Count == 0)
            {
                var target = cachedManager.TargetHandle;
                if (target != null && target.EditBone != null)
                    result.Add(target.EditBone);
            }

            return result;
        }

        static void OnHideBone()
        {
            var bones = GetAllSelectedBones();
            if (bones.Count == 0)
            {
                Debug.Log("[SizeboxFix] No bone selected to hide");
                return;
            }

            foreach (var bone in bones)
            {
                Transform t = bone.RealTransform;
                if (t == null) continue;

                if (!hiddenBones.ContainsKey(t))
                    hiddenBones[t] = t.localScale;

                t.localScale = Vector3.one * 0.0001f;
                Debug.Log("[SizeboxFix] Hidden bone: " + t.name);
            }
        }

        static void OnShowBone()
        {
            var bones = GetAllSelectedBones();
            if (bones.Count == 0)
            {
                // Restore ALL hidden bones (but skip deleted ones)
                foreach (var kvp in hiddenBones)
                {
                    if (kvp.Key != null && !deletedBones.Contains(kvp.Key))
                    {
                        kvp.Key.localScale = kvp.Value;
                        Debug.Log("[SizeboxFix] Restored bone: " + kvp.Key.name);
                    }
                }
                hiddenBones.Clear();
                return;
            }

            foreach (var bone in bones)
            {
                Transform t = bone.RealTransform;
                if (t == null) continue;

                // Skip deleted bones — they can't be restored
                if (deletedBones.Contains(t))
                {
                    Debug.Log("[SizeboxFix] Cannot restore deleted bone: " + t.name);
                    continue;
                }

                if (hiddenBones.ContainsKey(t))
                {
                    t.localScale = hiddenBones[t];
                    hiddenBones.Remove(t);
                }
                else
                {
                    t.localScale = Vector3.one;
                }
                Debug.Log("[SizeboxFix] Restored bone: " + t.name);
            }
        }

        static void OnDeleteBone()
        {
            var bones = GetAllSelectedBones();
            if (bones.Count == 0)
            {
                Debug.Log("[SizeboxFix] No bone selected to delete");
                return;
            }

            // Show confirmation dialog
            var msg = UiMessageBox.Create(
                "Permanently delete this bone?\n(Respawn model to restore)",
                "Delete Bone");
            msg.AddButtonsYesNo(() => { DoDeleteBones(bones); msg.Close(); });
            msg.Popup();
        }

        static void DoDeleteBones(System.Collections.Generic.List<EditBone> bones)
        {
            foreach (var bone in bones)
            {
                Transform t = bone.RealTransform;
                if (t == null) continue;

                var renderers = t.GetComponentsInChildren<Renderer>(true);
                int count = 0;
                foreach (var r in renderers)
                {
                    Object.Destroy(r);
                    count++;
                }

                var colliders = t.GetComponentsInChildren<Collider>(true);
                foreach (var c in colliders)
                {
                    Object.Destroy(c);
                }

                t.localScale = Vector3.one * 0.0001f;
                deletedBones.Add(t);

                Debug.Log("[SizeboxFix] Permanently deleted bone: " + t.name + " (" + count + " renderers destroyed)");
            }
        }

        static void OnSaveHiddenBones()
        {
            var entity = InterfaceControl.instance?.selectedEntity;
            if (entity == null)
            {
                Debug.Log("[SizeboxFix] No entity selected");
                return;
            }

            // Collect all hidden bone names
            var boneNames = new System.Collections.Generic.List<string>();
            foreach (var kvp in hiddenBones)
            {
                if (kvp.Key != null)
                    boneNames.Add(kvp.Key.name);
            }
            foreach (var t in deletedBones)
            {
                if (t != null && !boneNames.Contains(t.name))
                    boneNames.Add(t.name);
            }

            if (boneNames.Count == 0)
            {
                new Toast("_bones").Print("No hidden bones to save");
                return;
            }

            // Save to character folder
            string folder = Path.Combine(
                Application.persistentDataPath,
                "Character",
                entity.asset.AssetFullName);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, "hidden_bones.txt");
            File.WriteAllLines(path, boneNames.ToArray());

            Debug.Log("[SizeboxFix] Saved " + boneNames.Count + " hidden bones to " + path);
            new Toast("_bones").Print("Saved " + boneNames.Count + " hidden bones");
        }

        static void OnLoadHiddenBones()
        {
            var entity = InterfaceControl.instance?.selectedEntity;
            if (entity == null)
            {
                Debug.Log("[SizeboxFix] No entity selected");
                return;
            }

            string folder = Path.Combine(
                Application.persistentDataPath,
                "Character",
                entity.asset.AssetFullName);
            string path = Path.Combine(folder, "hidden_bones.txt");

            if (!File.Exists(path))
            {
                new Toast("_bones").Print("No hidden bones preset found");
                return;
            }

            string[] boneNames = File.ReadAllLines(path);
            int count = 0;

            // Find all bones on the entity and hide matching ones
            var allTransforms = entity.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                foreach (string name in boneNames)
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    if (t.name == name.Trim())
                    {
                        if (!hiddenBones.ContainsKey(t))
                            hiddenBones[t] = t.localScale;
                        t.localScale = Vector3.one * 0.0001f;
                        count++;
                        break;
                    }
                }
            }

            Debug.Log("[SizeboxFix] Loaded " + count + " hidden bones from " + path);
            new Toast("_bones").Print("Loaded " + count + " hidden bones");
        }
    }

    // === Feature: Always Look At Player ===

    [HarmonyPatch(typeof(SenseController), "CheckVisibility")]
    static class AlwaysVisibleToGiantess
    {
        static bool Prefix(EntityBase target, ref bool __result)
        {
            if (Plugin.AlwaysLookAtPlayer && target != null && target.isPlayer)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Giantess), "FinishInitialization")]
    static class ForceCanLookAtPlayer
    {
        static void Postfix(Giantess __instance)
        {
            if (Plugin.AlwaysLookAtPlayer)
            {
                __instance.canLookAtPlayer = true;
            }
        }
    }

    [HarmonyPatch]
    static class GameSettingsViewPatch
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("Pause.GameSettingsView")
                    ?? AccessTools.TypeByName("GameSettingsView");
            return AccessTools.Method(type, "Start");
        }

        static void Postfix(object __instance)
        {
            var settingsType = __instance.GetType();
            var addToggle = AccessTools.Method(settingsType, "AddToggle",
                new[] { typeof(string), typeof(bool), typeof(UnityEngine.Events.UnityAction<bool>) });

            if (addToggle == null)
            {
                addToggle = AccessTools.Method(settingsType.BaseType, "AddToggle",
                    new[] { typeof(string), typeof(bool), typeof(UnityEngine.Events.UnityAction<bool>) });
            }

            if (addToggle != null)
            {
                UnityEngine.Events.UnityAction<bool> callback = (bool value) =>
                {
                    Plugin.AlwaysLookAtPlayer = value;
                    var giantesses = Object.FindObjectsOfType<Giantess>();
                    foreach (var gts in giantesses)
                    {
                        gts.canLookAtPlayer = value || GlobalPreferences.LookAtPlayer.value;
                    }
                };

                addToggle.Invoke(__instance, new object[] { "Always Look At Player", Plugin.AlwaysLookAtPlayer, callback });

                UnityEngine.Events.UnityAction<bool> offsetCallback = (bool value) =>
                {
                    Plugin.DisableVerticalOffset = value;
                };
                addToggle.Invoke(__instance, new object[] { "Disable Vertical Offset", Plugin.DisableVerticalOffset, offsetCallback });

                // Reset Vertical Offset button
                var addButton = AccessTools.Method(settingsType, "AddButton",
                    new[] { typeof(string) });
                if (addButton == null)
                    addButton = AccessTools.Method(settingsType.BaseType, "AddButton",
                        new[] { typeof(string) });

                if (addButton != null)
                {
                    var btn = addButton.Invoke(__instance, new object[] { "Reset All Vertical Offsets" }) as Button;
                    if (btn != null)
                    {
                        btn.onClick.AddListener(() =>
                        {
                            var entities = Object.FindObjectsOfType<EntityBase>();
                            foreach (var e in entities)
                            {
                                if (Mathf.Abs(e.offset) > 0.001f)
                                {
                                    e.offset = 0f;
                                    e.Move(e.transform.position);
                                }
                            }
                            Debug.Log("[SizeboxFix] Reset vertical offset on all entities");
                        });
                    }
                }
            }
        }
    }

    // === Fix 8: Micro scale physics/camera stabilization ===
    // At extreme small sizes (<1mm), the camera near clip, physics sleep threshold,
    // and solver settings break down. This patch adjusts them dynamically.
    [HarmonyPatch(typeof(CameraEffectsSettings), "UpdateEffectsRealtime")]
    static class MicroScaleCameraFix
    {
        static void Postfix(CameraEffectsSettings __instance)
        {
            var cam = __instance.primaryCamera;
            if (cam == null) return;

            // Clamp near clip plane to minimum 0.001 to prevent depth buffer precision loss
            if (cam.nearClipPlane < 0.001f)
            {
                cam.nearClipPlane = 0.001f;
                cam.farClipPlane = cam.nearClipPlane * 20000f;
            }
        }
    }

    // Adjust physics settings and enforce minimum collider size when player scale changes
    [HarmonyPatch(typeof(EntityBase), "ChangeScale")]
    static class MicroScalePhysicsFix
    {
        static float originalDrag = -1f;

        static void Postfix(EntityBase __instance, float newScale)
        {
            if (!__instance.isPlayer) return;

            var rb = __instance.Rigidbody;
            if (rb == null) return;

            // Store original drag on first call
            if (originalDrag < 0f)
                originalDrag = rb.drag;

            if (newScale < 0.01f)
            {
                rb.sleepThreshold = 0.0001f;
                rb.solverIterations = 12;
                rb.solverVelocityIterations = 6;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                // Faster physics tick = smoother micro movement
                Time.fixedDeltaTime = 0.005f;
                // More drag to prevent sliding off surfaces
                rb.drag = 2f;
            }
            else if (newScale < 0.1f)
            {
                rb.sleepThreshold = 0.001f;
                rb.solverIterations = 8;
                rb.solverVelocityIterations = 4;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                Time.fixedDeltaTime = 0.01f;
                rb.drag = 1f;
            }
            else
            {
                rb.sleepThreshold = 0.005f;
                rb.solverIterations = 6;
                rb.solverVelocityIterations = 1;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                Time.fixedDeltaTime = 0.02f;
                rb.drag = originalDrag >= 0f ? originalDrag : 0f;

                var helper = __instance.GetComponent<MicroGroundHelper>();
                if (helper != null) helper.enabled = false;
            }
        }
    }

    // === Fix 8b: Raycast ground helper for micro scale ===
    // When physics colliders are too small to work, this uses raycasts to
    // detect surfaces and keep the player on the ground.
    public class MicroGroundHelper : MonoBehaviour
    {
        Rigidbody rb;
        EntityBase entity;
        float lastGroundY = float.NegativeInfinity;
        int framesSinceGround;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            entity = GetComponent<EntityBase>();
        }

        void FixedUpdate()
        {
            if (rb == null || entity == null) return;

            float height = entity.Height;
            if (height <= 0) height = 0.001f;

            Vector3 pos = transform.position;
            float rayDist = height * 10f;

            // Only intervene when falling — don't fight normal movement
            bool isFalling = rb.velocity.y < -height * 0.5f;

            // Cast down from above the player
            Vector3 rayOrigin = pos + Vector3.up * height;

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDist))
            {
                float groundY = hit.point.y;
                lastGroundY = groundY;
                framesSinceGround = 0;

                // Only rescue if player has fallen BELOW the surface
                float penetration = groundY - pos.y;
                if (penetration > height * 0.2f)
                {
                    // Player clipped through — teleport back on top
                    pos.y = groundY;
                    transform.position = pos;
                    rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                }
            }
            else
            {
                framesSinceGround++;

                // Extended search only after significant time falling
                if (framesSinceGround > 30 && isFalling)
                {
                    if (Physics.Raycast(pos + Vector3.up * rayDist * 2f, Vector3.down, out hit, rayDist * 5f))
                    {
                        if (pos.y < hit.point.y - height)
                        {
                            pos.y = hit.point.y;
                            transform.position = pos;
                            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                        }
                        lastGroundY = hit.point.y;
                        framesSinceGround = 0;
                    }
                }
            }
        }
    }

    // === Fix 8c: Root motion rotation snap ===
    // RootMotionTransfer applies animation root rotation to the rigidbody,
    // causing sudden 180-degree snaps on giantesses. Skip rotation for giantesses
    // since the capsule collider handles their movement.
    [HarmonyPatch(typeof(RootMotionTransfer), "FixedUpdate")]
    static class RootMotionRotationFix
    {
        static bool Prefix(RootMotionTransfer __instance)
        {
            var entity = Traverse.Create(__instance).Field("myEntity").GetValue<EntityBase>();
            if (entity == null || !entity.isGiantess) return true; // let original run for non-giantess

            var rb = Traverse.Create(__instance).Field("_rigidbody").GetValue<Rigidbody>();
            var delta = Traverse.Create(__instance).Field("delta").GetValue<Vector3>();

            if (rb != null)
            {
                bool isFrozen = rb.constraints == RigidbodyConstraints.FreezeAll;
                if (!isFrozen)
                    rb.MovePosition(rb.position + delta);
            }

            // Reset accumulators
            Traverse.Create(__instance).Field("delta").SetValue(Vector3.zero);
            Traverse.Create(__instance).Field("deltaRotation").SetValue(Quaternion.identity);

            return false;
        }
    }

    // === Fix 8d: Freeze rigidbody when idle to prevent pop/freak out ===
    // Instead of kinematic (breaks collision) or velocity zeroing (gravity wins),
    // freeze all position/rotation constraints when idle. This allows collision
    // detection but prevents any movement from forces.
    // Applied via GTSMovement idle state — update the existing idle branch.

    // === Fix 8e: Toast notification crash cascade ===
    // ToastInternal.TryCreate crashes when its prefab is null, turning
    // every game error into two errors. Silently skip when prefab missing.
    [HarmonyPatch]
    static class ToastCrashFix
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("ToastInternal");
            return AccessTools.Method(type, "TryCreate", new[] { typeof(string) });
        }

        static System.Exception Finalizer(System.Exception __exception)
        {
            return null; // Swallow all toast errors
        }
    }

    // Catch all ToastInternal crashes — PopUp, StartCoroutine on inactive object
    [HarmonyPatch]
    static class ToastPopUpCrashFix
    {
        static System.Type _toastInternalType;

        static bool Prepare()
        {
            _toastInternalType = AccessTools.TypeByName("ToastInternal");
            return _toastInternalType != null;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(_toastInternalType, "PopUp", new[] { typeof(Toast.Timeout) });
            yield return AccessTools.Method(_toastInternalType, "PopUp", new[] { typeof(string), typeof(Toast.Timeout) });
        }

        static bool Prefix(MonoBehaviour __instance)
        {
            // Skip if game object is inactive — can't start coroutines
            // Also destroy the stray toast object so it doesn't appear in menus
            if (__instance == null || !__instance.gameObject.activeInHierarchy)
            {
                if (__instance != null)
                    Object.Destroy(__instance.gameObject);
                return false;
            }
            return true;
        }

        static System.Exception Finalizer(System.Exception __exception)
        {
            return null;
        }
    }

    // === Fix 9: Blink morph selection + routine corruption ===

    // Fix 9-pre: Override blink morph selection.
    // The game hardcodes FindMorphByNameContains("笑い") which finds the
    // smile morph on models that have both "Blink" and "笑い" morphs.
    // Prefer dedicated blink morphs first, fall back to 笑い for MMD models.
    [HarmonyPatch(typeof(Humanoid), "InitializeMorphs")]
    static class BlinkMorphSelectionFix
    {
        static void Postfix(Humanoid __instance)
        {
            // Check if there's a dedicated "Blink" morph
            var morphs = __instance.Morphs;
            if (morphs == null) return;

            EntityMorphData blinkMorph = null;
            EntityMorphData currentBlink = Traverse.Create(__instance).Field("_blinkingMorph").GetValue<EntityMorphData>();

            // Look for exact "Blink" morph first (case-insensitive)
            foreach (var m in morphs)
            {
                if (m.Name.Equals("Blink", System.StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Equals("blink", System.StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Equals("まばたき", System.StringComparison.Ordinal))
                {
                    blinkMorph = m;
                    break;
                }
            }

            // If we found a better blink morph, override the game's choice
            if (blinkMorph != null && blinkMorph != currentBlink)
            {
                Traverse.Create(__instance).Field("_blinkingMorph").SetValue(blinkMorph);
                Plugin.Log.LogInfo("[Blink] Overrode blink morph: '" + currentBlink?.Name + "' -> '" + blinkMorph.Name + "'");
            }
        }
    }

    // Fix 9a: Prevent duplicate blink coroutines from stacking.
    [HarmonyPatch(typeof(Humanoid), "StartBlinkingRoutine")]
    static class BlinkRoutineFix
    {
        static System.Collections.Generic.HashSet<int> _blinkingEntities
            = new System.Collections.Generic.HashSet<int>();

        static bool Prefix(Humanoid __instance)
        {
            int id = __instance.GetInstanceID();

            // If already blinking, skip starting another coroutine
            if (_blinkingEntities.Contains(id))
            {
                return false; // Block duplicate blink coroutine
            }

            _blinkingEntities.Add(id);
            return true;
        }
    }

    // Prevent blink from overwriting user-set morph values
    // Patch BOTH SetMorphValue overloads (int and string) so the blink
    // restore always uses the user's intended value.
    [HarmonyPatch(typeof(EntityBase), "SetMorphValue", new[] { typeof(int), typeof(float) })]
    static class MorphUserChangeFix
    {
        static void Prefix(EntityBase __instance, int i, float weight)
        {
            var humanoid = __instance as Humanoid;
            if (humanoid == null) return;

            var blinkMorph = Traverse.Create(humanoid).Field("_blinkingMorph").GetValue<EntityMorphData>();
            if (blinkMorph == null) return;

            if (i < __instance.Morphs.Count && __instance.Morphs[i] == blinkMorph)
            {
                Traverse.Create(humanoid).Field("_blinkMorphUserState").SetValue(weight);
            }
        }
    }

    // Also catch string-based morph changes (from sliders, AI, load presets)
    [HarmonyPatch(typeof(EntityBase), "SetMorphValue", new[] { typeof(string), typeof(float) })]
    static class MorphUserChangeStringFix
    {
        static void Prefix(EntityBase __instance, string morphName, float weight)
        {
            var humanoid = __instance as Humanoid;
            if (humanoid == null) return;

            var blinkMorph = Traverse.Create(humanoid).Field("_blinkingMorph").GetValue<EntityMorphData>();
            if (blinkMorph == null) return;

            if (blinkMorph.Name == morphName)
            {
                Traverse.Create(humanoid).Field("_blinkMorphUserState").SetValue(weight);
            }
        }
    }

    // === Fix 9a: Force SkinnedMeshRenderers to always update ===
    // Prevents frustum culling from hiding face/body parts at close range.
    // The game's IsPosed setter overrides updateWhenOffscreen, so we need
    // to force it on multiple paths and also expand mesh bounds.
    [HarmonyPatch(typeof(EntityBase), "FinishInitialization")]
    static class ForceRendererUpdate
    {
        static void Postfix(EntityBase __instance)
        {
            if (!__instance.isGiantess && !__instance.isHumanoid) return;
            ForceAllRenderers(__instance);
        }

        public static void ForceAllRenderers(EntityBase entity)
        {
            var renderers = entity.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers)
            {
                r.updateWhenOffscreen = true;
                // Also expand local bounds so Unity doesn't cull submeshes
                var bounds = r.localBounds;
                bounds.Expand(bounds.size * 2f);
                r.localBounds = bounds;
            }
        }
    }

    // Re-force when pose mode changes (game resets updateWhenOffscreen here)
    [HarmonyPatch(typeof(Humanoid), "IsPosed", MethodType.Setter)]
    static class ForceRendererOnPoseChange
    {
        static void Postfix(Humanoid __instance)
        {
            var renderers = __instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers)
            {
                r.updateWhenOffscreen = true;
            }
        }
    }

    // === Fix 9b: MMD model double-sided face rendering ===
    // MMD shaders are unsupported on this GPU (RTX 5060 Ti) and fall back to
    // basic shaders with backface culling. Since no MMD shader works,
    // replace with a custom double-sided shader built from Standard.
    [HarmonyPatch(typeof(EntityBase), "InitializeMorphs")]
    static class MMDDoubleSidedFix
    {
        static Material doubleSidedTemplate;
        static bool initialized;

        static void EnsureTemplate()
        {
            if (initialized) return;
            initialized = true;

            // Create a template material with cull off using a shader that supports it
            // "Standard" shader supports _Cull property when we create it fresh
            var shader = Shader.Find("Standard");
            if (shader != null)
            {
                doubleSidedTemplate = new Material(shader);
                doubleSidedTemplate.SetFloat("_Mode", 0); // Opaque
                doubleSidedTemplate.SetFloat("_Cull", 0); // Off
            }
        }

        static void Postfix(EntityBase __instance)
        {
            if (!__instance.isGiantess && !__instance.isHumanoid) return;
            EnsureTemplate();

            var renderers = __instance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat == null || mat.shader == null) continue;
                    string sn = mat.shader.name;

                    // Only fix materials using broken MMD shaders or their fallbacks
                    bool needsFix = sn.Contains("MMD") || sn.Contains("InternalErrorShader")
                                 || sn == "Hidden/InternalErrorShader";

                    if (!needsFix) continue;

                    // Copy main texture and color to a fresh Standard shader material
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                    mat.shader = Shader.Find("Standard");
                    if (mat.shader == null) continue;

                    // Restore texture and color
                    if (mainTex != null) mat.SetTexture("_MainTex", mainTex);
                    mat.SetColor("_Color", color);

                    // Set rendering to double-sided
                    mat.SetFloat("_Cull", 0f); // Cull Off

                    // Make it look decent - no metallic, smooth
                    mat.SetFloat("_Metallic", 0f);
                    mat.SetFloat("_Glossiness", 0.3f);
                }
            }
        }
    }

    // === Feature: Morph Preset Save/Load ===
    // Simple Save/Load — saves all morphs to a file per model, loads them back.
    [System.Serializable]
    public class MorphPreset
    {
        public string[] names;
        public float[] weights;
    }

    [HarmonyPatch(typeof(MorphsView), "Awake")]
    static class MorphPresetButtons
    {
        static void Postfix(MorphsView __instance)
        {
            try
            {
                Plugin.Log.LogInfo("[MorphPreset] MorphsView.Awake postfix fired");

                var buttons = __instance.GetComponentsInChildren<Button>(true);
                Plugin.Log.LogInfo("[MorphPreset] Found " + buttons.Length + " buttons in MorphsView");

                if (buttons.Length < 1)
                {
                    Plugin.Log.LogWarning("[MorphPreset] No buttons found, cannot create Save/Load");
                    return;
                }

                Transform buttonParent = buttons[0].transform.parent;
                Plugin.Log.LogInfo("[MorphPreset] Button parent: " + buttonParent.name);

                // Shift the original button row up to make room for our row below
                var origRect = buttonParent.GetComponent<RectTransform>();
                var origPos = origRect.anchoredPosition;
                origRect.anchoredPosition = new Vector2(origPos.x, origPos.y + 16f);

                // Clone the button row and position it below the original
                var row2 = Object.Instantiate(buttonParent.gameObject, buttonParent.parent);
                row2.name = "MorphPresetButtons";
                var row2Rect = row2.GetComponent<RectTransform>();
                row2Rect.anchoredPosition = new Vector2(origPos.x, origPos.y - 12f);

                // Remove cloned children
                for (int i = row2.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(row2.transform.GetChild(i).gameObject);

                // Add Save/Load/Reset
                var saveBtn = CreateButton(row2.transform, buttons[0].gameObject, "Save", DoSave);
                var loadBtn = CreateButton(row2.transform, buttons[0].gameObject, "Load", DoLoad);
                var resetBtn = CreateButton(row2.transform, buttons[0].gameObject, "Reset", DoReset);

                Plugin.Log.LogInfo("[MorphPreset] Save/Load buttons created");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[MorphPreset] Failed to create buttons: " + ex);
            }
        }

        static GameObject CreateButton(Transform parent, GameObject template, string label, UnityEngine.Events.UnityAction action)
        {
            var go = Object.Instantiate(template, parent);
            go.name = label;

            var btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);

            var txt = go.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = label;
            }
            else
            {
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(go.transform, false);
                var newTxt = textGo.AddComponent<Text>();
                newTxt.text = label;
                newTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                newTxt.fontSize = 14;
                newTxt.alignment = TextAnchor.MiddleCenter;
                newTxt.color = Color.white;
                var rt = textGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }

            go.SetActive(true);
            return go;
        }

        static string GetPresetPath(EntityBase entity)
        {
            if (entity == null || entity.asset == null) return null;

            var editor = entity.GetComponent<CharacterEditor>();
            string folder;
            if (editor != null)
            {
                folder = editor.FolderPath + "Morphs" + Path.DirectorySeparatorChar;
            }
            else
            {
                // Normalize: strip .gts/.micro extensions so path is consistent
                string name = entity.asset.AssetFullName.Replace("/", "-").Replace("\\", "-");
                name = System.Text.RegularExpressions.Regex.Replace(name, @"\.(gts|micro)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                folder = Path.Combine(Application.persistentDataPath, "Character", name, "Morphs");
            }

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, "morphs.json");
        }

        static void RefreshMorphsView()
        {
            // Toggle the MorphsView off/on to force slider refresh
            var morphsView = Object.FindObjectOfType<MorphsView>();
            if (morphsView != null && morphsView.gameObject.activeSelf)
            {
                morphsView.gameObject.SetActive(false);
                morphsView.gameObject.SetActive(true);
            }
        }

        static void DoSave()
        {
            try
            {
                Plugin.Log.LogInfo("[MorphPreset] Save clicked");

                var entity = InterfaceControl.instance != null ? InterfaceControl.instance.selectedEntity : null;
                if (entity == null)
                {
                    Plugin.Log.LogWarning("[MorphPreset] Save: no entity selected");
                    new Toast("_morphPreset").Print("Save: no entity selected");
                    return;
                }

                var morphs = entity.Morphs;
                if (morphs == null || morphs.Count == 0)
                {
                    Plugin.Log.LogWarning("[MorphPreset] Save: no morphs on entity");
                    new Toast("_morphPreset").Print("Save: no morphs on entity");
                    return;
                }

                string path = GetPresetPath(entity);
                if (path == null)
                {
                    Plugin.Log.LogWarning("[MorphPreset] Save: could not determine path");
                    return;
                }

                var names = new System.Collections.Generic.List<string>();
                var weights = new System.Collections.Generic.List<float>();

                for (int i = 0; i < morphs.Count; i++)
                {
                    if (morphs[i].Weight > 0.001f)
                    {
                        names.Add(morphs[i].Name);
                        weights.Add(morphs[i].Weight);
                    }
                }

                var preset = new MorphPreset { names = names.ToArray(), weights = weights.ToArray() };
                File.WriteAllText(path, JsonUtility.ToJson(preset, true));
                Plugin.Log.LogInfo("[MorphPreset] Saved " + names.Count + " morphs -> " + path);
                new Toast("_morphPreset").Print("Saved " + names.Count + " morphs");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[MorphPreset] Save failed: " + ex);
            }
        }

        static void DoLoad()
        {
            try
            {
                Plugin.Log.LogInfo("[MorphPreset] Load clicked");

                var entity = InterfaceControl.instance != null ? InterfaceControl.instance.selectedEntity : null;
                if (entity == null)
                {
                    Plugin.Log.LogWarning("[MorphPreset] Load: no entity selected");
                    new Toast("_morphPreset").Print("Load: no entity selected");
                    return;
                }

                string path = GetPresetPath(entity);
                if (path == null || !File.Exists(path))
                {
                    Plugin.Log.LogWarning("[MorphPreset] Load: no preset at " + (path ?? "null"));
                    new Toast("_morphPreset").Print("No saved morphs found for this model");
                    return;
                }

                var preset = JsonUtility.FromJson<MorphPreset>(File.ReadAllText(path));
                if (preset.names == null || preset.weights == null) return;

                // Reset all to 0
                for (int i = 0; i < entity.Morphs.Count; i++)
                    entity.SetMorphValue(i, 0f);

                // Apply saved values
                for (int i = 0; i < preset.names.Length; i++)
                    entity.SetMorphValue(preset.names[i], preset.weights[i]);

                Plugin.Log.LogInfo("[MorphPreset] Loaded " + preset.names.Length + " morphs from " + path);
                new Toast("_morphPreset").Print("Loaded " + preset.names.Length + " morphs");

                // Refresh the morph sliders UI so they reflect the new values
                RefreshMorphsView();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[MorphPreset] Load failed: " + ex);
                new Toast("_morphPreset").Print("Load failed: " + ex.Message);
            }
        }

        static void DoReset()
        {
            var entity = InterfaceControl.instance != null ? InterfaceControl.instance.selectedEntity : null;
            if (entity == null)
            {
                new Toast("_morphPreset").Print("Reset: no entity selected");
                return;
            }

            var morphs = entity.Morphs;
            if (morphs == null || morphs.Count == 0)
            {
                new Toast("_morphPreset").Print("Reset: no morphs on entity");
                return;
            }

            for (int i = 0; i < morphs.Count; i++)
                entity.SetMorphValue(i, 0f);

            new Toast("_morphPreset").Print("Reset " + morphs.Count + " morphs to zero");
            RefreshMorphsView();
        }
    }

    // === Fix 10: Japanese morph name translation ===
    // MMD models have Japanese morph names. The game's built-in translator is incomplete.
    // This patches InitializeMorphs to translate names after they're loaded.
    [HarmonyPatch(typeof(EntityBase), "InitializeMorphs")]
    static class MorphNameTranslator
    {
        static System.Collections.Generic.Dictionary<string, string> translations;

        static void InitTranslations()
        {
            if (translations != null) return;
            translations = new System.Collections.Generic.Dictionary<string, string>
            {
                // Eyes
                {"まばたき", "Blink"}, {"ウィンク", "Wink"},
                // NOTE: "笑い" intentionally NOT translated — the blink system uses
                // FindMorphByNameContains("笑い") to find the blink morph. Translating
                // it breaks blinking on MMD models.
                {"ウィンク右", "Wink Right"}, {"ウィンク２", "Wink2"}, {"ウィンク2", "Wink2"},
                {"ウィンク2右", "Wink2 Right"}, {"ウインク2右", "Wink2 Right"},
                {"ウインク", "Wink"}, {"ウインク右", "Wink Right"},

                // Expressions
                {"真面目", "Serious"}, {"真面目左", "Serious Left"}, {"真面目右", "Serious Right"},
                {"怒り", "Angry"}, {"怒り左", "Angry Left"}, {"怒り右", "Angry Right"},
                {"困る", "Troubled"}, {"困る左", "Troubled Left"}, {"困る右", "Troubled Right"},
                {"にこり", "Grin"}, {"にこり左", "Grin Left"}, {"にこり右", "Grin Right"},
                {"喜び", "Joy"}, {"悲しい", "Sad"}, {"驚き", "Surprise"},
                {"てへぺろ", "Tongue Out"}, {"じと目", "Stare"},

                // Mouth
                {"あ", "Mouth A"}, {"い", "Mouth I"}, {"う", "Mouth U"},
                {"え", "Mouth E"}, {"お", "Mouth O"},
                {"ア", "Mouth A"}, {"イ", "Mouth I"}, {"ウ", "Mouth U"},
                {"エ", "Mouth E"}, {"オ", "Mouth O"},
                {"△", "Mouth Triangle"}, {"∧", "Mouth Lambda"},
                {"ω", "Mouth Omega"}, {"ワ", "Mouth Wa"},
                {"口角上げ", "Mouth Corners Up"}, {"口角下げ", "Mouth Corners Down"},
                {"口横広げ", "Mouth Widen"}, {"口すぼめ", "Mouth Pucker"},

                // Eyebrows
                {"上", "Up"}, {"下", "Down"},
                {"眉上げ", "Eyebrows Raise"}, {"眉下げ", "Eyebrows Lower"},

                // Directions / modifiers
                {"左", "Left"}, {"右", "Right"},
                {"前", "Front"}, {"後", "Back"},
                {"大", "Large"}, {"小", "Small"},

                // Body
                {"胸", "Chest"}, {"腰", "Waist"}, {"お腹", "Belly"},
                {"乳首", "Nipple"}, {"乳首左", "Nipple Left"}, {"乳首右", "Nipple Right"},

                // Other common
                {"照れ", "Blush"}, {"青ざめ", "Pale"},
                {"涙", "Tears"}, {"額", "Forehead"},
                {"がーん", "Shocked"}, {"はぅ", "Hau"},
                {"瞳小", "Small Pupils"}, {"瞳大", "Large Pupils"},
                {"星目", "Star Eyes"}, {"はぁと", "Heart Eyes"},
                {"ハイライト消", "Highlights Off"},
                {"目の影消", "Eye Shadow Off"},
                {"輪郭", "Outline"},

                // Squint variants (already English but partial)
                {"L_SQUINT", "Left Squint"}, {"R_SQUINT", "Right Squint"},
                {"L_SQUINCH", "Left Squinch"}, {"R_SQUINCH", "Right Squinch"},
                {"FULL_SQUINCH", "Full Squinch"}, {"FULL_SQUINT", "Full Squint"},
            };
        }

        static void Postfix(EntityBase __instance)
        {
            InitTranslations();
            var morphs = __instance.Morphs;
            if (morphs == null) return;

            for (int i = 0; i < morphs.Count; i++)
            {
                string name = morphs[i].Name;
                if (string.IsNullOrEmpty(name)) continue;

                // Direct match
                if (translations.ContainsKey(name))
                {
                    morphs[i].Name = translations[name];
                    continue;
                }

                // Try translating parts (e.g. "にこり左" -> check "にこり" + "左")
                string translated = name;
                bool changed = false;
                foreach (var kvp in translations)
                {
                    if (name.Contains(kvp.Key) && kvp.Key.Length > 1)
                    {
                        translated = translated.Replace(kvp.Key, kvp.Value);
                        changed = true;
                    }
                }

                // Second pass: translate single-char suffixes that were skipped
                if (!changed && translated == name)
                {
                    // No multi-char match, skip
                }
                else
                {
                    name = translated;
                }

                // Always do suffix replacement for Left/Right
                if (name.EndsWith("左"))
                    name = name.Substring(0, name.Length - 1) + " Left";
                else if (name.EndsWith("右"))
                    name = name.Substring(0, name.Length - 1) + " Right";

                if (name != morphs[i].Name)
                {
                    morphs[i].Name = name;
                }
            }
        }
    }

    // === Fix 11: NaN/zero-scale guard for BodyPhysics ===
    // BodyPhysics divides by lossyScale.y in SetHairPhysics, SetJigglePhysics,
    // PlaceHairHeadCollider, PlaceTorsoCollider, and BreastGrowth.
    // When scale approaches zero these produce NaN/Infinity, which propagates
    // through DynamicBone particles causing hair/jiggle/breast physics to explode,
    // colliders to go haywire (triggering mass collision events + loud stacking audio).

    // Guard SetHairPhysics — radius = 0.003f / lossyScale.y
    [HarmonyPatch(typeof(BodyPhysics), "SetHairPhysics")]
    static class BodyPhysicsHairScaleFix
    {
        static void Postfix(BodyPhysics __instance)
        {
            foreach (var db in __instance.GetComponentsInChildren<DynamicBone>(true))
            {
                if (float.IsNaN(db.m_Radius) || float.IsInfinity(db.m_Radius) || db.m_Radius > 1000f)
                {
                    db.m_Radius = 0.003f;
                    Plugin.Log.LogWarning("[Fix11] Clamped NaN/huge DynamicBone radius in hair physics");
                }
            }
        }
    }

    // Guard SetJigglePhysics — same issue
    [HarmonyPatch(typeof(BodyPhysics), "SetJigglePhysics")]
    static class BodyPhysicsJiggleScaleFix
    {
        static void Postfix(BodyPhysics __instance)
        {
            foreach (var db in __instance.GetComponentsInChildren<DynamicBone>(true))
            {
                if (float.IsNaN(db.m_Radius) || float.IsInfinity(db.m_Radius) || db.m_Radius > 1000f)
                {
                    db.m_Radius = 0.003f;
                    Plugin.Log.LogWarning("[Fix11] Clamped NaN/huge DynamicBone radius in jiggle physics");
                }
            }
        }
    }

    // Guard PlaceTorsoCollider — radius and height divided by lossyScale.y
    [HarmonyPatch(typeof(BodyPhysics), "PlaceTorsoCollider")]
    static class BodyPhysicsTorsoColliderFix
    {
        static void Postfix(BodyPhysics __instance)
        {
            foreach (var col in __instance.GetComponentsInChildren<DynamicBoneCollider>(true))
            {
                if (float.IsNaN(col.m_Radius) || float.IsInfinity(col.m_Radius) || col.m_Radius > 1000f)
                {
                    col.m_Radius = 0.07f;
                    Plugin.Log.LogWarning("[Fix11] Clamped NaN/huge collider radius in torso");
                }
                if (float.IsNaN(col.m_Height) || float.IsInfinity(col.m_Height) || col.m_Height > 1000f)
                {
                    col.m_Height = 0.5f;
                }
            }
        }
    }

    // Guard BreastGrowth coroutine — xOffset = ... / lossyScale.y
    // Also prevent the coroutine from running at all if scale is dangerously small
    [HarmonyPatch(typeof(BodyPhysics), "StartBe")]
    static class BodyPhysicsBreastGrowthFix
    {
        static bool Prefix(BodyPhysics __instance)
        {
            float scaleY = __instance.transform.lossyScale.y;
            if (scaleY < 0.0001f || float.IsNaN(scaleY))
            {
                Plugin.Log.LogWarning("[Fix11] Blocked breast expansion — scale too small: " + scaleY);
                return false;
            }
            return true;
        }
    }

    // === Fix 12: DynamicBone NaN guard ===
    // Guards against NaN/Infinity positions and bad m_ObjectScale that cause
    // the entire bone chain to explode. Only resets on actual data corruption,
    // not on large-but-valid movement (which is normal for giantess-scale models).
    [HarmonyPatch(typeof(DynamicBone), "LateUpdate")]
    static class DynamicBoneNaNGuard
    {
        static FieldInfo _particlesField;
        static FieldInfo _positionField;
        static FieldInfo _objectScaleField;

        static void Prepare()
        {
            _particlesField = AccessTools.Field(typeof(DynamicBone), "m_Particles");
            _objectScaleField = AccessTools.Field(typeof(DynamicBone), "m_ObjectScale");
            var particleType = AccessTools.Inner(typeof(DynamicBone), "Particle");
            if (particleType != null)
                _positionField = AccessTools.Field(particleType, "m_Position");
        }

        static void Prefix(DynamicBone __instance)
        {
            // Guard m_ObjectScale (set from lossyScale.x in UpdateDynamicBones)
            float objScale = (float)_objectScaleField.GetValue(__instance);
            if (float.IsNaN(objScale) || float.IsInfinity(objScale) || objScale < 0.0001f)
            {
                _objectScaleField.SetValue(__instance, 1f);
                __instance.ResetParticles();
                Plugin.Log.LogWarning("[Fix12] Reset DynamicBone — bad ObjectScale on " + __instance.name);
                return;
            }

            if (_particlesField == null || _positionField == null) return;

            var particles = _particlesField.GetValue(__instance) as System.Collections.IList;
            if (particles == null || particles.Count == 0) return;

            bool needsReset = false;
            foreach (var p in particles)
            {
                var pos = (Vector3)_positionField.GetValue(p);

                // Only reset on actual NaN/Infinity — not on large values
                if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z) ||
                    float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z))
                {
                    needsReset = true;
                    break;
                }
            }

            if (needsReset)
            {
                __instance.ResetParticles();
                Plugin.Log.LogWarning("[Fix12] Reset DynamicBone — NaN particle on " + __instance.name);
            }
        }
    }

    // === Fix 14: SoundManager NaN pitch/volume guard ===
    // When entity.Scale or Height is NaN/zero, Mathf.Log10 produces -Infinity,
    // corrupting pitch and volume. Audio goes crazy with distorted looping sounds.
    [HarmonyPatch(typeof(SoundManager), "OnNotify")]
    static class SoundManagerNaNGuard
    {
        static bool Prefix(IEvent e)
        {
            var stepEvent = e as StepEvent;
            if (stepEvent == null || stepEvent.entity == null) return false;

            float scale = stepEvent.entity.Scale;
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
                return false; // Skip this sound entirely

            float height = stepEvent.entity.Height;
            if (float.IsNaN(height) || float.IsInfinity(height) || height <= 0f)
                return false;

            return true;
        }
    }

    // === Fix 13: ColliderReshaper zero-scale guard ===
    // UpdateSingleDynamicMesh does 1f / transform.lossyScale.y with no check.
    // Skip the mesh update entirely if scale is too small.
    [HarmonyPatch(typeof(ColliderReshaper), "Update")]
    static class ColliderReshaperScaleGuard
    {
        static bool Prefix(ColliderReshaper __instance)
        {
            if (__instance.giantess == null) return true;

            float scaleY = __instance.giantess.transform.lossyScale.y;
            if (scaleY < 0.0001f || float.IsNaN(scaleY) || float.IsInfinity(scaleY))
            {
                return false; // Skip update entirely — scale is invalid
            }
            return true;
        }
    }

    // === Feature: Load button in pause menu ===
    // The game's pause menu has Save but no Load. This adds a Load button
    // that shows a list of saved files and loads the selected one.
    [HarmonyPatch]
    static class PauseMenuLoadButton
    {
        // PauseView is a nested class inside GuiManager
        static System.Type _pauseViewType;
        static FieldInfo _buttonLayoutField;
        static FieldInfo _buttonPrefabField;
        static MethodInfo _addButtonMethod;
        internal static GameObject _activeLoadPanel;

        static bool Prepare()
        {
            _pauseViewType = AccessTools.TypeByName("SizeboxUI.PauseView");
            if (_pauseViewType == null)
            {
                Plugin.Log?.LogWarning("[LoadButton] Could not find GuiManager+PauseView type");
                return false;
            }
            _buttonLayoutField = AccessTools.Field(_pauseViewType, "_buttonLayout");
            _buttonPrefabField = AccessTools.Field(_pauseViewType, "_buttonPrefab");
            _addButtonMethod = AccessTools.Method(_pauseViewType, "AddButton", new[] { typeof(string) });
            return _addButtonMethod != null;
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(_pauseViewType, "Awake");
        }

        static void Postfix(MonoBehaviour __instance)
        {
            try
            {
                // Use the game's own AddButton method to create a matching button
                var loadBtn = (Button)_addButtonMethod.Invoke(__instance, new object[] { "Load" });

                // Move the Load button to be right after Save (index 3, after Resume/Restart/Save)
                var layout = (GridLayoutGroup)_buttonLayoutField.GetValue(__instance);
                loadBtn.transform.SetSiblingIndex(3);

                loadBtn.onClick.AddListener(() => OnLoadClick(__instance));

                // Expand the grid container to fit the extra button
                var layoutRect = layout.GetComponent<RectTransform>();
                if (layoutRect != null)
                {
                    var size = layoutRect.sizeDelta;
                    var cellH = layout.cellSize.y + layout.spacing.y;
                    layoutRect.sizeDelta = new Vector2(size.x, size.y + cellH);
                }

                // Add AI Settings button after Load
                var aiBtn = (Button)_addButtonMethod.Invoke(__instance, new object[] { "AI Settings" });
                aiBtn.transform.SetSiblingIndex(4);
                aiBtn.onClick.AddListener(() =>
                {
                    // Close pause menu and open settings panel
                    if (GameController.Instance != null)
                        GameController.Instance.SetPausedState(false);
                    var guiMgr = SizeboxUI.GuiManager.Instance;
                    if (guiMgr != null)
                        guiMgr.ClosePauseMenu();
                    AISettingsPanel.Show();
                });

                // Expand grid for both extra buttons
                var layoutRect2 = layout.GetComponent<RectTransform>();
                if (layoutRect2 != null)
                {
                    var size2 = layoutRect2.sizeDelta;
                    var cellH2 = layout.cellSize.y + layout.spacing.y;
                    layoutRect2.sizeDelta = new Vector2(size2.x, size2.y + cellH2);
                }

                Plugin.Log.LogInfo("[LoadButton] Load button added to pause menu");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[LoadButton] Failed: " + ex);
            }
        }

        static void OnLoadClick(MonoBehaviour pauseView)
        {
            try
            {
                // Get saved files list
                var files = IOManager.Instance.GetListSavedFiles();
                if (files == null || files.Length == 0)
                {
                    new Toast("_loadMenu").Print("No saved scenes found");
                    return;
                }

                // Close existing load panel if open
                if (_activeLoadPanel != null)
                {
                    Object.Destroy(_activeLoadPanel);
                    _activeLoadPanel = null;
                    return;
                }

                // Create a simple load menu panel
                var panel = new GameObject("LoadMenu");
                panel.transform.SetParent(pauseView.transform, false);
                _activeLoadPanel = panel;

                var panelImg = panel.AddComponent<Image>();
                panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

                var panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.sizeDelta = Vector2.zero;

                // Scroll view container
                var scrollGo = new GameObject("Scroll");
                scrollGo.transform.SetParent(panel.transform, false);
                var scrollRect = scrollGo.AddComponent<RectTransform>();
                scrollRect.anchorMin = new Vector2(0.2f, 0.15f);
                scrollRect.anchorMax = new Vector2(0.8f, 0.85f);
                scrollRect.sizeDelta = Vector2.zero;

                var scrollView = scrollGo.AddComponent<ScrollRect>();

                // Content for scroll
                var contentGo = new GameObject("Content");
                contentGo.transform.SetParent(scrollGo.transform, false);
                var contentRect = contentGo.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.sizeDelta = new Vector2(0f, files.Length * 40f);

                var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 5f;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var csf = contentGo.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollView.content = contentRect;
                scrollView.vertical = true;
                scrollView.horizontal = false;

                // Add file buttons
                foreach (var file in files)
                {
                    string filename = file.Replace(".json", "");
                    var btnGo = new GameObject(filename);
                    btnGo.transform.SetParent(contentGo.transform, false);

                    var btnImg = btnGo.AddComponent<Image>();
                    btnImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

                    var le = btnGo.AddComponent<LayoutElement>();
                    le.preferredHeight = 35f;

                    var btn = btnGo.AddComponent<Button>();
                    btn.targetGraphic = btnImg;

                    var colors = btn.colors;
                    colors.highlightedColor = new Color(0.4f, 0.4f, 0.6f, 1f);
                    btn.colors = colors;

                    var textGo = new GameObject("Text");
                    textGo.transform.SetParent(btnGo.transform, false);
                    var txt = textGo.AddComponent<Text>();
                    txt.text = filename;
                    txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    txt.fontSize = 18;
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.color = Color.white;
                    var txtRect = textGo.GetComponent<RectTransform>();
                    txtRect.anchorMin = Vector2.zero;
                    txtRect.anchorMax = Vector2.one;
                    txtRect.sizeDelta = Vector2.zero;

                    string fileToLoad = file;
                    btn.onClick.AddListener(() =>
                    {
                        LoadSavedFile(fileToLoad, panel);
                    });
                }

                // Close button
                var closeBtnGo = new GameObject("CloseButton");
                closeBtnGo.transform.SetParent(panel.transform, false);
                var closeRect = closeBtnGo.AddComponent<RectTransform>();
                closeRect.anchorMin = new Vector2(0.35f, 0.03f);
                closeRect.anchorMax = new Vector2(0.65f, 0.1f);
                closeRect.sizeDelta = Vector2.zero;

                var closeImg = closeBtnGo.AddComponent<Image>();
                closeImg.color = new Color(0.5f, 0.2f, 0.2f, 1f);

                var closeBtn = closeBtnGo.AddComponent<Button>();
                closeBtn.targetGraphic = closeImg;
                closeBtn.onClick.AddListener(() => Object.Destroy(panel));

                var closeTextGo = new GameObject("Text");
                closeTextGo.transform.SetParent(closeBtnGo.transform, false);
                var closeTxt = closeTextGo.AddComponent<Text>();
                closeTxt.text = "Cancel";
                closeTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                closeTxt.fontSize = 18;
                closeTxt.alignment = TextAnchor.MiddleCenter;
                closeTxt.color = Color.white;
                var closeTxtRect = closeTextGo.GetComponent<RectTransform>();
                closeTxtRect.anchorMin = Vector2.zero;
                closeTxtRect.anchorMax = Vector2.one;
                closeTxtRect.sizeDelta = Vector2.zero;

                // Title
                var titleGo = new GameObject("Title");
                titleGo.transform.SetParent(panel.transform, false);
                var titleRect = titleGo.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.2f, 0.87f);
                titleRect.anchorMax = new Vector2(0.8f, 0.95f);
                titleRect.sizeDelta = Vector2.zero;
                var titleTxt = titleGo.AddComponent<Text>();
                titleTxt.text = "Load Scene";
                titleTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                titleTxt.fontSize = 24;
                titleTxt.alignment = TextAnchor.MiddleCenter;
                titleTxt.color = Color.white;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[LoadButton] OnLoadClick failed: " + ex);
            }
        }

        static System.Collections.IEnumerator RebuildAfterDelay()
        {
            // Wait 2 frames for scene to fully initialize
            yield return null;
            yield return null;
            Plugin.Log.LogInfo("[LoadButton] Rebuilding scene after map switch");
            SavedScenesManager.Instance.ReBuildScene();
        }

        static void ClearAllEntities()
        {
            try
            {
                // Deselect current entity first — prevents stale reference bugs
                // (invisible skeleton UI, broken handles, etc.)
                if (InterfaceControl.instance != null)
                    InterfaceControl.instance.SetSelectedObject(null);

                // Destroy all existing entities so loading starts fresh
                var allEntities = Object.FindObjectsOfType<EntityBase>();
                int count = 0;
                foreach (var entity in allEntities)
                {
                    if (entity != null && entity.gameObject != null)
                    {
                        entity.DestroyObject();
                        count++;
                    }
                }
                Plugin.Log.LogInfo("[LoadButton] Cleared " + count + " entities");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[LoadButton] ClearAllEntities failed: " + ex.Message);
            }
        }

        static void LoadSavedFile(string filename, GameObject panel)
        {
            try
            {
                if (!filename.EndsWith(".json"))
                    filename += ".json";

                // Build full path using the saves folder (private field _folderSaves)
                var folderField = AccessTools.Field(typeof(IOManager), "_folderSaves");
                string folder = (string)folderField.GetValue(IOManager.Instance);
                string fullPath = folder + Path.DirectorySeparatorChar + filename;

                Plugin.Log.LogInfo("[LoadButton] Loading: " + fullPath);
                string result = IOManager.Instance.LoadFile(fullPath);
                if (result == null)
                {
                    new Toast("_loadMenu").Print("Failed to load: " + filename);
                    return;
                }

                Object.Destroy(panel);

                // Parse the scene name from the save data
                var saveData = JsonUtility.FromJson<SaveDataStructures.SaveData>(result);
                string savedScene = saveData != null ? saveData.scene : null;
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                // Unpause and close the pause menu UI
                if (GameController.Instance != null)
                    GameController.Instance.SetPausedState(false);
                var guiMgr = SizeboxUI.GuiManager.Instance;
                if (guiMgr != null)
                    guiMgr.ClosePauseMenu();

                // Always reload the scene (even same scene) so Unity properly
                // destroys old entities before we spawn new ones. Direct
                // ClearAllEntities + ReBuildScene causes crashes because
                // DestroyObject is deferred and old entities still exist
                // when SpawnEntities runs.
                string targetScene = !string.IsNullOrEmpty(savedScene) ? savedScene : currentScene;
                if (savedScene != currentScene)
                    Plugin.Log.LogInfo("[LoadButton] Switching scene: " + currentScene + " -> " + savedScene);
                else
                    Plugin.Log.LogInfo("[LoadButton] Reloading scene: " + currentScene);

                UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode> callback = null;
                callback = (scene, mode) =>
                {
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= callback;
                    GameController.Instance.StartCoroutine(RebuildAfterDelay());
                };
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += callback;

                SavedScenesManager.Instance.LoadScene(targetScene);

                new Toast("_loadMenu").Print("Loaded: " + filename.Replace(".json", ""));
                Plugin.Log.LogInfo("[LoadButton] Loaded scene: " + filename);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[LoadButton] Load failed: " + ex);
                new Toast("_loadMenu").Print("Load failed: " + ex.Message);
            }
        }
    }

    // Patch PauseView.CloseIfOpen so Escape closes our Load panel
    [HarmonyPatch]
    static class PauseMenuLoadPanelEscape
    {
        static System.Type _pauseViewType;

        static bool Prepare()
        {
            _pauseViewType = AccessTools.TypeByName("SizeboxUI.PauseView");
            return _pauseViewType != null;
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(_pauseViewType, "CloseIfOpen");
        }

        static void Postfix(ref bool __result)
        {
            if (PauseMenuLoadButton._activeLoadPanel != null)
            {
                Object.Destroy(PauseMenuLoadButton._activeLoadPanel);
                PauseMenuLoadButton._activeLoadPanel = null;
                __result = true; // Tell PauseView we closed something
            }
        }
    }

    // === Fix 15: Material Presets scroll fix ===
    // The presets list has no ScrollRect, so when there are many presets
    // you can't scroll down to see them all.
    [HarmonyPatch]
    static class MaterialPresetScrollFix
    {
        static System.Type _basePresetsType;

        static bool Prepare()
        {
            _basePresetsType = AccessTools.TypeByName("SizeboxUI.BasePresetsView")
                ?? AccessTools.TypeByName("BasePresetsView");
            return _basePresetsType != null;
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(_basePresetsType, "OnEnable");
        }

        static void Postfix(object __instance)
        {
            try
            {
                var fileEntryParentField = AccessTools.Field(_basePresetsType, "fileEntryParent");
                if (fileEntryParentField == null) return;

                var fileEntryParent = fileEntryParentField.GetValue(__instance) as RectTransform;
                if (fileEntryParent == null) return;

                // Check if ScrollRect already exists on parent
                var existingScroll = fileEntryParent.GetComponentInParent<UnityEngine.UI.ScrollRect>();
                if (existingScroll != null) return;

                // Wrap the file entry parent in a scroll view
                Transform container = fileEntryParent.parent;
                if (container == null) return;

                // Add ScrollRect to the container
                var scrollRect = container.gameObject.GetComponent<UnityEngine.UI.ScrollRect>();
                if (scrollRect == null)
                    scrollRect = container.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();

                scrollRect.content = fileEntryParent;
                scrollRect.vertical = true;
                scrollRect.horizontal = false;
                scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 30f;

                // Add mask so entries don't render outside the container
                var mask = container.gameObject.GetComponent<UnityEngine.UI.Mask>();
                if (mask == null)
                    mask = container.gameObject.AddComponent<UnityEngine.UI.Mask>();
                mask.showMaskGraphic = true;

                // Ensure container has an Image for the mask
                var img = container.gameObject.GetComponent<UnityEngine.UI.Image>();
                if (img == null)
                {
                    img = container.gameObject.AddComponent<UnityEngine.UI.Image>();
                    img.color = new Color(0, 0, 0, 0.01f); // Nearly invisible
                }

                // Make content expand with entries
                var csf = fileEntryParent.gameObject.GetComponent<ContentSizeFitter>();
                if (csf == null)
                    csf = fileEntryParent.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                Plugin.Log.LogInfo("[Fix15] Added scroll to presets view");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[Fix15] Preset scroll fix failed: " + ex.Message);
            }
        }
    }

    // === Fix 16: ObjectManager memory leak ===
    // _OnObjectRemoved() has a copy-paste bug — it ADDS the entity back to the dictionary
    // and fires OnObjectAdd instead of removing. Same bug in _OnMicroRemoved().
    // This causes every destroyed micro and object to leak in memory forever,
    // making the game progressively slower during long sessions.
    [HarmonyPatch(typeof(ObjectManager), "_OnObjectRemoved")]
    static class ObjectManagerRemoveFix
    {
        static bool Prefix(ObjectManager __instance, EntityBase entity)
        {
            // Do the correct thing: REMOVE from dictionary
            __instance.ObjectDictionary.Remove(entity.id);
            return false; // Skip the broken original
        }
    }

    [HarmonyPatch(typeof(ObjectManager), "_OnMicroRemoved")]
    static class MicroManagerRemoveFix
    {
        static bool Prefix(Micro micro)
        {
            // The original fires OnMicroAdd instead of OnMicroRemove.
            // We skip it — the entity is already unregistered from _entityDictionary
            // by UnregisterEntity before this is called. The MicroManager handles
            // its own dictionary separately.
            return false; // Skip the broken original
        }
    }

    // === Fix 17: EventManager dead listener leak ===
    // Null listeners are logged but never removed, accumulating forever.
    // This patches SendEvent to clean up dead listeners.
    [HarmonyPatch(typeof(EventManager), "SendEvent")]
    static class EventManagerCleanupFix
    {
        static bool Prefix(IEvent e)
        {
            var listenersField = AccessTools.Field(typeof(EventManager), "listeners");
            if (listenersField == null) return true;

            var listeners = listenersField.GetValue(null) as System.Collections.IList;
            if (listeners == null) return true;

            // Iterate backwards so we can safely remove
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                var listener = listeners[i];
                if (listener == null)
                {
                    listeners.RemoveAt(i);
                    continue;
                }

                // Access the listener's fields via reflection
                var listenerField = AccessTools.Field(listener.GetType(), "listener");
                var codeField = AccessTools.Field(listener.GetType(), "interestCode");
                if (listenerField == null || codeField == null) continue;

                var actualListener = listenerField.GetValue(listener) as IListener;
                if (actualListener == null)
                {
                    listeners.RemoveAt(i);
                    continue;
                }

                var code = codeField.GetValue(listener);
                if (code != null && code.Equals(e.code))
                {
                    actualListener.OnNotify(e);
                }
            }

            return false; // Skip original
        }
    }

    // === Version label on main menu ===
    [HarmonyPatch(typeof(MenuController), "Start")]
    static class MainMenuVersionLabel
    {
        static void Postfix(MenuController __instance)
        {
            try
            {
                // Add version text to bottom-right of the main menu canvas
                var canvas = __instance.menuUI;
                if (canvas == null) return;

                var labelGo = new GameObject("SizeboxFixVersion");
                labelGo.transform.SetParent(canvas.transform, false);

                var txt = labelGo.AddComponent<Text>();
                txt.text = "SizeboxFix v1.5.0";
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.fontSize = 16;
                txt.alignment = TextAnchor.LowerRight;
                txt.color = new Color(1f, 1f, 1f, 0.5f);

                var rect = labelGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-15f, 10f);
                rect.sizeDelta = new Vector2(200f, 25f);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[VersionLabel] " + ex.Message);
            }
        }
    }

    // === Fix 18: City performance optimizations ===

    // Phase 1: Cache renderer arrays so MakeBuildingsVisible/MakeRoadsVisible
    // don't call GetComponentsInChildren every toggle.
    // Phase 2: Per-building distance culling in Update.
    [HarmonyPatch(typeof(CityBuilder), "Update")]
    static class CityPerformanceFix
    {
        // Per-city cached data
        internal static Dictionary<int, Renderer[]> _buildingRendererCache = new Dictionary<int, Renderer[]>();
        internal static Dictionary<int, Renderer[]> _roadRendererCache = new Dictionary<int, Renderer[]>();
        static Dictionary<int, Transform[]> _buildingTransformCache = new Dictionary<int, Transform[]>();
        static Dictionary<int, int> _cullingIndex = new Dictionary<int, int>();

        const int BUILDINGS_PER_FRAME = 50;
        const float BUILDING_CULL_DIST_MULTIPLIER = 3f; // Cull buildings beyond 3x city radius

        static void Postfix(CityBuilder __instance)
        {
            int id = __instance.GetInstanceID();
            Camera cam = Camera.main;
            if (cam == null) return;

            // Per-building distance culling
            Transform[] buildingTransforms;
            if (!_buildingTransformCache.TryGetValue(id, out buildingTransforms))
            {
                // First time — cache building transforms
                var buildingRootField = AccessTools.Field(typeof(CityBuilder), "_buildingRoot");
                var buildingRoot = buildingRootField?.GetValue(__instance) as GameObject;
                if (buildingRoot == null) return;

                var buildings = buildingRoot.GetComponentsInChildren<Assets.Scripts.ProceduralCityGenerator.CityBuilding>(true);
                buildingTransforms = new Transform[buildings.Length];
                for (int i = 0; i < buildings.Length; i++)
                    buildingTransforms[i] = buildings[i].transform;

                _buildingTransformCache[id] = buildingTransforms;
                _cullingIndex[id] = 0;
                Plugin.Log.LogInfo("[CityOpt] Cached " + buildings.Length + " building transforms for culling");
            }

            if (buildingTransforms.Length == 0) return;

            // Process a batch of buildings per frame
            int startIdx;
            if (!_cullingIndex.TryGetValue(id, out startIdx))
                startIdx = 0;

            Vector3 camPos = cam.transform.position;
            float cityScale = __instance.Scale;
            float cullDistSqr = Mathf.Pow(cityScale * __instance.Height * BUILDING_CULL_DIST_MULTIPLIER, 2);

            int processed = 0;
            for (int i = startIdx; processed < BUILDINGS_PER_FRAME && i < buildingTransforms.Length; i++, processed++)
            {
                if (buildingTransforms[i] == null) continue;
                var renderer = buildingTransforms[i].GetComponent<Renderer>();
                if (renderer == null) continue;

                float distSqr = (camPos - buildingTransforms[i].position).sqrMagnitude;
                renderer.enabled = distSqr < cullDistSqr;
            }

            startIdx += processed;
            if (startIdx >= buildingTransforms.Length)
                startIdx = 0;
            _cullingIndex[id] = startIdx;
        }
    }

    // Phase 1: Cache renderers for MakeBuildingsVisible
    [HarmonyPatch(typeof(CityBuilder), "MakeBuildingsVisible")]
    static class CityRendererCacheFix
    {
        static bool Prefix(CityBuilder __instance, bool visible)
        {
            var buildingRootField = AccessTools.Field(typeof(CityBuilder), "_buildingRoot");
            var floorRootField = AccessTools.Field(typeof(CityBuilder), "_floorRoot");
            var floorMatField = AccessTools.Field(typeof(CityBuilder), "floorMaterial");
            var lowPolyField = AccessTools.Field(typeof(CityBuilder), "lowPoly");
            var visibleField = AccessTools.Field(typeof(CityBuilder), "_isBuildingsVisible");

            var buildingRoot = buildingRootField?.GetValue(__instance) as GameObject;
            var floorRoot = floorRootField?.GetValue(__instance) as GameObject;
            if (buildingRoot == null || floorRoot == null) return true;

            // Cache renderers
            int id = __instance.GetInstanceID();
            Renderer[] renderers;
            if (!CityPerformanceFix._buildingRendererCache.TryGetValue(id, out renderers))
            {
                renderers = buildingRoot.GetComponentsInChildren<Renderer>(true);
                CityPerformanceFix._buildingRendererCache[id] = renderers;
                Plugin.Log.LogInfo("[CityOpt] Cached " + renderers.Length + " building renderers");
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = visible;
            }

            var floorRenderer = floorRoot.GetComponent<MeshRenderer>();
            if (floorRenderer != null)
            {
                var mat = visible ? floorMatField?.GetValue(__instance) as Material
                                  : lowPolyField?.GetValue(__instance) as Material;
                if (mat != null) floorRenderer.material = mat;
            }

            visibleField?.SetValue(__instance, visible);
            return false; // Skip original
        }
    }

    // Phase 1: Cache renderers for MakeRoadsVisible
    [HarmonyPatch(typeof(CityBuilder), "MakeRoadsVisible")]
    static class CityRoadRendererCacheFix
    {
        static bool Prefix(CityBuilder __instance, bool visible)
        {
            var roadRootField = AccessTools.Field(typeof(CityBuilder), "_roadRoot");
            var visibleField = AccessTools.Field(typeof(CityBuilder), "_isRoadsVisible");

            var roadRoot = roadRootField?.GetValue(__instance) as GameObject;
            if (roadRoot == null) return true;

            int id = __instance.GetInstanceID();
            Renderer[] renderers;
            if (!CityPerformanceFix._roadRendererCache.TryGetValue(id, out renderers))
            {
                renderers = roadRoot.GetComponentsInChildren<Renderer>(true);
                CityPerformanceFix._roadRendererCache[id] = renderers;
                Plugin.Log.LogInfo("[CityOpt] Cached " + renderers.Length + " road renderers");
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = visible;
            }

            visibleField?.SetValue(__instance, visible);
            return false; // Skip original
        }
    }

    // Phase 3: Reduce debris lifetime from 20s to 6s
    [HarmonyPatch]
    static class CityDebrisLifetimeFix
    {
        static System.Type _cityBuildingType;

        static bool Prepare()
        {
            _cityBuildingType = AccessTools.TypeByName("Assets.Scripts.ProceduralCityGenerator.CityBuilding");
            return _cityBuildingType != null;
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(_cityBuildingType, "DebrisAnimation");
        }

        // Can't easily patch a coroutine's yield content, so instead patch
        // the Destroy calls. We'll use a Postfix on the building's destruction
        // to clean up faster.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Just let it run — we'll handle cleanup differently
            return instructions;
        }
    }

    // === AI Settings Panel ===
    // Opens from pause menu "AI Settings" button
    public class AISettingsPanel : MonoBehaviour
    {
        static AISettingsPanel _instance;
        bool _visible;
        Vector2 _scrollPos;
        Rect _windowRect = new Rect(100, 50, 550, 700);
        int _selectedTab; // 0 = Shared, -1 = Scenarios, 1+ = agent configs
        string _newScenarioName = "";

        bool _inputDisabled;

        void Update()
        {
            if (_visible && !_inputDisabled)
            {
                if (InputManager.inputs != null)
                    InputManager.inputs.Disable();
                _inputDisabled = true;
            }
            else if (!_visible && _inputDisabled)
            {
                if (InputManager.inputs != null)
                    InputManager.inputs.Enable();
                _inputDisabled = false;
            }
        }

        public static void Show()
        {
            if (_instance == null)
            {
                var go = new GameObject("AISettingsPanel");
                Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<AISettingsPanel>();
            }
            _instance._visible = true;
        }

        void OnGUI()
        {
            if (!_visible) return;
            _windowRect = GUI.Window(98765, _windowRect, DrawWindow, "AI Giantess Settings");
        }

        void DrawWindow(int id)
        {
            var mgr = AIGiantess.Instance;
            if (mgr == null || mgr._sharedConfig == null)
            {
                GUILayout.Label("AI Manager not loaded");
                if (GUILayout.Button("Close")) _visible = false;
                GUI.DragWindow();
                return;
            }

            var shared = mgr._sharedConfig;
            var agents = mgr._agentConfigs;

            // Tab buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_selectedTab == -1, "Scenarios", "Button", GUILayout.Height(30)))
                _selectedTab = -1;
            if (GUILayout.Toggle(_selectedTab == 0, "Shared", "Button", GUILayout.Height(30)))
                _selectedTab = 0;
            for (int i = 0; i < agents.Count; i++)
            {
                if (GUILayout.Toggle(_selectedTab == i + 1, agents[i].Name, "Button", GUILayout.Height(30)))
                    _selectedTab = i + 1;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            if (_selectedTab == -1)
            {
                GUILayout.Label("Custom Scenarios", BoldLabel());

                // Load saved custom scenarios
                string scenarioDir = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "SizeboxAI_Scenarios");
                if (System.IO.Directory.Exists(scenarioDir))
                {
                    foreach (var file in System.IO.Directory.GetFiles(scenarioDir, "*.txt"))
                    {
                        string scenarioName = System.IO.Path.GetFileNameWithoutExtension(file);
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(scenarioName, GUILayout.Height(25)))
                        {
                            LoadCustomScenario(file, agents);
                        }
                        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(25)))
                        {
                            System.IO.File.Delete(file);
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(10);
                GUILayout.Label("Save Current As:", EditorLabel());
                _newScenarioName = GUILayout.TextField(_newScenarioName ?? "", GUILayout.Height(22));
                if (GUILayout.Button("Save Scenario", GUILayout.Height(30)) && !string.IsNullOrEmpty(_newScenarioName))
                {
                    SaveCustomScenario(_newScenarioName, agents);
                    _newScenarioName = "";
                }

                GUILayout.Space(10);
                GUILayout.Label("Click a scenario to load. X to delete.", SmallLabel());
            }
            else if (_selectedTab == 0)
            {
                // Shared settings
                GUILayout.Label("Shared Settings", BoldLabel());
                GUILayout.Space(5);

                GUILayout.Label("AI Model:", EditorLabel());
                shared.Model = GUILayout.TextField(shared.Model, GUILayout.Height(22));

                GUILayout.Space(5);
                GUILayout.Label("Decision Interval (seconds):", EditorLabel());
                string intervalStr = GUILayout.TextField(shared.DecisionInterval.ToString(), GUILayout.Height(22));
                float parsed;
                if (float.TryParse(intervalStr, out parsed))
                    shared.DecisionInterval = parsed;

                GUILayout.Space(10);
                GUILayout.Label("Quick Model Select:", EditorLabel());
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Hermes 3", GUILayout.Height(25)))
                    shared.Model = "nousresearch/hermes-3-llama-3.1-70b";
                if (GUILayout.Button("Grok 4.1", GUILayout.Height(25)))
                    shared.Model = "x-ai/grok-4.1-fast";
                if (GUILayout.Button("Grok 4", GUILayout.Height(25)))
                    shared.Model = "x-ai/grok-4-fast";
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Grok 4.20", GUILayout.Height(25)))
                    shared.Model = "x-ai/grok-4.20";
                if (GUILayout.Button("Magnum v4", GUILayout.Height(25)))
                    shared.Model = "anthracite-org/magnum-v4-72b";
                if (GUILayout.Button("Hermes 4", GUILayout.Height(25)))
                    shared.Model = "nousresearch/hermes-4-70b";
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("GPT-OSS 120B", GUILayout.Height(25)))
                    shared.Model = "openai/gpt-oss-120b";
                if (GUILayout.Button("Dolphin 24B", GUILayout.Height(25)))
                    shared.Model = "cognitivecomputations/dolphin-mistral-24b-venice-edition:free";
                if (GUILayout.Button("Euryale 70B", GUILayout.Height(25)))
                    shared.Model = "sao10k/l3.3-euryale-70b";
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("DeepSeek V3.2", GUILayout.Height(25)))
                    shared.Model = "deepseek/deepseek-v3.2";
                if (GUILayout.Button("Mistral Large", GUILayout.Height(25)))
                    shared.Model = "mistralai/mistral-large-2512";
                if (GUILayout.Button("WizardLM 8x22B", GUILayout.Height(25)))
                    shared.Model = "microsoft/wizardlm-2-8x22b";
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Llama 3.3 70B", GUILayout.Height(25)))
                    shared.Model = "meta-llama/llama-3.3-70b-instruct";
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label("Default TTS Provider:", EditorLabel());
                GUILayout.BeginHorizontal();
                // Show the first agent's TTS as default
                if (agents.Count > 0)
                {
                    var def = agents[0];
                    if (GUILayout.Toggle(def.TtsProvider == "edge", "Edge", "Button"))
                        foreach (var a in agents) a.TtsProvider = "edge";
                    if (GUILayout.Toggle(def.TtsProvider == "fish", "Fish", "Button"))
                        foreach (var a in agents) a.TtsProvider = "fish";
                    if (GUILayout.Toggle(def.TtsProvider == "elevenlabs", "ElevenLabs", "Button"))
                        foreach (var a in agents) a.TtsProvider = "elevenlabs";
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (agents.Count > 0 && agents[0].TtsProvider == "fish")
                {
                    GUILayout.Label("Fish API Key (shared):", EditorLabel());
                    string fishKey = agents[0].TtsFishApiKey;
                    fishKey = GUILayout.PasswordField(fishKey, '*', GUILayout.Height(22));
                    foreach (var a in agents) a.TtsFishApiKey = fishKey;
                }

                GUILayout.Space(5);
                bool ttsOn = agents.Count > 0 && agents[0].TtsEnabled;
                bool newTts = GUILayout.Toggle(ttsOn, " TTS Enabled (all agents)");
                if (newTts != ttsOn)
                    foreach (var a in agents) a.TtsEnabled = newTts;

                GUILayout.Space(15);
                GUILayout.Label("Chat & Audio Settings", BoldLabel());

                GUILayout.Label("Max Words per Response: " + AIGiantess.maxWords, EditorLabel());
                AIGiantess.maxWords = (int)GUILayout.HorizontalSlider(AIGiantess.maxWords, 15, 100);

                GUILayout.Label("Chat Display Time: " + AIGiantess.chatDisplayTime.ToString("F0") + "s", EditorLabel());
                AIGiantess.chatDisplayTime = GUILayout.HorizontalSlider(AIGiantess.chatDisplayTime, 5, 60);

                GUILayout.Label("Max Chat Lines: " + AIGiantess.maxChatLines, EditorLabel());
                AIGiantess.maxChatLines = (int)GUILayout.HorizontalSlider(AIGiantess.maxChatLines, 3, 30);

                GUILayout.Label("Chat Font Size: " + AIGiantess.chatFontSize, EditorLabel());
                AIGiantess.chatFontSize = (int)GUILayout.HorizontalSlider(AIGiantess.chatFontSize, 10, 24);

                GUILayout.Label("Delay Between Agents: " + AIGiantess.chatStaggerDelay.ToString("F1") + "s", EditorLabel());
                AIGiantess.chatStaggerDelay = GUILayout.HorizontalSlider(AIGiantess.chatStaggerDelay, 0, 15);

                GUILayout.Label("Same Agent Auto-Talk: " + AIGiantess.autoTalkInterval.ToString("F0") + "s", EditorLabel());
                AIGiantess.autoTalkInterval = GUILayout.HorizontalSlider(AIGiantess.autoTalkInterval, 10, 120);

                GUILayout.Label("Audio Overlap Gap: " + AIGiantess.audioOverlapGap.ToString("F1") + "s", EditorLabel());
                AIGiantess.audioOverlapGap = GUILayout.HorizontalSlider(AIGiantess.audioOverlapGap, 0, 5);

                GUILayout.Label("Conversation Memory: " + AIGiantess.conversationMemory + " entries", EditorLabel());
                AIGiantess.conversationMemory = (int)GUILayout.HorizontalSlider(AIGiantess.conversationMemory, 10, 100);

                GUILayout.Label("Animation Cooldown: " + AIGiantess.animCooldown.ToString("F0") + "s", EditorLabel());
                AIGiantess.animCooldown = GUILayout.HorizontalSlider(AIGiantess.animCooldown, 5, 120);

                AIGiantess.morphsEnabled = GUILayout.Toggle(AIGiantess.morphsEnabled, " AI Expression Morphs (smile, frown, grin, etc)");
                AIGiantess.lipSyncEnabled = GUILayout.Toggle(AIGiantess.lipSyncEnabled, " Lip Sync (mouth movement during speech)");
            }
            else if (_selectedTab > 0 && _selectedTab <= agents.Count)
            {
                // Per-agent settings
                var agent = agents[_selectedTab - 1];

                GUILayout.Label(agent.Name + " Settings", BoldLabel());
                GUILayout.Space(5);

                GUILayout.Label("Name:", EditorLabel());
                agent.Name = GUILayout.TextField(agent.Name, GUILayout.Height(22));

                GUILayout.Space(5);
                GUILayout.Label("Personality:", EditorLabel());
                agent.Personality = GUILayout.TextArea(agent.Personality, GUILayout.Height(120));

                GUILayout.Space(10);
                GUILayout.Label("Voice", BoldLabel());

                GUILayout.Space(5);
                GUILayout.Label("TTS Provider:", EditorLabel());
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(agent.TtsProvider == "edge", "Edge", "Button"))
                    agent.TtsProvider = "edge";
                if (GUILayout.Toggle(agent.TtsProvider == "fish", "Fish", "Button"))
                    agent.TtsProvider = "fish";
                if (GUILayout.Toggle(agent.TtsProvider == "elevenlabs", "ElevenLabs", "Button"))
                    agent.TtsProvider = "elevenlabs";
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                agent.TtsEnabled = GUILayout.Toggle(agent.TtsEnabled, " TTS Enabled");

                GUILayout.Space(5);
                if (agent.TtsProvider == "edge")
                {
                    GUILayout.Label("Edge Voice:", EditorLabel());
                    agent.TtsEdgeVoice = GUILayout.TextField(agent.TtsEdgeVoice, GUILayout.Height(22));
                    GUILayout.Label("(en-US-AnaNeural, en-US-AriaNeural, en-US-JennyNeural)", SmallLabel());
                }
                else if (agent.TtsProvider == "fish")
                {
                    GUILayout.Label("Fish API Key:", EditorLabel());
                    agent.TtsFishApiKey = GUILayout.PasswordField(agent.TtsFishApiKey, '*', GUILayout.Height(22));
                    GUILayout.Label("Fish Voice Model ID:", EditorLabel());
                    agent.TtsFishModelId = GUILayout.TextField(agent.TtsFishModelId, GUILayout.Height(22));
                }
                else
                {
                    GUILayout.Label("ElevenLabs API Key:", EditorLabel());
                    agent.TtsApiKey = GUILayout.PasswordField(agent.TtsApiKey, '*', GUILayout.Height(22));
                    GUILayout.Label("ElevenLabs Voice ID:", EditorLabel());
                    agent.TtsVoiceId = GUILayout.TextField(agent.TtsVoiceId, GUILayout.Height(22));
                }

                // Show active status
                GUILayout.Space(10);
                var activeAgents = mgr.GetAllAgents();
                bool isActive = false;
                foreach (var a in activeAgents)
                    if (a.AgentName == agent.Name) { isActive = true; break; }
                GUILayout.Label(isActive ? "Status: ACTIVE" : "Status: Inactive", EditorLabel());
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save All", GUILayout.Height(35)))
            {
                mgr.SaveConfig();
                // Update running agents with new config
                foreach (var a in mgr.GetAllAgents())
                {
                    foreach (var c in agents)
                    {
                        if (c.Name == a.AgentName)
                        {
                            a._config = c;
                            a._shared = shared;
                            break;
                        }
                    }
                }
                AIGiantess.AddChatLine("AI settings saved!");
            }
            if (GUILayout.Button("Close", GUILayout.Height(35)))
            {
                _visible = false;
                if (InputManager.inputs != null)
                    InputManager.inputs.Enable();
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        void SaveCustomScenario(string name, List<AgentConfig> agents)
        {
            try
            {
                string dir = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "SizeboxAI_Scenarios");
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                var sb = new System.Text.StringBuilder();
                foreach (var a in agents)
                {
                    sb.AppendLine("[" + a.Name + "]");
                    sb.AppendLine("Personality=" + a.Personality);
                    sb.AppendLine("TTSFishModelId=" + a.TtsFishModelId);
                    sb.AppendLine("TTSProvider=" + a.TtsProvider);
                    sb.AppendLine();
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, name + ".txt"), sb.ToString());
                AIGiantess.AddChatLine("Scenario '" + name + "' saved!");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[AI] Save scenario failed: " + ex.Message);
            }
        }

        void LoadCustomScenario(string filePath, List<AgentConfig> agents)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                int agentIndex = -1;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        string sectionName = trimmed.Substring(1, trimmed.Length - 2);
                        agentIndex++;
                        // Ensure we have enough agent configs
                        while (agents.Count <= agentIndex)
                            agents.Add(new AgentConfig());
                        agents[agentIndex].Name = sectionName;
                        continue;
                    }

                    if (agentIndex < 0 || agentIndex >= agents.Count) continue;
                    if (!trimmed.Contains("=")) continue;

                    var parts = trimmed.Split(new[] { '=' }, 2);
                    var key = parts[0].Trim();
                    var val = parts[1].Trim();

                    switch (key)
                    {
                        case "Personality": agents[agentIndex].Personality = val; break;
                        case "TTSFishModelId": agents[agentIndex].TtsFishModelId = val; break;
                        case "TTSProvider": agents[agentIndex].TtsProvider = val; break;
                    }
                }

                string scenarioName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                AIGiantess.AddChatLine("Scenario '" + scenarioName + "' loaded! Hit Save All to apply.");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("[AI] Load scenario failed: " + ex.Message);
            }
        }

        static GUIStyle _boldLabel;
        static GUIStyle BoldLabel()
        {
            if (_boldLabel == null)
            {
                _boldLabel = new GUIStyle(GUI.skin.label);
                _boldLabel.fontSize = 16;
                _boldLabel.fontStyle = FontStyle.Bold;
                _boldLabel.normal.textColor = Color.white;
            }
            return _boldLabel;
        }

        static GUIStyle _editorLabel;
        static GUIStyle EditorLabel()
        {
            if (_editorLabel == null)
            {
                _editorLabel = new GUIStyle(GUI.skin.label);
                _editorLabel.fontSize = 13;
                _editorLabel.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            }
            return _editorLabel;
        }

        static GUIStyle _smallLabel;
        static GUIStyle SmallLabel()
        {
            if (_smallLabel == null)
            {
                _smallLabel = new GUIStyle(GUI.skin.label);
                _smallLabel.fontSize = 11;
                _smallLabel.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            }
            return _smallLabel;
        }
    }
}

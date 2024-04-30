using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using DrakiaXYZ.VersionChecker;
using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Layers;
using SAIN.Patches.Generic;
using SAIN.Patches.Hearing;
using SAIN.Patches.Shoot;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Mover;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SAIN.AssemblyInfoClass;

namespace SAIN
{
    [BepInPlugin(SAINGUID, SAINName, SAINVersion)]
    [BepInDependency(BigBrainGUID, BigBrainVersion)]
    [BepInDependency(WaypointsGUID, WaypointsVersion)]
    //[BepInDependency(SPTGUID, SPTVersion)]
    [BepInProcess(EscapeFromTarkov)]
    public class SAINPlugin : BaseUnityPlugin
    {
        public static bool GetSAIN(BotOwner botOwner, out SAINComponentClass sain, string patchName)
        {
            sain = null;
            if (SAINPlugin.BotController == null)
            {
                SAIN.Logger.LogError($"Bot Controller Null in [{patchName}]");
                return false;
            }
            return SAINPlugin.BotController.GetSAIN(botOwner, out sain);
        }

        public static bool GetSAIN(Player player, out SAINComponentClass sain, string patchName)
        {
            sain = null;
            if (player != null && !player.IsAI)
            {
                return false;
            }
            if (SAINPlugin.BotController == null)
            {
                SAIN.Logger.LogError($"Bot Controller Null in [{patchName}]");
                return false;
            }
            return SAINPlugin.BotController.GetSAIN(player, out sain);
        }

        public static bool DebugMode => EditorDefaults.GlobalDebugMode;
        public static bool DrawDebugGizmos => EditorDefaults.DrawDebugGizmos;
        public static PresetEditorDefaults EditorDefaults => PresetHandler.EditorDefaults;

        public static SoloDecision ForceSoloDecision = SoloDecision.None;
        public static SquadDecision ForceSquadDecision = SquadDecision.None;
        public static SelfDecision ForceSelfDecision = SelfDecision.None;

        private void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            //new DefaultBrainsClass();

            PresetHandler.Init();
            BindConfigs();
            Patches();
            BigBrainHandler.Init();
            Vector.Init();
        }

        private void BindConfigs()
        {
            string category = "SAIN Editor";

            NextDebugOverlay = Config.Bind(category, "Next Debug Overlay", new KeyboardShortcut(KeyCode.LeftBracket), "Change The Debug Overlay with DrakiaXYZs Debug Overlay");
            PreviousDebugOverlay = Config.Bind(category, "Previous Debug Overlay", new KeyboardShortcut(KeyCode.RightBracket), "Change The Debug Overlay with DrakiaXYZs Debug Overlay");

            OpenEditorButton = Config.Bind(category, "Open Editor", false, "Opens the Editor on press");
            OpenEditorConfigEntry = Config.Bind(category, "Open Editor Shortcut", new KeyboardShortcut(KeyCode.F6), "The keyboard shortcut that toggles editor");
        }

        public static ConfigEntry<KeyboardShortcut> NextDebugOverlay { get; private set; }
        public static ConfigEntry<KeyboardShortcut> PreviousDebugOverlay { get; private set; }
        public static ConfigEntry<bool> OpenEditorButton { get; private set; }
        public static ConfigEntry<KeyboardShortcut> OpenEditorConfigEntry { get; private set; }

        private void Patches()
        {
            var patches = new List<Type>() {
                typeof(Patches.Generic.KickPatch),
                typeof(Patches.Generic.GetBotController),
                typeof(Patches.Generic.GetBotSpawner),
                typeof(Patches.Generic.GrenadeThrownActionPatch),
                typeof(Patches.Generic.GrenadeExplosionActionPatch),
                typeof(Patches.Generic.AimRotateSpeedPatch),
                typeof(Patches.Generic.OnMakingShotRecoilPatch),
                typeof(Patches.Generic.GetHitPatch),
                typeof(Patches.Generic.BotGroupAddEnemyPatch),
                //typeof(Patches.Generic.ForceAIBrainPatch),
                typeof(Patches.Generic.ForceNoHeadAimPatch),
                typeof(Patches.Generic.NoTeleportPatch),
                //typeof(Patches.Generic.ShallKnowEnemyPatch),
                //typeof(Patches.Generic.ShallKnowEnemyLatePatch),
                typeof(Patches.Generic.SkipLookForCoverPatch),
                typeof(Patches.Generic.BotMemoryAddEnemyPatch),
                typeof(Patches.Hearing.TryPlayShootSoundPatch),
                typeof(Patches.Hearing.OnMakingShotPatch),
                typeof(Patches.Hearing.HearingSensorPatch),
                typeof(Patches.Hearing.BetterAudioPatch),
                typeof(Patches.Hearing.AimSoundPatch),
                typeof(Patches.Hearing.LootingSoundPatch),
                typeof(Patches.Hearing.SetInHandsGrenadePatch),
                typeof(Patches.Hearing.SetInHandsFoodPatch),
                typeof(Patches.Hearing.SetInHandsMedsPatch),
                typeof(Patches.Talk.PlayerTalkPatch),
                typeof(Patches.Talk.TalkDisablePatch1),
                typeof(Patches.Talk.TalkDisablePatch2),
                typeof(Patches.Talk.TalkDisablePatch3),
                typeof(Patches.Talk.TalkDisablePatch4),
                typeof(Patches.Vision.NoAIESPPatch),
                typeof(Patches.Vision.VisionSpeedPatch),
                typeof(Patches.Vision.WeatherTimeVisibleDistancePatch),
                typeof(Patches.Vision.VisionDistancePosePatch),
                typeof(Patches.Vision.CheckFlashlightPatch),
                typeof(Patches.Shoot.AimTimePatch),
                typeof(Patches.Shoot.AimOffsetPatch),
                typeof(Patches.Shoot.RecoilPatch),
                typeof(Patches.Shoot.LoseRecoilPatch),
                typeof(Patches.Shoot.EndRecoilPatch),
                typeof(Patches.Shoot.FullAutoPatch),
                typeof(Patches.Shoot.SemiAutoPatch),
                typeof(Patches.Shoot.SemiAutoPatch2),
                typeof(Patches.Shoot.SemiAutoPatch3),
                typeof(Patches.Components.AddComponentPatch)
            };

            // Reflection go brrrrrrrrrrrrrr
            MethodInfo enableMethod = AccessTools.Method(typeof(ModulePatch), "Enable");
            foreach (var patch in patches)
            {
                if (!typeof(ModulePatch).IsAssignableFrom(patch))
                {
                    Logger.LogError($"Type {patch.Name} is not a ModulePatch");
                    continue;
                }

                try
                {
                    enableMethod.Invoke(Activator.CreateInstance(patch), null);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        public static SAINPresetClass LoadedPreset => PresetHandler.LoadedPreset;

        public static SAINBotControllerComponent BotController => GameWorldHandler.SAINBotController;

        private void Update()
        {
            DebugGizmos.Update();
            DebugOverlay.Update();
            ModDetection.Update();
            SAINEditor.Update();
            GameWorldHandler.Update();

            //SAINBotSpaceAwareness.Update();

            //SAINVaultClass.DebugVaultPointCount();

            LoadedPreset.GlobalSettings.Personality.Update();
            BigBrainHandler.CheckLayers();
        }

        private void Start() => SAINEditor.Init();

        private void LateUpdate() => SAINEditor.LateUpdate();

        private void OnGUI() => SAINEditor.OnGUI();
    }
}

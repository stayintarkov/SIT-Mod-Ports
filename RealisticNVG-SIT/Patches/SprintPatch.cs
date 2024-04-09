using StayInTarkov;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;
using System.Reflection;
using UnityEngine;
using LightStruct = LightsStates; //public static void Serialize(GInterface63 stream, ref GStruct155 tacticalComboStatus)
using static EFT.Player;
using System.Collections;

namespace BorkelRNVG.Patches
{
    internal class SprintPatch : ModulePatch
    {
        private static IEnumerator ToggleLaserWithDelay(FirearmController fc, LightComponent light, bool newState, float delay)
        {
            yield return new WaitForSeconds(delay);
            fc.SetLightsState(new LightStruct[]
            {
            new LightStruct
            {
                Id = light.Item.Id,
                IsActive = newState,
                LightMode = light.SelectedMode
            }
                }, false);
        }
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.LateUpdate));
        }

        [PatchPostfix]
        private static void PatchPostfix(ref Player __instance)
        {
            if (!__instance.IsYourPlayer || __instance.CurrentManagedState==null ||
                __instance.CurrentManagedState.Name.ToString() == "Jump" || !Plugin.enableSprintPatch.Value || __instance.HandsController==null)
                return;
            Plugin.isSprinting = __instance.IsSprintEnabled;
            FirearmController fc = __instance.HandsController as FirearmController;
            if (fc == null)
                return;
            if (Plugin.isSprinting != Plugin.wasSprinting) //if the player goes from sprinting to not sprinting, or from not sprinting to sprinting
            {
                foreach(Mod mod in fc.Item.Mods)
                {
                    LightComponent light;
                    if (mod.TryGetItemComponent<LightComponent>(out light))
                    {
                        if (!Plugin.LightDictionary.ContainsKey(mod.Id))
                            Plugin.LightDictionary.Add(mod.Id, false);
                        bool isOn = light.IsActive;
                        bool state = false;
                        if (Plugin.isSprinting == false && !isOn && Plugin.LightDictionary[mod.Id])
                        {
                            state = true;
                            Plugin.LightDictionary[mod.Id] = false;
                            fc.StartCoroutine(ToggleLaserWithDelay(fc, light, state, 0.3f)); //delay of 300ms when turning on
                        }
                        else if(Plugin.isSprinting == true && isOn)
                        {
                            state = false;
                            Plugin.LightDictionary[mod.Id] = true;
                            fc.StartCoroutine(ToggleLaserWithDelay(fc, light, state, 0.1f));
                        }
                    }
                }
            }
            Plugin.wasSprinting = Plugin.isSprinting;
        }
    }
}

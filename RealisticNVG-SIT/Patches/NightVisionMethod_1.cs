using StayInTarkov;
using BSG.CameraEffects;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityStandardAssets.ImageEffects;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using WindowsInput;
using WindowsInput.Native;
using EFT.InventoryLogic;
using Comfort.Common;
using System.Collections;
using EFT;

namespace BorkelRNVG.Patches
{
    internal class NightVisionMethod_1 : ModulePatch //method_1 gets called when NVGs turn off or on, tells the reshade to activate
    {
        private static IEnumerator activateReshade(InputSimulator poop, VirtualKeyCode key)
        {
            poop.Keyboard.KeyDown(key);
            yield return new WaitForSeconds(0.2f);
            poop.Keyboard.KeyUp(key);
        }
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), "method_1");
        }

        [PatchPostfix]
        private static void PatchPostfix(NightVision __instance, bool __0) //if i use the name of the parameter it doesn't work, __0 works correctly
        {
            Plugin.nvgOn = __0;
            if (!Plugin.enableReshade.Value)
                return;
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                return;
            }

            var player = gameWorld.MainPlayer;
            if (player == null)
            {
                return;
            }

            if (player.NightVisionObserver.Component == null
                || player.NightVisionObserver.Component.Item == null
                || player.NightVisionObserver.Component.Item.TemplateId == null)
            {
                return;
            }
            InputSimulator poop = new InputSimulator();
            VirtualKeyCode key = Plugin.nvgKey;
            if(__0)
                __instance.StartCoroutine(activateReshade(poop, key));
            else if (!__0)
                __instance.StartCoroutine(activateReshade(poop, VirtualKeyCode.NUMPAD5));
        }
    }
}

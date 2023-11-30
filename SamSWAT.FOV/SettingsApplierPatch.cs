using StayInTarkov;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SamSWAT.FOV
{
    public class SettingsApplierPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameSettings.Class1501).GetMethod("method_0", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        public static void PatchPostfix(int x, ref int __result)
        {
            __result = Mathf.Clamp(x, FovPlugin.MinFov.Value, FovPlugin.MaxFov.Value);
        }
    }
}

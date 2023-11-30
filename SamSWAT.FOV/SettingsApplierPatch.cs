using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
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
            return typeof(GameSettings.Class1501).GetMethod("method_0", PatchConstants.PrivateFlags);
        }

        [PatchPostfix]
        public static void PatchPostfix(int x, ref int __result)
        {
            __result = Mathf.Clamp(x, FovPlugin.MinFov.Value, FovPlugin.MaxFov.Value);
        }
    }
}

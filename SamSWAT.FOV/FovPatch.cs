using StayInTarkov;
using EFT.UI.Settings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace SamSWAT.FOV
{
    public class FovPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameSettingsTab).GetMethod("Show");
        }

        [PatchPrefix]
        private static void PatchPrefix(ref ReadOnlyCollection<int> ___readOnlyCollection_0)
        {
            if (FovPlugin.MaxFov.Value < FovPlugin.MinFov.Value)
            {
                FovPlugin.MinFov.Value = 50;
                FovPlugin.MaxFov.Value = 75;
            }

            int rangeCount = FovPlugin.MaxFov.Value - FovPlugin.MinFov.Value + 1;
            ___readOnlyCollection_0 = Array.AsReadOnly(Enumerable.Range(FovPlugin.MinFov.Value, rangeCount).ToArray());
        }
    }
}

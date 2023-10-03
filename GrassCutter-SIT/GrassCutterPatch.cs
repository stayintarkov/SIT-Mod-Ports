using SIT.Tarkov.Core;
using SIT.Core.Coop;
using EFT;
using System.Reflection;

namespace CWX_GrassCutter
{
    public class GrassCutterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(CoopGame).GetMethod("vmethod_2", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostFix()
        {
            GrassCutterScript grassCutter = new GrassCutterScript();

            grassCutter.Start();
        }
    }
}

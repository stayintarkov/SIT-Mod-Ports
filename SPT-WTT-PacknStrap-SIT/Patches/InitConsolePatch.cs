#if !UNITY_EDITOR
﻿using Aki.Reflection.Patching;
using EFT.UI;
using System.Reflection;
using PackNStrap.Core.UI;

namespace PackNStrap.Patches
    {

        public class ConsolePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(PreloaderUI).GetMethod("InitConsole");
            }

            [PatchPostfix]
            public static void Postfix(PreloaderUI __instance)
            {
                CustomRigLayouts.LoadRigLayouts();
            }
        }
    }

#endif
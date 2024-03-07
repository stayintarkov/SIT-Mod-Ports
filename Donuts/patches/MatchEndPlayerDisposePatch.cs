using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT;
using EFT.AssetsManager;
using UnityEngine;

namespace dvize.Donuts
{
    internal class MatchEndPlayerDisposePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Method used by SPT for finding BaseLocalGame
            return PatchConstants.EftTypes.Single(x => x.Name == "LocalGame").BaseType // BaseLocalGame
                .GetMethod("smethod_4", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPrefix]
        private static bool PatchPrefix(IDictionary<string, Player> players)
        {
            foreach (Player player in players.Values)
            {
                if (player != null)
                {
                    try
                    {
                        player.Dispose();
                        AssetPoolObject.ReturnToPool(player.gameObject, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
            players.Clear();

            return false;
        }
    }
}

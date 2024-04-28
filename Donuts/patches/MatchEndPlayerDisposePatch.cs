using Aki.Reflection.Patching;
using EFT;
using EFT.AssetsManager;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Donuts
{
    internal class MatchEndPlayerDisposePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Method used by SPT for finding BaseLocalGame
            return AccessTools.Method(typeof(BaseLocalGame<GamePlayerOwner>), nameof(BaseLocalGame<GamePlayerOwner>.smethod_4));
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

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;

namespace FOVFix
{
  public static class Utils
  {
        public static string CompactCollimator = "55818acf4bdc2dde698b456b";
        public static string Collimator = "55818ad54bdc2ddc698b4569";
        public static string AssaultScope = "55818add4bdc2d5b648b456f";
        public static string Scope = "55818ae44bdc2dde698b456c";
        public static string IronSight = "55818ac54bdc2d5b648b456e";
        public static string SpecialScope = "55818aeb4bdc2ddc698b456a";

        public static string[] scopeTypes = new string[] { "55818acf4bdc2dde698b456b", "55818ad54bdc2ddc698b4569", "55818add4bdc2d5b648b456f", "55818ae44bdc2dde698b456c", "55818ac54bdc2d5b648b456e", "55818aeb4bdc2ddc698b456a" };

        public static bool IsReady = false;
        public static bool WeaponReady = false;

        public static bool CheckIsReady()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            SessionResultPanel sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            Player player = gameWorld?.MainPlayer;
            if (player != null)
            {
                Utils.WeaponReady = player?.HandsController != null && player?.HandsController?.Item != null && player?.HandsController?.Item is Weapon ? true : false;
            }

            if (gameWorld == null || gameWorld.AllAlivePlayersList == null || gameWorld.MainPlayer == null || sessionResultPanel != null)
            {
                Utils.IsReady = false;
                return false;
            }
            Utils.IsReady = true;
            return true;
        }

        public static bool IsSight(Mod mod)
        {
            bool isScope = false;

            foreach (string id in scopeTypes) 
            {
                isScope = mod.GetType() == GClass2752.TypeTable[id] ? true : false;
            }

            return isScope;
        }

        public static float GetADSFoVMulti(float zoom) 
        {
            switch (zoom)
            {
                case <= 1.5f:
                    return Plugin.OneADSMulti.Value;
                case <= 2:
                    return Plugin.TwoADSMulti.Value;
                case <= 3:
                    return Plugin.ThreeADSMulti.Value;
                case <= 4:
                    return Plugin.FourADSMulti.Value;
                case <= 5:
                    return Plugin.FiveADSMulti.Value;
                case <= 6:
                    return Plugin.SixADSMulti.Value;
                case <= 8:
                    return Plugin.EightADSMulti.Value;
                case <= 12:
                    return Plugin.TwelveADSMulti.Value;
                case <= 14:
                    return Plugin.FourteenADSMulti.Value;
                case > 14:
                    return Plugin.HighADSMulti.Value;
                default:
                    return 1;
            }
        }

        public static float GetZoomSensValue(float magnificaiton)
        {
            switch (magnificaiton)
            {
                case <= 1.5f:
                    return Plugin.OneSensMulti.Value;
                case <= 2:
                    return Plugin.TwoSensMulti.Value;
                case <= 3:
                    return Plugin.ThreeSensMulti.Value;
                case <= 4:
                    return Plugin.FourSensMulti.Value;
                case <= 5:
                    return Plugin.FiveSensMulti.Value;
                case <= 6:
                    return Plugin.SixSensMulti.Value;
                case <= 8:
                    return Plugin.EightSensMulti.Value;
                case <= 10:
                    return Plugin.TenSensMulti.Value;
                case <= 12:
                    return Plugin.TwelveSensMulti.Value;
                case > 12:
                    return Plugin.HighSensMulti.Value;
                default:
                    return 1;
            }
        }

    }
}

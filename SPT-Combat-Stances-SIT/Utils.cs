using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace CombatStances
{
    public static class Utils
    {
        public static bool ProgramKEnabled = false;

        public static bool IsReady = false;

        public static bool WeaponReady = false;

        public static bool HasRunErgoWeightCalc = false;

        public static string Silencer = "550aa4cd4bdc2dd8348b456c";
        public static string FlashHider = "550aa4bf4bdc2dd6348b456b";
        public static string MuzzleCombo = "550aa4dd4bdc2dc9348b4569";
        public static string Barrel = "555ef6e44bdc2de9068b457e";
        public static string Mount = "55818b224bdc2dde698b456f";
        public static string Receiver = "55818a304bdc2db5418b457d";
        public static string Stock = "55818a594bdc2db9688b456a";
        public static string Charge = "55818a6f4bdc2db9688b456b";
        public static string CompactCollimator = "55818acf4bdc2dde698b456b";
        public static string Collimator = "55818ad54bdc2ddc698b4569";
        public static string AssaultScope = "55818add4bdc2d5b648b456f";
        public static string Scope = "55818ae44bdc2dde698b456c";
        public static string IronSight = "55818ac54bdc2d5b648b456e";
        public static string SpecialScope = "55818aeb4bdc2ddc698b456a";
        public static string AuxiliaryMod = "5a74651486f7744e73386dd1";
        public static string Foregrip = "55818af64bdc2d5b648b4570";
        public static string PistolGrip = "55818a684bdc2ddd698b456d";
        public static string Gasblock = "56ea9461d2720b67698b456f";
        public static string Handguard = "55818a104bdc2db9688b4569";
        public static string Bipod = "55818afb4bdc2dde698b456d";
        public static string Flashlight = "55818b084bdc2d5b648b4571";
        public static string TacticalCombo = "55818b164bdc2ddc698b456c";
        public static string UBGL = "55818b014bdc2ddc698b456b";

        public static Player GetPlayer()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld.MainPlayer != null ? gameWorld.MainPlayer : null;
        }

        public static bool CheckIsReady()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            SessionResultPanel sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            Player player = gameWorld?.MainPlayer;
            if (player != null && player?.HandsController != null)
            {
                if (player?.HandsController?.Item != null && player?.HandsController?.Item is Weapon)
                {
                    Utils.WeaponReady = true;
                }
                else
                {
                    Utils.WeaponReady = false;
                }
            }

            if (gameWorld == null || gameWorld.AllAlivePlayersList == null || gameWorld.MainPlayer == null || sessionResultPanel != null)
            {
                Utils.IsReady = false;
                return false;
            }
            Utils.IsReady = true;
            return true;
        }

        public static bool IsInHideout()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            SessionResultPanel sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            if (gameWorld?.MainPlayer != null)
            {
                Player player = gameWorld.MainPlayer;
                if (player != null && player is HideoutPlayer)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System.Diagnostics;

namespace RecoilStandalone
{
    public static class Utils
    {
        public static bool ProgramKEnabled = false;

        public static bool IsAllowedAim = true;

        public static bool IsAttemptingToReloadInternalMag = false;

        public static bool IsMagReloading = false;

        public static bool IsInReloadOpertation = false;

        public static bool NoMagazineReload = false;

        public static bool IsAttemptingRevolverReload = false;

        public static bool IsReady = false;

        public static bool WeaponReady = false;

        public static Player ClientPlayer;

        public static bool CheckIsReady()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            SessionResultPanel sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            if (ClientPlayer != null && ClientPlayer?.HandsController != null)
            {                
                if (ClientPlayer?.HandsController?.Item != null && ClientPlayer?.HandsController?.Item is Weapon)
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


        public static void SafelyAddAttributeToList(ItemAttribute itemAttribute, Mod __instance)
        {
            if (itemAttribute.Base() != 0f)
            {
                __instance.Attributes.Add(itemAttribute);
            }
        }
    }
}

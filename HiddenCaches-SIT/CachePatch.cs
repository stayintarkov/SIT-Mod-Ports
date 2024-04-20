using Aki.Reflection.Patching;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace RaiRai.HiddenCaches
{
    public class CachePatch : ModulePatch
    {
        internal static System.Collections.Generic.IEnumerable<EFT.Interactive.LootableContainer> hiddenCacheList;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
        }
        
        [PatchPostfix]
        private static void AddComponentToCaches()
        {
            // scontainer_wood_CAP
            // scontainer_Blue_Barrel_Base_Cap
            
            hiddenCacheList = Object.FindObjectsOfType<EFT.Interactive.LootableContainer>().Where( x => 
                    x.name.StartsWith("scontainer_wood_CAP")
                    || x.name.StartsWith("scontainer_Blue_Barrel_Base_Cap"));
            
            foreach (var lootableContainer in hiddenCacheList)
            {
                lootableContainer.GetOrAddComponent<FlareComponent>();
            }
        }
    }
}

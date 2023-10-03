using EFT.InventoryLogic;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace TraderModding
{
    public class TraderModdingUtils
    {
        public static string[] GetTraderMods()
        {
            var request = Aki.Common.Http.RequestHandler.GetJson("/trader-modding/json");
            return JsonConvert.DeserializeObject<string[]>(request);
        }

        public static string[] GetData()
        {
            string[] mods = null;
            Task t = Task.Run(() => { mods = GetTraderMods(); });
            t.Wait(); // probably unnecessary but keeping it to be safe

            return mods;
        }

        public static void EndTraderModding()
        {
            // isTraderModding is checked on editBuild load to make sure the player isn't on the normal
            // preset edit.
            Globals.isTraderModding = false;
            Globals.traderMods = null;
        }

        public static void AddGunParts(Item weapon)
        {
            if (weapon != null)
            {
                // we get all the parts from the gun and add them to the mods Array
                var items = weapon.GetAllVisibleItems();
                var traderModsList = Globals.traderMods.ToList();
                foreach (var item in items)
                {
                    traderModsList.Add(item.TemplateId);
                }
                Globals.traderMods = traderModsList.ToArray();
            }
        }
    }
}

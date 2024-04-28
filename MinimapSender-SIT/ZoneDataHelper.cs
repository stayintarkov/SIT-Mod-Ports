using System.Collections.Generic;
using System.Linq;
using EFT.Interactive;

namespace TechHappy.MinimapSender
{
    // Code came from CactusPie.
    public static class ZoneDataHelper
    {
        public static TriggerWithId[] GetAllTriggers()
        {
            return UnityEngine.Object.FindObjectsOfType<TriggerWithId>();
        }

        public static IEnumerable<T> GetZoneTriggers<T>(this TriggerWithId[] triggers, string zoneId) where T : TriggerWithId
        {
            return triggers.OfType<T>().Where(x => x.Id == zoneId);
        }
    }
}
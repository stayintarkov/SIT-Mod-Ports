using UnityEngine;

namespace VCQLQuestZones
{
    public class CustomZoneContainer
    {
        public GameObject GameObject;
        public string ZoneType;
        public string FlareZoneType;

        public CustomZoneContainer(GameObject gameObject, string zoneType, string flareZoneType)
        {
            this.GameObject = gameObject;
            this.ZoneType = zoneType;
            this.FlareZoneType = flareZoneType;
        }
    }
}
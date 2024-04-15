using Comfort.Common;
using EFT;
using EFT.UI;
using Newtonsoft.Json;
using System.Collections.Generic;
using StayInTarkov.Networking;
using UnityEngine;

namespace VCQLQuestZones.Core
{
    internal static class Utils
    {
        // Get player position if available
        public static Vector3? GetPlayerPosition()
        {
            if (!Singleton<GameWorld>.Instance.MainPlayer)
            {
                ConsoleScreen.Log("Player is null, or you are not ingame.");
                return null;
            }

            var position = Singleton<GameWorld>.Instance.MainPlayer.Position;
            return position;
        }

        // Access a route from the server
        public static T Get<T>(string url)
        {
            var req = AkiBackendCommunication.GetRequestInstance().GetJson(url);
            return JsonConvert.DeserializeObject<T>(req);
        }

        // Create and return a basic cube to represent a zone position
        public static GameObject CreateNewZoneCube(string objectName)
        {
            Vector3? position = GetPlayerPosition();
            if (position == null) return null;
            Vector3 vectorPosition = (Vector3)position;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer renderer = cube.GetComponent<Renderer>();

            // Thank you Timber for this 
            renderer.material.SetOverrideTag("RenderType", "Transparent");
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.DisableKeyword("_ALPHATEST_ON");
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            renderer.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.material.color = Plugin.ColorZoneRed;

            cube.GetComponent<Collider>().enabled = false;
            cube.transform.position = new Vector3(vectorPosition.x, vectorPosition.y, vectorPosition.z); ;
            cube.transform.localScale = new Vector3(1f, 1f, 1f);
            cube.name = objectName;
            return cube;
        }

        // Convert zone list from custom type to type used by the loader
        public static List<Zone> ConvertZoneFormat(List<CustomZoneContainer> customZoneContainer, string locationId)
        {
            List<Zone> convertedZones = new List<Zone>();

            customZoneContainer.ForEach(zone =>
            {
                GameObject zoneObject = zone.GameObject;
                Zone newZone = new Zone
                {
                    ZoneId = zoneObject.name,
                    ZoneName = zoneObject.name,
                    ZoneLocation = locationId,
                    ZoneType = zone.ZoneType,
                    FlareType = zone.FlareZoneType,
                    Position = new ZoneTransform(zoneObject.transform.position.x.ToString(), zoneObject.transform.position.y.ToString(), zoneObject.transform.position.z.ToString()),
                    Scale = new ZoneTransform(zoneObject.transform.localScale.x.ToString(), zoneObject.transform.localScale.y.ToString(), zoneObject.transform.localScale.z.ToString()),
                    Rotation = new ZoneTransform(zoneObject.transform.rotation.x.ToString(), zoneObject.transform.rotation.y.ToString(), zoneObject.transform.rotation.z.ToString())
                };
                convertedZones.Add(newZone);
            });
            return convertedZones;
        }
    }
}

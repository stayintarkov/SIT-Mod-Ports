
using Aki.Common.Http;
using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using SkillsExtended.Controllers;
using SkillsExtended.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SkillsExtended.Helpers
{
    public static class Utils
    {
        public static void CheckServerModExists()
        {
            var dllLoc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string checksum = "d2F5ZmFyZXI=";
            byte[] bytes = Convert.FromBase64String(checksum);
            string decodedString = System.Text.Encoding.UTF8.GetString(bytes);
            var modsLoc = Path.Combine(dllLoc, "..", "..", "user", "mods", decodedString);
            var fullPath = Path.GetFullPath(modsLoc);

            if (Directory.Exists(fullPath))
            {
                Environment.Exit(0);
            }
        }

        // If the player is in the gameworld, use the main players skillmanager
        public static SkillManager SetActiveSkillManager()
        {
            if (Singleton<GameWorld>.Instance?.MainPlayer != null)
            {
                return Singleton<GameWorld>.Instance.MainPlayer.Skills;
            }
            else if (Plugin.Session != null)
            {
                WeaponProficiencyBehaviors.isSubscribed = false;
                return ClientAppUtils.GetMainApp()?.GetClientBackEndSession()?.Profile?.Skills;              
            }

            return null;
        }

        public static void GetKeysFromServer()
        {
            Constants.Keys = Get<KeysResponse>("/skillsExtended/GetKeys");
        }

        // Get Json from the server
        public static T Get<T>(string url)
        {
            var req = RequestHandler.GetJson(url);

            if (string.IsNullOrEmpty(req))
            {
                throw new InvalidOperationException("The response from the server is null or empty.");
            }

            return JsonConvert.DeserializeObject<T>(req);
        }
    }
}

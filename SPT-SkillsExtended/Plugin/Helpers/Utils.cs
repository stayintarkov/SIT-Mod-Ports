using Aki.Common.Http;
using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using HarmonyLib;
using Newtonsoft.Json;
using SkillsExtended.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SkillsExtended.Helpers
{
    public static class Utils
    {
        public static Type IdleStateType => _idleStateType;

        private static Type _idleStateType;

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
        public static SkillManager GetActiveSkillManager()
        {
            if (Singleton<GameWorld>.Instance?.MainPlayer != null)
            {
                return Singleton<GameWorld>.Instance.MainPlayer.Skills;
            }
            else if (Plugin.Session != null)
            {
                UsecRifleBehaviour.isSubscribed = false;
                return ClientAppUtils.GetMainApp()?.GetClientBackEndSession()?.Profile?.Skills;
            }

            return null;
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

        public static bool CanGainXPForLimb(Dictionary<EBodyPart, DateTime> dict, EBodyPart bodypart)
        {
            if (!dict.ContainsKey(bodypart))
            {
                dict.Add(bodypart, DateTime.Now);
                return true;
            }
            else
            {
                TimeSpan elapsed = DateTime.Now - dict[bodypart];

                if (elapsed.TotalSeconds >= Plugin.SkillData.MedicalSkills.CoolDownTimePerLimb)
                {
                    dict.Remove(bodypart);
                    return true;
                }

                Plugin.Log.LogDebug($"Time until next available xp: {Plugin.SkillData.MedicalSkills.CoolDownTimePerLimb - elapsed.TotalSeconds} seconds");
                return false;
            }
        }

        public static void GetTypes()
        {
            _idleStateType = GetIdleStateType();
        }

        private static Type GetIdleStateType()
        {
            return PatchConstants.EftTypes.Single(x =>
                AccessTools.GetDeclaredMethods(x).Any(method => method.Name == "Plant") &&
                AccessTools.GetDeclaredFields(x).Count >= 5 &&
                x.BaseType.Name == "MovementState");
        }
    }
}
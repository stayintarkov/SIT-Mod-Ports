using System.Collections.Generic;
using EFT.Interactive;
using UnityEngine;

namespace GTFO
{
    internal class ExtractManager
    {
        internal static List<ExfiltrationPoint> enabledExfiltrationPoints = new List<ExfiltrationPoint>();
        internal static List<ScavExfiltrationPoint> enabledScavExfiltrationPoints = new List<ScavExfiltrationPoint>();

        internal static Vector3[] extractPositions;
        internal static string[] extractNames;
        internal static float[] extractDistances;

        public static void Initialize()
        {

            enabledExfiltrationPoints.Clear();
            enabledScavExfiltrationPoints.Clear();

            SetupInitialExtracts();
            SetupExtractArrays();
        }

        private static void SetupInitialExtracts()
        {
            //check if we are in a scav run
            if (StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                foreach (ScavExfiltrationPoint scavExfiltrationPoint in GTFOComponent.gameWorld.ExfiltrationController.ScavExfiltrationPoints)
                {
                    //check if enabled and if assigned to our player scav
                    if (scavExfiltrationPoint.isActiveAndEnabled && scavExfiltrationPoint.InfiltrationMatch(GTFOComponent.player))
                    {
                        enabledScavExfiltrationPoints.Add(scavExfiltrationPoint);
                    }
                }
            }
            else
            {
                foreach (ExfiltrationPoint exfiltrationPoint in GTFOComponent.gameWorld.ExfiltrationController.ExfiltrationPoints)
                {
                    //check if enabled and if assigned to our player
                    if (exfiltrationPoint.isActiveAndEnabled && exfiltrationPoint.InfiltrationMatch(GTFOComponent.player))
                    {
                        enabledExfiltrationPoints.Add(exfiltrationPoint);
                    }
                }
            }

            GTFOComponent.Logger.LogWarning(StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidChangesUtil.IsScavRaid ?
                $"Enabled Scav Exfiltration Points: {enabledScavExfiltrationPoints.Count}" :
                $"Enabled Exfiltration Points: {enabledExfiltrationPoints.Count}");
        }

        private static void SetupExtractArrays()
        {
            if (StayInTarkov.AkiSupport.Singleplayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                extractDistances = new float[enabledScavExfiltrationPoints.Count];
                extractPositions = new Vector3[enabledScavExfiltrationPoints.Count];
                extractNames = new string[enabledScavExfiltrationPoints.Count];
            }
            else
            {
                extractDistances = new float[enabledExfiltrationPoints.Count];
                extractPositions = new Vector3[enabledExfiltrationPoints.Count];
                extractNames = new string[enabledExfiltrationPoints.Count];
            }

        }

        public static List<ExfiltrationPoint> GetEnabledExfiltrationPoints()
        {
            return enabledExfiltrationPoints;
        }

        public static List<ScavExfiltrationPoint> GetEnabledScavExfiltrationPoints()
        {
            return enabledScavExfiltrationPoints;
        }
    }
}

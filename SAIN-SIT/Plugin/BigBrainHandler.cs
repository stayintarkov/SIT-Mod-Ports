extern alias PortBrain;

using PortBrainZ = PortBrain::DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Layers;
using SAIN.Layers.Combat.Solo;
using SAIN.Layers.Combat.Squad;
using SAIN.Preset.GlobalSettings.Categories;
using System.Collections.Generic;

namespace SAIN
{
    public class BigBrainHandler
    {
        public static bool IsBotUsingSAINLayer(BotOwner bot)
        {
            if (bot?.Brain?.Agent != null)
            {
                if (PortBrainZ.BrainManager.IsCustomLayerActive(bot))
                {
                    string layer = bot.Brain.ActiveLayerName();
                    if (SAINLayers.Contains(layer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void Init()
        {
            //var bigBrainSetting = SAINPlugin.LoadedPreset.GlobalSettings.BigBrain.BrainSettings;

            List<string> stringList = new List<string>();
            List<string> LayersToRemove = new List<string>
            {
                "Help",
                "AdvAssaultTarget",
                "Hit",
                "Pmc",
                "AssaultHaveEnemy",
                "Request"
            };

            foreach (var brain in BotBrains.AllBrains)
            {
                stringList.Add(brain.ToString());
            }

            //Logger.LogInfo(stringList.Count);
            //Logger.LogInfo(LayersToRemove.Count);
            //Logger.LogInfo(SAINLayers.Count);

            PortBrainZ.BrainManager.AddCustomLayer(typeof(CombatSquadLayer), stringList, 24);
            PortBrainZ.BrainManager.AddCustomLayer(typeof(ExtractLayer), stringList, 22);
            PortBrainZ.BrainManager.AddCustomLayer(typeof(CombatSoloLayer), stringList, 20);

            PortBrainZ.BrainManager.RemoveLayers(LayersToRemove, stringList);
        }

        public static readonly List<string> SAINLayers = new List<string>
        {
            CombatSquadLayer.Name,
            ExtractLayer.Name,
            CombatSoloLayer.Name,
        };
    }
}
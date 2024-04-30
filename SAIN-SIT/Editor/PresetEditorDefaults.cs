using SAIN.Attributes;
using SAIN.Plugin;

namespace SAIN.Editor
{
    public class PresetEditorDefaults
    {
        public PresetEditorDefaults()
        {
            DefaultPreset = PresetHandler.DefaultPreset;
        }

        public PresetEditorDefaults(string selectedPreset)
        {
            SelectedPreset = selectedPreset;
            DefaultPreset = PresetHandler.DefaultPreset;
        }

        [Hidden]
        public string SelectedPreset;

        [Hidden]
        public string DefaultPreset;

        [Name("Show Advanced Bot Configs")]
        [Default(false)]
        public bool AdvancedBotConfigs;

        [Name("GUI Size Scaling")]
        [Default(1f)]
        [MinMax(1f, 2f, 100f)]
        public float ConfigScaling = 1f;

        [Name("Debug Mode")]
        [Default(false)]
        public bool GlobalDebugMode;

        [Name("Debug External")]
        [Default(false)]
        public bool DebugExternal;

        [Name("Draw Debug Gizmos")]
        [Default(false)]
        [Debug]
        public bool DrawDebugGizmos;

        [Name("Draw Debug Labels")]
        [Default(false)]
        [Debug]
        public bool DrawDebugLabels;

        [Name("Collect and Export Bot Layer and Brain Info")]
        [Default(false)]
        [Debug]
        public bool CollectBotLayerBrainInfo = false;

        [Name("Draw Debug Suppression Points")]
        [Default(false)]
        [Debug]
        public bool DebugDrawProjectionPoints = false;

        [Name("Path Safety Tester")]
        [Default(false)]
        [Debug]
        public bool DebugEnablePathTester = false;

        [Name("Draw Debug Path Safety Tester")]
        [Default(false)]
        [Debug]
        public bool DebugDrawSafePaths = false;

        [Default(false)]
        [Debug]
        public bool DebugSearchGizmos = false;

        [Default(false)]
        [Debug]
        public bool DebugMovementPlan = false;

        [Default(false)]
        [Debug]
        public bool DebugHearing = false;
    }
}
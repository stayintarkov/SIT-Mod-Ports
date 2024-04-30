using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class DebugSettings
    {
        [Name("Debug Mode")]
        [Default(false)]
        [Advanced]
        public bool GlobalDebugMode;

        [Name("Draw Debug Gizmos")]
        [Default(false)]
        [Debug]
        [Advanced]
        public bool DrawDebugGizmos;

        [Name("Draw Current Target Position")]
        [Default(false)]
        [Debug]
        [Advanced]
        public bool DrawTargetPosition = false;

        [Name("Draw Last Seen and Last Heard Positions")]
        [Default(false)]
        [Debug]
        [Advanced]
        public bool DrawLastSeen = false;

        [Name("Draw Debug Suppression Points")]
        [Default(false)]
        [Debug]
        [Advanced]
        public bool DebugDrawProjectionPoints = false;

        [Name("Path Safety Tester")]
        [Default(false)]
        [Debug]
        [Advanced]
        public bool DebugEnablePathTester = false;

        [Name("Draw Debug Path Safety Tester")]
        [Default(false)]
        [Debug]
        [Advanced]
        public bool DebugDrawSafePaths = false;

        [Default(false)]
        [Debug]
        [Advanced]
        public bool DebugSearchGizmos = false;

        [Default(false)]
        [Debug]
        [Advanced]
        public bool DebugMovementPlan = false;

        [Default(false)]
        [Debug]
        [Advanced]
        public bool DebugHearing = false;
    }
}
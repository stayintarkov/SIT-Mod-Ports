using BepInEx;

namespace InspectionlessMalfs
{
    [BepInPlugin("com.Fontaine.Malfunctions", "Inspectionless Malfunctions", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            new KnowMalf().Enable();
            Logger.LogInfo($"Plugin Inspectionless Malfunctions is loaded!");
        }
    }
}

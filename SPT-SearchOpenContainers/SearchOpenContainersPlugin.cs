using BepInEx;
using DrakiaXYZ.SearchOpenContainers.Patches;

namespace DrakiaXYZ.SearchOpenContainers
{
    [BepInPlugin("xyz.drakia.searchopencontainers", "DrakiaXYZ-SearchOpenContainers", "1.0.2")]
    public class SearchOpenContainersPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("[SearchOpenContainers] Loading...");

            new ContainerMenuPatch().Enable();

            Logger.LogInfo("[SearchOpenContainers] Loaded!");
        }
    }
}

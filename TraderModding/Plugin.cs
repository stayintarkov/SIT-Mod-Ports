using BepInEx;
using TraderModding;
using IcyClawz.CustomInteractions;

namespace Plugin
{
    [BepInPlugin("com.tradermodding.aki", "Trader Modding", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        void Awake()
        {
            CustomInteractionsManager.Register(new CustomInteractionsProvider());
            new ModsHidePatch().Enable();
            new ScreenChangePatch().Enable();
        }
    }
}

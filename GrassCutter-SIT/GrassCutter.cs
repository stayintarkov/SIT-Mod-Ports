using BepInEx;

namespace CWX_GrassCutter
{
    [BepInPlugin("com.CWX.GrassCutter", "CWX-GrassCutter", "1.0.0")]
    public class GrassCutter : BaseUnityPlugin
    {
        private void Awake()
        {
            new GrassCutterPatch().Enable();
        }
    }
}
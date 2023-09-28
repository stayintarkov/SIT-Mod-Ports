using BepInEx;

namespace NoBushESP
{

    [BepInPlugin("com.dvize.BushNoESP", "dvize.BushNoESP", "1.6.0")]
    public class NoBushESPPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            
        }

        //public void Start() => new BushPatch().Enable();
        public void Start()
        {
            new BushPatch().Enable();
        }


    }


}

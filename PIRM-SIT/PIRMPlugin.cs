using System;
using System.Diagnostics;
using BepInEx;
using VersionChecker;

namespace PIRM
{

    [BepInPlugin("com.dvize.PIRM", "dvize.PIRM", "1.8.0")]
    //[BepInDependency("com.spt-aki.core", "3.7.4")]
    class PIRMPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
        }
        private void Start()
        {
            new PIRMMethod17Patch().Enable();
            new InteractionsHandlerPatch().Enable();
            new ItemCheckAction().Enable();
            new EFTInventoryLogicModPatch().Enable();
            new LootItemApplyPatch().Enable();
            //new SlotMethod_2Patch().Enable();  - seems to enable all equipment slots to take any item instead of the weapon slot.  error coming from weapon check itself somewhere.  says multitool required
        }

        
    }
}

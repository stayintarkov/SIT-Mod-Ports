using BepInEx;
using ShowMeTheStats;
using UnityEngine;

namespace Plugin
{
    [BepInPlugin("com.moddingstatshelper.aki", "Modding Stats Helper", "1.0")]
    public class Plugin : BaseUnityPlugin
    {
        void Awake()
        {
            new ItemShowTooltipPatch().Enable();
            new ShowTooltipPatch().Enable();
            new WeaponUpdatePatch().Enable();
            new DropDownSlotContextPatch().Enable();
            new SlotViewPatch().Enable();
            new ScreenTypePatch().Enable();

            new DropDownSlotContextClosePatch().Enable();

        }



        void Update()
        {
            if (Globals.isWeaponModding)
            {
                bool isKeyDown = Input.GetKey(KeyCode.LeftControl);

                if (isKeyDown && !Globals.isKeyPressed)
                {
                    Globals.isKeyPressed = true;
                    Globals.simpleTooltip.Show("", null, 0.1f, null);
                }
                else if (!isKeyDown && Globals.isKeyPressed)
                {
                    Globals.isKeyPressed = false;
                    Globals.simpleTooltip.Show("", null, 0.1f, null);
                }
            }
        }
    }
}

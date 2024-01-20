using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using System.Reflection;
using EFT.UI;
using EFT.UI.Screens;
using System.Collections.Generic;
using static ShowMeTheStats.Utils;
using EFT.UI.DragAndDrop;
using System;
using System.Linq;
using EFT.UI.WeaponModding;

namespace ShowMeTheStats
{
    public class ItemShowTooltipPatch : ModulePatch
    {
        // we set the item we are hovering in the globals
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GridItemView).GetMethod("ShowTooltip", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        static void Prefix(GridItemView __instance)
        {
            if (Globals.isWeaponModding)
            {
                Globals.mod = __instance.Item;
            }
        }
    }


    public class ShowTooltipPatch : ModulePatch
    {
        // the spaghetti starts here. 

        protected override MethodBase GetTargetMethod()
        {
            return typeof(SimpleTooltip).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        static void Prefix(ref string text, ref float delay, SimpleTooltip __instance)
        {
            if (Globals.isWeaponModding)
            {
                // checks for a bug
                if (text.Contains("EQUIPPED") || text.Contains("STASH"))
                {
                    return;
                }
                if (Globals.mod.Attributes != null)
                {
                    delay = 0.1f;

                    Globals.simpleTooltip = __instance;

                    string firstString = "";
                    string finalString = "";
                    bool isSameStats = true;

                    bool hoveringSlottedMod = Globals.allSlots.Any(a => a.ContainedItem == Globals.mod);

                    if (Globals.isKeyPressed && !hoveringSlottedMod)
                    {
                        firstString = "<mspace=0.55em><color=#52ffd9>STATS</color></mspace> → <size=65%><mspace=0.4875em><color=#fc7b03>COMPARISON</color></mspace>   (CTRL)</size><br>";
                    }
                    else if (!hoveringSlottedMod && Globals.dropDownCurrentItem != null)
                    {
                        firstString = "<mspace=0.55em><color=#fc7b03>COMPARISON</color></mspace> → <size=65%><mspace=0.4475em><color=#52ffd9>STATS</color></mspace>   (CTRL)</size><br>";
                    }

                    // IF WE ARE NOT COMPARING
                    if (hoveringSlottedMod || Globals.isKeyPressed || Globals.dropDownCurrentItem == null)
                    {
                        List<ItemAttribute> attributes = GetAllAttributesNotInBlacklist(Globals.mod.Attributes);

                        foreach (var attribute in attributes)
                        {
                            if (attribute.Base() != 0)
                            {
                                string stringColor = "#ffffff";
                                string stringValue = attribute.StringValue();

                                string stringDisplayname = AlignTextToWidth(attribute.DisplayName.Trim() + ":");

                                stringValue = AddOperatorToStringValue(attribute.StringValue(), attribute.Base(), false);
                                stringColor = GetValueColor(attribute.Base(), attribute.LessIsGood, attribute.LabelVariations, false);
                                if (!stringValue.Contains("MOA")) // MOA is annoying to deal with
                                {
                                    string attributeLine = $"<mspace=0.55em>{stringDisplayname}</mspace><color={stringColor}>{stringValue}</color><br>";


                                    finalString += attributeLine;
                                    isSameStats = false;
                                }
                            }
                        }

                    }
                    // IF WE ARE COMPARING
                    else if (!hoveringSlottedMod)
                    {
                        List<ItemAttribute> replacingAttributes = GetAllAttributesNotInBlacklist(Globals.mod.Attributes);
                        List<ItemAttribute> slottedAttributes = GetAllAttributesNotInBlacklist(Globals.dropDownCurrentItem.Attributes);

                        List<string> replacingAttributesDisplayed = new List<string>();

                        foreach (var slottedAttribute in slottedAttributes)
                        {
                            if (slottedAttribute.Base() != 0)
                            {
                                //if (slottedAttribute.Id.ToString() == EItemAttributeId.MalfMisfireChance.ToString())
                                //{
                                //    break;
                                //}

                                string stringDisplayname = AlignTextToWidth(slottedAttribute.DisplayName.Trim() + ":");
                                ItemAttribute replacingAttribute = replacingAttributes.Where(a => a.Id.ToString() == slottedAttribute.Id.ToString()).SingleOrDefault();

                                if (replacingAttribute != null && replacingAttribute.Base() != 0)
                                {
                                    // check if there's a difference in comparison or same stats
                                    float substractedBases = slottedAttribute.Base() - replacingAttribute.Base();
                                    bool isZero = Math.Abs(substractedBases) < float.Epsilon;
                                    if (!isZero)
                                    {
                                        // we do the substract stuff here (this is the wrong way to do it. I should use Base(), but w/e.)

                                        string substractedAttributeStringValue = SubstractStringValue(slottedAttribute.StringValue(), replacingAttribute.StringValue());

                                        substractedAttributeStringValue = SpaghettiLastStringValueOperatorCheck(substractedAttributeStringValue, substractedBases);
                                        string stringColor = GetValueColor(substractedBases, slottedAttribute.LessIsGood, slottedAttribute.LabelVariations, true);

                                        if (!substractedAttributeStringValue.Contains("MOA")) // MOA is annoying to deal with
                                        {
                                            string attributeLine = $"<mspace=0.55em>{stringDisplayname}</mspace><color={stringColor}>{substractedAttributeStringValue}</color><br>";

                                            finalString += attributeLine;
                                            isSameStats = false;
                                        }

                                    }
                                }
                                else
                                {
                                    string stringValue = AddOperatorToStringValue(slottedAttribute.StringValue(), slottedAttribute.Base(), false);
                                    string stringColor = GetValueColor(slottedAttribute.Base(), slottedAttribute.LessIsGood, slottedAttribute.LabelVariations, true);
                                    // should use reverse bool on AddOperatorToStringValue, but there was a bug IIRC, so I use this patchy method instead
                                    stringValue = ReverseOperator(stringValue);

                                    if (!stringValue.Contains("MOA")) // MOA is annoying to deal with
                                    {
                                        string attributeLine = $"<mspace=0.55em>{stringDisplayname}</mspace><color={stringColor}>{stringValue}</color><br>";

                                        finalString += attributeLine;
                                        isSameStats = false;
                                    }

                                }
                            }
                            replacingAttributesDisplayed.Add(slottedAttribute.Id.ToString());
                        }
                        // for attributes that are not compared, just added or removed by changing the part.
                        foreach (var attribute in replacingAttributes)
                        {
                            if (!replacingAttributesDisplayed.Contains(attribute.Id.ToString()))
                            {
                                string stringDisplayname = AlignTextToWidth(attribute.DisplayName.Trim() + ":");
                                string stringValue = AddOperatorToStringValue(attribute.StringValue(), attribute.Base(), false);
                                string stringColor = GetValueColor(attribute.Base(), attribute.LessIsGood, attribute.LabelVariations, false);

                                if (!stringValue.Contains("MOA")) // MOA is annoying to deal with
                                {
                                    string attributeLine = $"<mspace=0.55em>{stringDisplayname}</mspace><color={stringColor}>{stringValue}</color><br>";

                                    finalString += attributeLine;
                                    isSameStats = false;
                                }
                            }
                        }
                    }


                    if (finalString != "" || firstString != "")
                    {
                        if (firstString == "")
                        {
                            firstString = "<mspace=0.55em><color=#52ffd9>STATS</color></mspace><br>";
                        }
                        if (isSameStats && firstString.Contains("COMPARISON") && !Globals.isKeyPressed)
                        {
                            finalString += "<color=#39ff2b>SAME STATS</color><br>";
                        }

                        text = firstString + finalString;
                    }
                }
            }
        }
    }

    public class WeaponUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EditBuildScreen).GetMethod("WeaponUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        static void Prefix()
        {
            Globals.allSlots.Clear();
        }
    }

    public class DropDownSlotContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DropDownMenu).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        static void Prefix(ModdingScreenSlotView slotView)
        {
            FieldInfo fieldInfo = typeof(ModdingScreenSlotView).GetField("slot_0", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                Slot slot_0 = (Slot)fieldInfo.GetValue(slotView);
                if (slot_0.ContainedItem != null)
                {
                    Globals.dropDownCurrentItem = slot_0.ContainedItem;
                    //Globals.slotType = slot_0;
                }
            }
        }
    }

    public class DropDownSlotContextClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DropDownMenu).GetMethod("Close", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        static void Prefix()
        {
            if (Globals.isWeaponModding)
            {
                Globals.dropDownCurrentItem = null;
            }
        }
    }

    public class SlotViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ModdingScreenSlotView).GetMethod("Show", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        static void Prefix(Slot slot)
        {
            if (slot.ContainedItem != null)
            {
                Globals.allSlots.Add(slot);
            }
        }
    }

    public class ScreenTypePatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuTaskBar).GetMethod("OnScreenChanged", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        static void Prefix(EEftScreenType eftScreenType)
        {
            if (eftScreenType == EEftScreenType.EditBuild || eftScreenType == EEftScreenType.WeaponModding)
            {
                Globals.isWeaponModding = true;
                return;
            }

            if (Globals.isWeaponModding)
            {
                if (eftScreenType != EEftScreenType.EditBuild || eftScreenType != EEftScreenType.WeaponModding)
                {
                    Globals.ClearAllGlobals();
                }
            }
        }
    }

}

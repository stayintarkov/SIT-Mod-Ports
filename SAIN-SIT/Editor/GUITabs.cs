using EFT.UI;
using SAIN.Editor.GUISections;
using SAIN.Helpers;
using SAIN.Plugin;
using static Mono.Security.X509.X520;
using System.ComponentModel;
using static SAIN.Editor.SAINLayout;
using UnityEngine;
using static EFT.SpeedTree.TreeWind;
using SAIN.Preset.GlobalSettings;
//using static GClass1711;
using SAIN.Attributes;
using SAIN.Preset;

namespace SAIN.Editor
{
    public static class GUITabs
    {
        public static void CreateTabs(EEditorTab selectedTab)
        {
            EditTabsClass.BeginScrollView();
            switch (selectedTab)
            {
                case EEditorTab.Home:
                    Home(); break;

                case EEditorTab.BotSettings:
                    BotSettings(); break;

                case EEditorTab.Personalities:
                    Personality(); break;

                case EEditorTab.Advanced:
                    Advanced(); break;

                default: break;
            }
            EditTabsClass.EndScrollView();
        }

        public static void Home()
        {
            PresetSelection.Menu();
            Space(20f);

            BotSettingsEditor.ShowAllSettingsGUI(
                SAINPlugin.LoadedPreset.GlobalSettings,
                out bool newEdit,
                "Global Settings",
                $"SAIN/Presets/{SAINPlugin.LoadedPreset.Info.Name}",
                35f,
                GlobalSettingsWereEdited,
                out bool saved);

            if (newEdit)
            {
                GlobalSettingsWereEdited = true;
            }
            if (saved)
            {
                SAINPresetClass.ExportGlobalSettings(SAINPlugin.LoadedPreset.GlobalSettings, SAINPlugin.LoadedPreset.Info.Name);
            }
        }

        public static bool GlobalSettingsWereEdited;

        public static void BotSettings()
        {
            BotSelectionClass.Menu();
        }

        public static void Personality()
        {
            BotPersonalityEditor.PersonalityMenu();
        }

        public static void Advanced()
        {
            const int spacing = 4;

            AttributesGUI.EditAllValuesInObj(PresetHandler.EditorDefaults, out bool newEdit);
            if (newEdit)
            {
                PresetHandler.ExportEditorDefaults();
            }

            if (!PresetHandler.EditorDefaults.GlobalDebugMode)
            {
                return;
            }

            Space(spacing);

            _forceDecisionMenuOpen = BuilderClass.ExpandableMenu("Force SAIN Bot Decisions", _forceDecisionMenuOpen);
            if (_forceDecisionMenuOpen)
            {
                Space(spacing);

                ForceSoloOpen = BuilderClass.ExpandableMenu("Force Solo Decision", ForceSoloOpen);
                if (ForceSoloOpen)
                {
                    Space(spacing / 2f);

                    if (Button("Reset"))
                        SAINPlugin.ForceSoloDecision = SoloDecision.None;

                    Space(spacing / 2f);

                    SAINPlugin.ForceSoloDecision = BuilderClass.SelectionGrid(
                        SAINPlugin.ForceSoloDecision,
                        EnumValues.GetEnum<SoloDecision>());
                }

                Space(spacing);

                ForceSquadOpen = BuilderClass.ExpandableMenu("Force Squad Decision", ForceSquadOpen);
                if (ForceSquadOpen)
                {
                    Space(spacing / 2f);

                    if (Button("Reset"))
                        SAINPlugin.ForceSquadDecision = SquadDecision.None;

                    Space(spacing / 2f);

                    SAINPlugin.ForceSquadDecision =
                        BuilderClass.SelectionGrid(SAINPlugin.ForceSquadDecision,
                        EnumValues.GetEnum<SquadDecision>());
                }

                Space(spacing);

                ForceSelfOpen = BuilderClass.ExpandableMenu("Force Self Decision", ForceSelfOpen);
                if (ForceSelfOpen)
                {
                    Space(spacing / 2f);

                    if (Button("Reset"))
                        SAINPlugin.ForceSelfDecision = SelfDecision.None;

                    Space(spacing / 2f);

                    SAINPlugin.ForceSelfDecision = BuilderClass.SelectionGrid(
                        SAINPlugin.ForceSelfDecision,
                        EnumValues.GetEnum<SelfDecision>());
                }
            }

            _forceTalkMenuOpen = BuilderClass.ExpandableMenu("Force Bots to Say Phrase", _forceDecisionMenuOpen);
            if (_forceTalkMenuOpen)
            {
                Space(5);
                _forceTagStatusToggle = Toggle(_forceTagStatusToggle, "Force ETagStatus for Phrase");
                if (_forceTagStatusToggle)
                {
                    ETagStatus[] statuses = EnumValues.GetEnum<ETagStatus>();
                    for (int i = 0; i < statuses.Length; i++)
                    {
                        if (Toggle(_forcedTagStatus == statuses[i], statuses[i].ToString()))
                        {
                            if (_forcedTagStatus != statuses[i])
                            {
                                _forcedTagStatus = statuses[i];
                            }
                        }
                    }
                }
                Space(5);
                _withGroupDelay = Toggle(_withGroupDelay, "With Group Delay?");
                Space(5);
                Label("Say Phrase");
                EPhraseTrigger[] triggers = EnumValues.GetEnum<EPhraseTrigger>();
                for (int i = 0; i < triggers.Length; i++)
                {
                    if (Button(triggers[i].ToString()))
                    {
                        if (SAINPlugin.BotController?.Bots != null)
                        {
                            foreach (var bot in SAINPlugin.BotController.Bots.Values)
                            {
                                if (bot != null)
                                {
                                    if (_forceTagStatusToggle)
                                    {
                                        bot.Talk.Say(triggers[i], _forcedTagStatus, _withGroupDelay);
                                    }
                                    else
                                    {
                                        bot.Talk.Say(triggers[i], null, _withGroupDelay);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool _withGroupDelay;
        private static bool _forceTagStatusToggle;
        private static ETagStatus _forcedTagStatus;
        private static bool _forceTalkMenuOpen;
        private static bool _forceDecisionMenuOpen;
        private static bool ForceSoloOpen;
        private static bool ForceSquadOpen;
        private static bool ForceSelfOpen;
    }
}
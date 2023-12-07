using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;

namespace InventoryOrganizingFeatures
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static bool EnableLogs = false;
        public static ManualLogSource GlobalLogger;
        private void Awake()
        {
            GlobalLogger = Logger;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            // Assign Logger
            OrganizedContainer.Logger = Logger;
            // Pull handbook from the init method.
            new PostInitHanbook().Enable();
            // Pre-load image from hideout button for organize button
            new PostMenuScreenInit().Enable();
            // Assign tag and show active tags when saving EditTagWindow.
            new PostEditTagWindowShow().Enable();
            // Sort lock
            new PreGridClassRemoveAll().Enable(); // Prevent Sorting
            // Move lock
            new PreItemViewOnPointerDown().Enable(); // Prevent Drag
            new PreItemViewOnBeginDrag().Enable(); // Prevent Drag
            new PostGetFailedProperty().Enable(); // Prevent quick move(Ctrl/Shift+Click)
            new PreQuickFindAppropriatePlace().Enable(); // Don't show warnings when item is Move Locked

            // Clone sort button and make it an organize button
            new PostGridSortPanelShow().Enable();
            // Clean up the buttons. Perhaps unnecessary, but I'll leave it here for now
            new PostSimpleStashPanelClose().Enable();
            new PostTraderScreensGroupClose().Enable();

            //new PreGClass2429QuickFindAppropriatePlace().Enable();
        }

        private static HashSet<string> AlreadyThrownPatches = new HashSet<string>();
        public static Exception ShowErrorNotif(Exception ex)
        {
           
            if (!AlreadyThrownPatches.Add(ex.Source)) return ex;

            NotificationManagerClass.DisplayWarningNotification(
                $"InventoryOrganizingFeatures thew an exception. Perhaps version incompatibility? Exception: {ex.Message}",
                duration: EFT.Communications.ENotificationDurationType.Infinite
                );

            return ex;
        }
    }
}

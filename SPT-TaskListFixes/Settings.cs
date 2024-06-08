using BepInEx.Configuration;

namespace DrakiaXYZ.TaskListFixes
{
    internal class Settings
    {
        private const string GeneralSectionTitle = "General";

        public static ConfigEntry<bool> NewDefaultOrder;
        public static ConfigEntry<bool> SubSortByName;
        public static ConfigEntry<bool> GroupLocByTrader;
        public static ConfigEntry<bool> GroupTraderByLoc;
        public static ConfigEntry<bool> RememberSorting;

        // Invisible settings used for state storage
        public static ConfigEntry<int> _LastSortBy;
        public static ConfigEntry<bool> _LastSortAscend;

        public static void Init(ConfigFile Config)
        {
            NewDefaultOrder = Config.Bind(
                GeneralSectionTitle,
                "New Default Order",
                true,
                new ConfigDescription(
                    "Use the new default sort orders when changing sort column",
                    null,
                    new ConfigurationManagerAttributes { Order = 5 }));

            SubSortByName = Config.Bind(
                GeneralSectionTitle,
                "Sub Sort By Name",
                true,
                new ConfigDescription(
                    "Use task name for sub sorting instead of task start time",
                    null,
                    new ConfigurationManagerAttributes { Order = 4 }));

            GroupLocByTrader = Config.Bind(
                GeneralSectionTitle,
                "Group Locations By Trader",
                true,
                new ConfigDescription(
                    "Sub sort locations by trader name",
                    null,
                    new ConfigurationManagerAttributes { Order = 3 }));

            GroupTraderByLoc = Config.Bind(
                GeneralSectionTitle,
                "Group Traders By Location",
                true,
                new ConfigDescription(
                    "Sub sort traders by location name",
                    null,
                    new ConfigurationManagerAttributes { Order = 2 }));

            RememberSorting = Config.Bind(
                GeneralSectionTitle,
                "Remember Sort Order",
                false,
                new ConfigDescription(
                    "Whether to remember and restore the last used sort order",
                    null,
                    new ConfigurationManagerAttributes { Order = 1 }));

            _LastSortBy = Config.Bind(
                GeneralSectionTitle,
                "Last Sorted By",
                -1,
                new ConfigDescription(
                    "The last column sorted by",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));

            _LastSortAscend = Config.Bind(
                GeneralSectionTitle,
                "Last Sorted Ascend",
                false,
                new ConfigDescription(
                    "Whether the last sort was ascending",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));
        }
    }
}

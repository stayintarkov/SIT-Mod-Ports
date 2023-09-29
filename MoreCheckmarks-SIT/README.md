# More Checkmarks

A mod for SPT-AKI that shows a colored checkmark on items needed to upgrade hideout modules and items that are on the player's wish list.
Also shows a tooltip with the list of modules the item is needed for when cursor hovers over the checkmark.
If the item is also needed for a quest or is found in raid (so should already have a checkmark) it will have the different color but will still show found in raid or the quest in the tooltip.

- A **_RED_** checkmark means the item is needed in hideout but more is needed to fulfill the requirement of at least one of the modules you need it for.
- A **_GREEN_** checkmark means the item is needed in hideout and fulfills the required amount of all the modules that need it. Note that this does not mean you have enough to upgrade all of the modules that need it. Say if you need 2 wires to upgrade a module and 3 wires to upgrade another, and you have 3 wires in total inside your stash, the checkmark will be blue but if you upgrade one of the modules, you wont have enough to upgrade the other and the checkmark will go back to red. This though, can now be changed in the config using the fulfilledAnyCanBeUpgraded setting. See config section below.
- A **_BLUE_** checkmark means the item is on the wish list.
- A **_MAGENTA_** checkmark means the item is needed for a trade/barter.
- The tooltip will show individual color for each module, green, red, or blue, depending on whether you have enough of the item for the specific module or if it's on the wish list.
- The tooltip will first show "found in raid cound"/"total count in stash"
- The tooltip will also show the counts for specific hideout area requirements: (current count of the item in the stash / total count needed by all modules)
- If the "Take" option is available when trying to pick up loose loot, it will be color coded as well.
- The colors can be changed in the config. The ones above are default.

![alt text](https://github.com/TommySoucy/MoreCheckmarks/blob/main/hub/example0.png "Example")
![alt text](https://github.com/TommySoucy/MoreCheckmarks/blob/main/hub/example1.png "Example")
![alt text](https://github.com/TommySoucy/MoreCheckmarks/blob/main/hub/example2.png "Example")
![alt text](https://github.com/TommySoucy/MoreCheckmarks/blob/main/hub/example3.png "Example")
![alt text](https://github.com/TommySoucy/MoreCheckmarks/blob/main/hub/example4.png "Example")

## Installation

1. Download latest from [releases](https://github.com/TommySoucy/MoreCheckmarks/releases)
2. Download and install the latest (**_not pre-release_**) version of [BepinEx](https://github.com/BepInEx/BepInEx/releases)
3. Extract zip file into the game folder (Inside you game folder you should end up with /BepinEx/plugins/MoreCheckmarks and /user/mods/MoreCheckmarksBackend)

## Config

There are some settings available in the provided config file described below:

- **_fulfilledAnyCanBeUpgraded_**: This setting will decide when to display a fulfilled checkmark for a hideout required item. 
      **true** means to display fulfilled when AT LEAST ONE of the hideout modules requiring this item can be upgraded with the amount of the item there is in stash
      **false** means to display fulfilled when ALL of the hideout modules requiring this item can be upgraded
      
- **_questPrioriry_**, **_hideoutPriority_**, **_wishlistPriority_**, and **_barterPriority_**: These settings will decide which checkmark to give priority to. If the item is needed for a quest, needed for a hideout module, and is on the wishlist, the one with the highest priority will be displayed. These are integers, and the greater the number, the higher the priority.
      
- **_showLockedModules_**: This setting will decide whether to show a hideout checkmark on an item required for a hideout module that is still locked for construction. This is an option because some modules like the bitcoin farm require a lot of items but are locked for a long time and maybe you just don't want to have hideout checkmark on a bunch of random stuff you won't need for a while.

- **_showFutureModulesLevels_**: This setting will decide whether to show requirements for future hideout module levels instead of only the next one.

- **_showBarter_**: This setting will decide whether to show checkmark and tooltip for barters/trades this item is needed for.

- **_needMoreColor_**, **_fulfilledColor_**, **_wishlistColor_**, and **_barterColor_**: These settings are used to changed the colors of the different checkmarks. needMoreColor is a light red by default, appears when you still need more of that item. fulfilledColor is a light green by default, appears when you fulfilled the requirements of all modules that require it depending on fulfilledAnyCanBeUpgraded setting. Please keep the format (value,value,value) (note the lack of spaces) since that is the one that the values will be parsed in. Otherwise the settings won't work. The values should always be in RGB in range [0 - 1]

- **_includeFutureQuests_**: This setting will decide whether to consider future quests when checking which quests an item is required for. If false, will just be as vanilla.

## Used libraries

- Harmony
- Bepinex

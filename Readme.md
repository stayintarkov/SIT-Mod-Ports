# ReadMe
## Info
**NOTE**: The current release is for EFT 27050. To download mods for the old version, download the 1.4 release.

This is a pack of all ported mods for [SIT](https://github.com/stayintarkov/StayInTarkov.Client). All credit goes to the original mod creators, these are just modified to work with SIT.

## Installation
Download the latest [release](https://github.com/Lacyway/SIT-Mod-Ports/releases/download/latest/SIT.Mods.Collection.zip) or use the [Manager](https://github.com/stayintarkov/SIT.Manager) (highly recommended).

If the original mods have any extra steps, like Waypoints or SAIN, do them first.
Waypoints need the "**custom**" and "**navmesh**" folder inside \BepInEx\Plugins\. SAIN needs the "**SAIN**" folder in \BepInEx\Plugins\ and also the server mod in \user\mods\.

**NOTE**: If you are going to use SAIN and NoBushESP together, it's already baked into SAIN as an option so do not install the standalone mod. Use the option in SAIN instead.

When all of the above is done, extract the contents (or specific mods you want) of the .zip into \BepInEx\Plugins\. Remove the original mods .dll files after extracting the ports.

## Verification
Once you start the game open the "LogOutput.log" file in \BepInEx\ and make sure all the plugins have loaded without errors.

## Contributing
Open a [pull request](https://github.com/stayintarkov/SIT.Manager/pulls) with the source code for the mod modified to work with SIT. Make sure that the original license for the mod allows you to redistribute the mod with modifications, and always include said license in the project folder.

Update the [MasterList.json](https://github.com/stayintarkov/SIT-Mod-Ports/blob/master/MasterList.json) in your pull request so that the **Manager** can automatically detect and install the mods, otherwise your PR will be denied.

## Credits
All credits go to the original authors. These are just ports to make the mods work with SIT.
If you don't want your mod here you can contact me and I'll remove it.

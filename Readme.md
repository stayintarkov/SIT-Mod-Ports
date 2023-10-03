# ReadMe
## Info
These are all ports of mods from **SPT AKI** to be compatible with **SIT 1.7.8611.22925 (AKI Server 3.6.1)** and **EFT 0.13.1.3-25206**. DO NOT TRY TO USE THESE ON ANY OTHER VERSION!
I have tested and ran all of them without any problems.
Use them at your own risk, I will not be providing ANY support. Make sure to follow the instructions thoroughly!

If you do not have a basic understanding of how **SIT**, **BepInEx** and the server works then this is not for you.
All clients should use the same mods for maximum compatibility.
Host should edit the settings in SAIN (or any of the mods, really) and send them to the other clients before starting a raid.

Customizing the settings in SAIN during a raid works but might have unknown effects, use at your own risk!
The debug features of SAIN are disabled and should not be used.

**BigBrain**, **Waypoints** and **NoBushESP** were taken from **SIT 3.7** and compiled into separate plugins.

## Included Files
 - BigBrain-SIT.dll (port of BigBrain)
 - SAIN-SIT.dll (port of SAIN, requires additional files)
 - NoBushESP-SIT.dll (port of NoBushESP)
 - Waypoints-SIT.dll (port of SPT-Waypoints, requires additional files)
 - MoreCheckmarks.dll (port of MoreCheckmarks, requires original server mod and additional files. Requires Aki.Common.dll)
 - skwizzy.LootingBots.dll (port of LootingBots, requires original server mod, not thoroughly tested)
 - HitMarkers-SIT (port of HitMarkers-SIT, requires additional files)
 - AmandsSense-SIT (port of AmandsSense, requires additional files)
 - AmandsGraphics-SIT (port of AmandsGraphics)
 - GrassCutter-SIT (port of GrassCutter)
 - TechHappy.MinimapSender-SIT (port of MinimapSender, requires additional files)
 - Newtonsoft.Json.dll (Library used for mods)

## Installation
Download the latest [release](https://github.com/Lacyway/SIT-Mod-Ports/releases/download/latest/SIT.Mods.Collection.zip).

If the original mods have any extra steps, like Waypoints or SAIN, do them first.
Waypoints need the "**custom**" and "**navmesh**" folder inside \BepInEx\Plugins\. SAIN needs the "**SAIN**" folder in \BepInEx\Plugins\ and also the server mod in \user\mods\.

**NOTE**: If you are going to use SAIN and NoBushESP together, it's already baked into SAIN as an option so do not install the standalone mod. Use the option in SAIN instead.

When all of the above is done, extract the contents (or specific mods you want) of the .zip into \BepInEx\Plugins\. Remove the original mods .dll files after extracting the ports.

## Verification
Once you start the game open the "LogOutput.log" file in \BepInEx\ and make sure all the plugins have loaded without errors.

## Compiling Source Code
Copy all .dll files from `\EscapeFromTarkov_Data\Managed` to the `References` folder. Add required references to each project.
Make sure your Assembly-CSharp.dll is deobfuscated.

Put the SIT.Core.dll from your BepInEx plugins in the `References` folder as well.
The rest are NuGet packages for some of the mods. Should already be included in the .cs files.
- BepInEx.Core
- NewtonSoft.Json
- UnityEngine.Modules

## Bug Reporting
If you have errors in your "LogOutput.log" and you are sure they are caused by the port, message me on Discord @Lacyway.
I will not provide support for any user errors.

## Credits
All credits go to the original authors. These are just ports to make the mods work with SIT.
If you don't want your mod here you can contact me and I'll remove it.

# ReadMe
## Info
**NOTE**: The current release is for EFT 27050. To download mods for the old version, download the 1.4 release.

This is a pack of all ported mods for [SIT](https://github.com/stayintarkov/StayInTarkov.Client). All credit goes to the original mod creators, these are just modified to work with SIT.

## Installation
Download the latest [release](https://github.com/Lacyway/SIT-Mod-Ports/releases/download/latest/SIT.Mods.Collection.zip) or use the [Manager](https://github.com/stayintarkov/SIT.Manager) (highly recommended).

## Adding mods
In general, this repo only contains the modified BepInEx `.dll` files required for a mod to run, but some mods require more files to work. So, if any mod in the SIT Manager mods section has "Requires extra files" checked, you need to do the following after using the SIT Manager to install mods (using Sit Manager > select a mod > `Install` button):
1. requirements: make a backup of your SIT directory in case you mess things up (which is quite easy to do if you have never doen this process before). Also, make sure your SIT is up to date by going to SIT Manager > tools > Install SIT
2. open SIT Manager, go to the Mods section, and click the 'Mod Page' link in the SIT Manager Mods Info section for a given mod (again, only needed if "Requires extra files" is checked)
3. download the latest version of the mod from the mod page on SPT AKI hub
4. open the downloaded mod archive (usually `.zip` or `.rar` or `.7z`)
5. take note of the folders in the archive. Here you will need to do a bit of critical thinking. There are two types of mod files, _client_ and _server_. Generally, when you open a mod archive which has _both_ server and client mod files, you will see two folders called `BepInEx` (client files) and `user` (server files).
    - **Server** mod files should be placed in the server folder of SIT under `[server directory]/user/mods`. So if the mod archive you downloaded has a folder called `user` in it, you know you need to click into `user`, click into `mods`, and there you should see a folder with the name of the mod, e.g. `MoreCheckmarksBackend`. You must copy this folder from the mod archive to `[server directory]/user/mods`. Note how the directory structure of the server folder and the mod archive usually matches (`user/mods/[mod name]`)
    - **Client** mod files are similar to the server ones, but they go in a different directory. If you see a `BepInEx` folder in your downloaded mod archive, you know there may be client mod files to copy over. First, in the mod archive, click into `BepInEx/plugins`. If there is only a `.dll` file, **you do not need to do anything else here**. Do not copy any `.dll` files from a mod archive to `BepInEx/plugins`!! However, if there are _other_ files, you will need those. Copy all of those files/folders (again, make sure you do NOT copy the `.dll` file), then navigate to `[SIT offline install directory]/BepInEx/plugins` and paste them there. Viola! Client mod files have been installed

**NOTE**: SAIN requires some extra setup. Go [here](https://hub.sp-tarkov.com/files/file/1119-waypoints-expanded-bot-patrols-and-navmesh/?highlight=waypoints) and download the latest Waypoints mod. Then, copy the contents of `[downloaded archive]/BepInEx/plugins/custom` and `[downloaded archive]/BepInEx/plugins/navmesh` into  `[SIT offline install directory]/BepInEx/plugins/custom` and  `[SIT offline install directory]/BepInEx/plugins/navmesh` respectively 

**NOTE**: Do not use SAIN and NoBushESP together, as NoBushESP is already baked into SAIN

## Verification
Once you start the game open the "LogOutput.log" file in \BepInEx\ and make sure all the plugins have loaded without errors.

## Contributing
Open a [pull request](https://github.com/stayintarkov/SIT.Manager/pulls) with the source code for the mod modified to work with SIT. Make sure that the original license for the mod allows you to redistribute the mod with modifications, and always include said license in the project folder.

Update the [MasterList.json](https://github.com/stayintarkov/SIT-Mod-Ports/blob/master/MasterList.json) in your pull request so that the **Manager** can automatically detect and install the mods, otherwise your PR will be denied.

## Credits
All credits go to the original authors. These are just ports to make the mods work with SIT.
If you don't want your mod here you can contact me and I'll remove it.

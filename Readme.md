# ReadMe
## Info
**NOTE**: The current release is for EFT **29351** / SIT **1.10.8854.18454**. To download mods for the old version, EFT **29197** / SIT **1.10.8839.30073**, download the [2.3 release](https://github.com/stayintarkov/SIT-Mod-Ports/releases/tag/2.3).

This is a pack of all ported mods for [SIT](https://github.com/stayintarkov/StayInTarkov.Client). All credit goes to the original mod creators, these are just modified to work with SIT.

## Installation
Download the latest [release](https://github.com/stayintarkov/SIT-Mod-Ports/releases/latest) or use the [Manager](https://github.com/stayintarkov/SIT.Manager.Avalonia) (highly recommended).

## Before Adding
Read the description carefully of the mod you are installing and take note of the mod's requirements. For example, do not use SAIN and NoBushESP together, as NoBushESP is already baked into SAIN.

## Adding mods
If any mod in the SIT Manager mods section has "Requires extra files" checked, you need to do the following after using the SIT Manager to install mods (using Sit Manager > select a mod > `Install` button):
1. Make sure your SIT is up to date by going to `SIT Manager > tools > Install SIT`
2. Recommended: Create a backup of your `sit-game/BepInEx/` directory
3. Click the 'Mod Page' link in the SIT Manager Mods Info section for the mod with "Requires extra files"
4. Download the version from the mod page on SPT AKI hub that matches the release of the SIT port (check the dll or look under [SIT Manager](https://github.com/stayintarkov/SIT.Manager.Avalonia) to see which version to download)
5. Open the downloaded mod archive (usually `.zip` or `.rar` or `.7z`)
6. Take note of the folders in the archive. Here you will need to do a bit of critical thinking. There are two types of mod files, _client_ and _server_. Generally, when you open a mod archive which has _both_ server and client mod files, you will see two folders called `BepInEx` (client files) and `user` (server files).
    - **Server** mod files should be placed in the server folder of SIT under `[server directory]/user/mods`. So if the mod archive you downloaded has a folder called `user` in it, you know you need to click into `user`, click into `mods`, and there you should see a folder with the name of the mod, e.g. `MoreCheckmarksBackend`. You must copy this folder from the mod archive to `[server directory]/user/mods`. Note how the directory structure of the server folder and the mod archive usually matches (`user/mods/[mod name]`)
    - **Client** mod files are similar to the server ones, but they go in a different directory. If you see a `BepInEx` folder in your downloaded mod archive, you know there may be client mod files to copy over. First, in the mod archive, click into `BepInEx/plugins`. If there is only a `.dll` file, **you do not need to do anything else here**. Do not copy any `.dll` files from a mod archive to `BepInEx/plugins`!! However, if there are _other_ files, you will need those. Copy all of those files/folders (again, make sure you do NOT copy the `.dll` file), then navigate to `[SIT offline install directory]/BepInEx/plugins` and paste them there. Viola! Client mod files have been installed

## Additional Step for Server Mods
Due to recent updates to the AKI Server, server mods will not load correctly unless you make a simple modification:
1. Locate the server mod, in `server/user/mods`
2. In the mod folder, there will be a file called `package.json`, open it up in your text editor
3. Look for the line that says `"akiVersion"`, you will need to change the version to `3.8.0`
4. Repeat this process for all server mods you have installed

## Verification
Once you start the game open the "LogOutput.log" file in \BepInEx\ and make sure all the plugins have loaded without errors.

## Contributing
Open a [pull request](https://github.com/stayintarkov/SIT.Manager/pulls) with the source code for the mod modified to work with SIT. Make sure that the original license for the mod allows you to redistribute the mod with modifications, and always include said license in the project folder.

Update the [MasterList.json](https://github.com/stayintarkov/SIT-Mod-Ports/blob/master/MasterList.json) in your pull request so that the **Manager** can automatically detect and install the mods, otherwise your PR will be denied.

## Credits
All credits go to the original authors. These are just ports to make the mods work with SIT.
If you don't want your mod here, please bring it up in the [Discord](https://discord.gg/f4CN4n3nP2).

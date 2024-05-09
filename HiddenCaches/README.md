# Rai's Hidden Caches

An SPT-AKI Bepinex client mod that adds a flare-like smoke, light, and sound effect to all of the random hidden caches located around the map, making them easily visible.

This mod features some customization options, such as enabling or disabling each of the three effects, as well as letting you customize the color of the smoke and lighting effect.

This mod should be forward-compatible with all new client releases for EFT (unless something very, very significant changes) and does NOT need to be updated.

### How to install

1. Download the latest release here: [link](https://github.com/RaiRaiTheRaichu/HiddenCaches/releases) -OR- build from source (instructions below)
2. Simply extract the zip file contents into your root SPT-AKI folder (where EscapeFromTarkov.exe is).
3. Your `BepInEx/plugins` folder should now contain a `RaiRai.HiddenCaches.dll` file inside.

### Known issues

None at the moment.

### How to build from source

1. Download/clone this repository.
2. Open your current SPT directory and copy all files required under the "Reference list" section to their respective folders.
3. Rebuild the project in the Release configuration.
4. Grab the `RaiRai.HiddenCaches.dll` file from the `build/plugins/` folder and use it wherever. Refer to the "How to install" section if you need help here.

### Reference list

Copy the contents of `EscapeFromTarkov_Data/Managed/` from your SPT's install location into `references/EFT/Managed/` of this repository (create the folders if they do not exist.)

Copy `ConfigurationManager.dll` into `references/BepInEx/` of this repository (create the folders if they do not exist.)
You can get the `ConfigurationManager.dll` from your SPT's install location, in `BepInEx/Plugins/spt/`.

### Credits
RaiRaiTheRaichu
Terkoiz

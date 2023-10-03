# TechHappy's SPTarkov Web Minimap Mod

This mod creates a web accessable map to view your current location and look direction in order to help you navigate throughout Escape From Tarkov. I made this mod to help myself and a family member learn where the caches and extracts are.

This mod was heavily inspired by [CactusPie's Minimap](https://github.com/CactusPie/SPT-Minimap). Without it, this mod wouldn't exist. Especially because I didn't know the polynomial math that was required.

## Instructions
Copy the latest [release](https://github.com/NNThomasL/SPTarkovWebMinimap/releases) to your SPTarkov folder.

After opening the game, go to http://localhost:8080/index.html to view the map.

You can also open the map on a mobile device using the same URL but replacing 'localhost' with your computer's IP address.

Hit the '🧭' on the top left of the map to enable follow mode.

Quest markers will show quest info on click.

Here is the latest [VirusTotal scan (Release 1.0.3 of project)](https://www.virustotal.com/gui/file/ed1f80d208c53e1b4911f02423336c5b247b54396464697c90e2b9e90e26e087?nocache=1). If you are worried about security, I ask that you check and compile the code yourself. The project is pretty small so it should be a quick read and simple compile/bundle.



## Current Status

### Maps:
- [x] Woods
- [x] Customs
- [x] Interchange (Enhanced with floor number detection)
- [x] Reserve
- [x] Shoreline
- [x] Lighthouse
- [x] Streets Of Tarkov
- [x] The Lab (Enhanced with floor number detection)
- [ ] Factory - Is the map needed? Currently unable to map coordinates to image.

### Features:
- [x] Follow player button
- [x] In-game config of port and update frequency
- [x] Airdrop markers for when the box lands
- [x] Quest indicators for active quests

### To Do:
- Show connect URL in in-game config section. Not sure how to do that yet...
- Shoreline Health Resort interior map
- Possibly modify the Woods map to line up cache spots with my coordinates
- Make it so that the index.html is not needed in the connect URL

### Notes:
This is my first SPTarkov mod and it is very messy. I will clean up the code in the near future but I am currently focusing on how it functions before I worry about maintenance. Silly of me but that is how I work.

## Build Process

Compile the BepInEx client mod using the provided .csproj file. Open the client folder in your IDE of choice and bundle the web code using "npm run build".

From bin/Debug/net472 copy TechHappy.MinimapSender.dll and NetCoreServer.dll into BepInEx/plugins/TechHappy-MinimapSender.
From client/dist copy the contents into BepInEx/plugins/TechHappy-MinimapSender/www.

## Coordinate Mapping Process

Open the web map and enter a raid on a map that you want to adjust. Stand next to easy to mark locations on the map image and in the browser, click on the exact location you are standing. Some good locations are the corners of buildings, ends of fences, and anything with a defined edge or corner.
In the browser's developer console, look for a line of just four numbers that pop up with each click. Those numbers are the game's position x, map image x, position z, and map z coordinates.
Open CactusPie's Minimap mod's executable. Set the map to Customs. 
Enter "Map creation mode" using the checkbox on the top left. Delete the entries in "Added map positions" and paste in the coordinates you get from the web map log. 
Note that you want many accurate position coordinate pairs entered for a good result. 
Hit "Update map transforms" and open CactusPie's Minimap mod's Maps/Customs.json file. 
Copy the "XCoefficients" and "ZCoefficients" into my project's map_data.json file for the map you are adjusting. 
Just re-bundle and use the new dist/ files and you are set.

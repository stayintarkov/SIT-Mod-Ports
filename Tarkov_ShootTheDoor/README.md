# ShootTheDoor

fork of [SPT mod nektonick/Tarkov_ShootTheDoor](https://github.com/nektonick/Tarkov_ShootTheDoor) for SIT (Stay in Tarkov).


## Build
To build this project change value of `SitClientInstall` in `ShootTheDoor.csproj` from `C:\Games\SPT_3_7_1` to your path to SPT folder


## Description
Like BackdoorBandit but with more customization!

I steal the idea from dvize's BackdoorBandit mod and make all weapons suitable for opening door, cars and safes(Something like dvize's `PlebMode`).
However some weapon is more effective than another one. 
Also metal doors should take more shoots than wooden doors.
Like in real life.

## Install 
Unzip archive to your SIT folder.

client-side DLL file goes into %SIT%/BepInEx/plugins

## Usage

- Take your gun.
- Shoot to the door (or car door, or safe, or even door with keycard access)
    - Aim for the lock for more damage.
    - Try shotguns. They do a lot of damage!
- When door HP drops to zero door will open.
- Also
    - Try to use meele weapon and granedes to open doors
    - Try to change some values in BepInEx config menu (F12) accoring to your playstyle
    - Try Lockpick ammo(sold by Mechanic by default) to deal even more damage against objects
        - Try to create your custom lockpick ammo for AKM or Mosin

## New features

- Lockpick ammo
    - You can add new ammo with high damage agains objects sold by traders. See examples in config.json
        - Build-in config.json checking on start of the server! No more runtime erros because of negative damage value!
    - Also added new options in BepInEx config menu (F12)

## TODO

- Kick locked doors make damage to you and door
- Lock door back after unlocking by key
- Penetration make sense

## Config

- You can change some values in BepInEx config menu (F12):
    - HP - More HP - more shoots to break the lock
        - ObjectHP - Same for doors, containers and car doors
    - Lock Hits - lock hits have increased damage, non-lock hits have decreased damage
        - NonLockHitDmgMult
        - LockHitDmgMult
    - Material Protection - some material should take more shoots. This is just a damage divider but you can consider this as door armor class.
        - ThinWoodProtectionMult
        - PlasticProtectionMult
        - ThickWoodProtectionMult
        - ThinMetalProtectionMult
        - ThickMetalProtectionMult
    - Specific Weapon - make certain types of weapon more effective against doors
        - MeeleWeaponDamageMult - because crowbar is effective in real life
    - Lockpick ammo - configure base damage for different tiers of new lockpick ammo

## Credits

SPT Mod:
- github: https://github.com/nektonick/Tarkov_ShootTheDoor

Original mod:
- github - https://github.com/dvize/BackdoorBandit
- SPT - https://hub.sp-tarkov.com/files/file/1154-backdoor-bandit-bb/#overview
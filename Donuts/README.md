# Donuts Rules

1. Bots will only spawn in same level/height as the spawn marker
2. Bots will only spawn in maximum distance (radius) around the spawn marker
3. One random spawn marker will be picked in a group
 - if the timer is passed its eligible to spawn (Unless IgnoreTimerFirstSpawn is true for the point. It will be set to false after a successful spawn)
 - if they are within the BotTimerTrigger distance the point is eligible to spawn.
 - If the SpawnChance is reached, it is eligible to spawn.
 - Validate that the spawn is not in a wall, in the air, in the player's line of site, minimum distance from the player.  It will attempt to find a valid point up to the Bepinex Configured Max Tries specified.
 - One to MaxRandomNumBots from the Spawn Marker info will be generated of type WildSpawnType
4. Timers will be reset if there is a successful spawn or a failure from within a group.
5. If a spawn sucessfully spawns up to their MaxSpawnsBeforeCooldown number, then it is in 'cooldown' until the timer specified in the bepinex config is reached.

Assumptions
- Spawns within a group will be on/around the same bot trigger distance otherwise only the closest spawn will be enabled.
- Each unique or standalone spawn should be given its own group number.

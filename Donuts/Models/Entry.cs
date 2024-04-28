namespace Donuts
{
    internal class Entry
    {
        public string MapName
        {
            get; set;
        }
        public int GroupNum
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        public Position Position
        {
            get; set;
        }
        public string WildSpawnType
        {
            get; set;
        }
        public float MinDistance
        {
            get; set;
        }
        public float MaxDistance
        {
            get; set;
        }

        public float BotTriggerDistance
        {
            get; set;
        }

        public float BotTimerTrigger
        {
            get; set;
        }
        public int MaxRandomNumBots
        {
            get; set;
        }

        public int SpawnChance
        {
            get; set;
        }

        public int MaxSpawnsBeforeCoolDown
        {
            get; set;
        }

        public bool IgnoreTimerFirstSpawn
        {
            get; set;
        }

        public float MinSpawnDistanceFromPlayer
        {
            get; set;
        }
    }
}

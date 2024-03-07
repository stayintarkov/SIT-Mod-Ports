using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Models
{
    public class LocationSettings
    {
        public int EscapeTimeLimit { get; set; } = int.MaxValue;
        public float VExChance { get; set; } = 50;
        public float[] BossSpawnChances { get; set; } = new float[0];

        public LocationSettings()
        {

        }

        public LocationSettings(int escapeTimeLimit) : this()
        {
            EscapeTimeLimit = escapeTimeLimit;
        }
    }
}

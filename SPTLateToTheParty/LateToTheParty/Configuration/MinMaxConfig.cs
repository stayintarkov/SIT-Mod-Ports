using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Configuration
{
    public class MinMaxConfig
    {
        [JsonProperty("min")]
        public double Min { get; set; } = 0;

        [JsonProperty("max")]
        public double Max { get; set; } = 100;

        public MinMaxConfig()
        {

        }

        public MinMaxConfig(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public void Round()
        {
            Min = Math.Round(Min);
            Max = Math.Round(Max);
        }

        public void MinFloorMaxCeiling()
        {
            Min = Math.Floor(Min);
            Max = Math.Ceiling(Max);
        }

        public static MinMaxConfig operator +(MinMaxConfig a, MinMaxConfig b)
        {
            return new MinMaxConfig(a.Min + b.Min, a.Max + b.Max);
        }

        public static MinMaxConfig operator -(MinMaxConfig a, MinMaxConfig b)
        {
            return new MinMaxConfig(a.Min - b.Min, a.Max - b.Max);
        }

        public static MinMaxConfig operator *(MinMaxConfig a, MinMaxConfig b)
        {
            return new MinMaxConfig(a.Min * b.Min, a.Max * b.Max);
        }

        public static MinMaxConfig operator /(MinMaxConfig a, MinMaxConfig b)
        {
            return new MinMaxConfig(a.Min / b.Min, a.Max / b.Max);
        }

        public static MinMaxConfig operator +(MinMaxConfig a, double b)
        {
            return new MinMaxConfig(a.Min + b, a.Max + b);
        }

        public static MinMaxConfig operator -(MinMaxConfig a, double b)
        {
            return new MinMaxConfig(a.Min - b, a.Max - b);
        }

        public static MinMaxConfig operator *(MinMaxConfig a, double b)
        {
            return new MinMaxConfig(a.Min * b, a.Max * b);
        }

        public static MinMaxConfig operator /(MinMaxConfig a, double b)
        {
            return new MinMaxConfig(a.Min / b, a.Max / b);
        }
    }
}

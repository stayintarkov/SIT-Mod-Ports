using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static LootValue.Globals;

namespace LootValue
{
	public struct LootValueConfigColor
	{
		public int UpperBound { get; set; }
		public string HexColor { get; set; }

		public LootValueConfigColor(string values)
		{
			values = values.Substring(1, values.Length-2);
			string[] split = values.Split(':');

			if (int.TryParse(split[0], out int upperBound))
				UpperBound = upperBound;
			else
				UpperBound = int.MaxValue;

			HexColor = split[1];
		}

		public LootValueConfigColor(int upperbound, string hexColor)
		{
			UpperBound = upperbound;
			HexColor = hexColor;
		}

		public override string ToString()
		{
			return $"[{UpperBound}:{HexColor}]";
		}
	}

	internal static class SlotColoring
	{
		public static readonly List<LootValueConfigColor> DefaultColors = new List<LootValueConfigColor>()
		{
			new LootValueConfigColor(5000, "#ff0000"),
			new LootValueConfigColor(7500, "#ffa500"),
			new LootValueConfigColor(10000, "#ffff00"),
			new LootValueConfigColor(15000, "#00ff00"),
			new LootValueConfigColor(20000, "#00ffff"),
			new LootValueConfigColor(int.MaxValue, "#ff00ff")
		};

		public static List<LootValueConfigColor> Colors = new List<LootValueConfigColor>();

		static SlotColoring()
		{
			Colors.AddRange(DefaultColors);
		}

		public static void ReadColors(string configString)
		{
			if (CheckConfigStringFormat(configString))
			{
				Colors.Clear();

				string[] bounds = configString.Split(',');

				foreach (string bound in bounds)
				{
					var t = new LootValueConfigColor(bound);
					Colors.Add(t);
				}
			}
			else
			{
				logger.LogError($"Custom colors string format was invalid. Load default colors");
				UseDefaultColors();
			}
		}

		public static void UseDefaultColors()
		{
			Colors.Clear();
			Colors.AddRange(DefaultColors);
		}

		private static bool CheckConfigStringFormat(string configString)
		{
			string[] bounds = configString.Split(',');

			for (int i = 0; i < bounds.Length; i++)
			{
				string bound = bounds[i];

				if (!bound.StartsWith("["))
				{
					logger.LogWarning($"Custom color format failed. Entry #{i} must start with an [");
					return false;
				} 

				if (!bound.EndsWith("]"))
				{
					logger.LogWarning($"Custom color format failed. Entry #{i} must end with an ]");
					return false;
				}

				string[] values = bound.Substring(1, bound.Length - 2).Split(':');
				if (values.Length == 2)
				{
					if (!int.TryParse(values[0], out int value))
					{
						if (i == bounds.Length - 1)
							continue;
						else
						{
							logger.LogWarning($"Custom color format failed. Entry #{i} has invalid upper bound");
							return false;
						}
					}
				}
				else
				{
					logger.LogWarning($"Custom color format failed. Entry #{i} is missing element");
					return false;
				}
			}

			return true;
		}

		private enum ESlotColor
		{
			Red,
			Orange,
			Yellow,
			Green,
			Lightblue,
			Pink
		}

		static Dictionary<ESlotColor, string> slotColors { get; } = new Dictionary<ESlotColor, string>()
		{
			{ ESlotColor.Red, "#ff0000" },
			{ ESlotColor.Orange, "#ffa500" },
			{ ESlotColor.Yellow, "#ffff00" },
			{ ESlotColor.Green, "#00ff00" },
			{ ESlotColor.Lightblue, "#00ffff" },
			{ ESlotColor.Pink, "#ff00ff" }
		};

		public static string GetColorFromValuePerSlots(int valuePerSlot)
		{
			foreach(var bound in Colors)
			{
				if (valuePerSlot < bound.UpperBound)
				{
					return bound.HexColor;
				}
			}

			return "#ff00ff";
		}
	}
}

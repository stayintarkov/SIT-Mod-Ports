using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrakiaXYZ.BotDebug.Helpers
{
    internal static class EnumExtension
    {
        public static T Next<T>(this T src) where T : Enum
        {
            T[] values = (T[])Enum.GetValues(src.GetType());
            int nextIndex = Array.IndexOf<T>(values, src) + 1;
            if (values.Length <= nextIndex)
            {
                return values[0];
            }

            return values[nextIndex];
        }

        public static T Previous<T>(this T src) where T : Enum
        {
            T[] values = (T[])Enum.GetValues(src.GetType());
            int prevIndex = Array.IndexOf<T>(values, src) - 1;
            if (prevIndex < 0)
            {
                return values[values.Length - 1];
            }

            return values[prevIndex];
        }
    }
}

using EFT.UI;
using StayInTarkov;
using System.Linq;
using System.Reflection;

namespace SamSWAT.TimeWeatherChanger.Utils
{
    static class CursorSettings
    {
		private static readonly MethodInfo setCursorMethod;

		static CursorSettings()
		{
			var cursorType = StayInTarkovHelperConstants.EftTypes.Single(x => x.GetMethod("SetCursor") != null);
			setCursorMethod = cursorType.GetMethod("SetCursor");
		}

		public static void SetCursor(ECursorType type)
		{
			setCursorMethod.Invoke(null, new object[] { type });
		}
	}
}

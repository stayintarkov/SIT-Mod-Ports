using EFT.Communications;
using StayInTarkov;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SamSWAT.TimeWeatherChanger.Utils
{
    static class Notifier
    {
        private static readonly MethodInfo notifierMessageMethod;
        private static readonly MethodInfo notifierWarningMessageMethod;

		static Notifier()
		{
			var notifierType = StayInTarkovHelperConstants.EftTypes.Single(x => x.GetMethod("DisplayMessageNotification") != null);
			notifierMessageMethod = notifierType.GetMethod("DisplayMessageNotification");
			notifierWarningMessageMethod = notifierType.GetMethod("DisplayWarningNotification");
		}

		public static void DisplayMessageNotification(string message, ENotificationDurationType duration = ENotificationDurationType.Default, ENotificationIconType iconType = ENotificationIconType.Default, Color? textColor = null)
		{
			notifierMessageMethod.Invoke(null, new object[] { message, duration, iconType, textColor });
		}

		public static void DisplayWarningNotification(string message, ENotificationDurationType duration = ENotificationDurationType.Default)
		{
			notifierWarningMessageMethod.Invoke(null, new object[] { message, duration });
		}
	}
}

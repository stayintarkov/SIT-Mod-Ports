//using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using StayInTarkov;
using System.Reflection;

using TraderClass = Trader0;

namespace LootValue
{
	internal static class NumberExtensions
	{
		public static string FormatNumber(this int s)
		{
			if (s > 1000)
				return $"{string.Format("{0:0.0}", (double)s / 1000)}k";
			return s.ToString();
		}

		public static string FormatNumber(this double s)
		{
			if (s > 1000)
				return $"{string.Format("{0:0.0}", s / 1000)}k";
			return s.ToString();
		}
	}

	internal static class TraderClassExtensions
	{
        //private static ISession Session => ClientAppUtils.GetMainApp().GetClientBackEndSession();
        private static IBackEndSession Session => StayInTarkovHelperConstants.GetMainApp().GetClientBackEndSession();

        private static readonly FieldInfo SupplyDataField =
			typeof(TraderClass).GetField("supplyData_0", BindingFlags.NonPublic | BindingFlags.Instance);

		public static SupplyData GetSupplyData(this TraderClass trader) =>
			SupplyDataField.GetValue(trader) as SupplyData;

		public static void SetSupplyData(this TraderClass trader, SupplyData supplyData) =>
			SupplyDataField.SetValue(trader, supplyData);

		public static async void UpdateSupplyData(this TraderClass trader)
		{
			Result<SupplyData> result = await Session.GetSupplyData(trader.Id);

			if (result.Failed)
				return;

			trader.SetSupplyData(result.Value);
		}
	}
}

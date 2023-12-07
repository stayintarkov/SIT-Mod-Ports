using Aki.Common.Http;
using Aki.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static LootValue.Globals;
using static System.Collections.Specialized.BitVector32;

namespace LootValue
{
	internal static class FleaPriceCache
	{
		static Dictionary<string, CachePrice> cache = new Dictionary<string, CachePrice>();

		public static double? FetchPrice(string templateId)
		{
			bool fleaAvailable = Session.RagFair.Available || LootValueMod.ShowFleaPriceBeforeAccess.Value;

			if (!fleaAvailable || !LootValueMod.EnableFleaQuickSell.Value)
				return null;

			if (cache.ContainsKey(templateId))
			{
				double secondsSinceLastUpdate = (DateTime.Now - cache[templateId].lastUpdate).TotalSeconds;
				if (secondsSinceLastUpdate > 300)
					return QueryAndTryUpsertPrice(templateId, true);
				else
					return cache[templateId].price;
			}
			else
				return QueryAndTryUpsertPrice(templateId, false);
		}

		private static string QueryPrice(string templateId)
		{
			return RequestHandler.PostJson("/LootValue/GetItemLowestFleaPrice", JsonConvert.SerializeObject(new FleaPriceRequest(templateId)));
		}

		private static double? QueryAndTryUpsertPrice(string templateId, bool update)
		{
			string response = QueryPrice(templateId);

			bool hasPlayerFleaPrice = !(string.IsNullOrEmpty(response) || response == "null");

			if (hasPlayerFleaPrice)
			{
				double price = double.Parse(response);

				if (update)
					cache[templateId].Update(price);
				else
					cache[templateId] = new CachePrice(price);

				return price;
			}

			return null;
		}
	}

	internal class CachePrice
	{
		public double price { get; private set; }
		public DateTime lastUpdate { get; private set; }

		public CachePrice(double price)
		{
			this.price = price;
			lastUpdate = DateTime.Now;
		}

		public void Update(double price)
		{
			this.price = price;
			lastUpdate = DateTime.Now;
		}
	}
}

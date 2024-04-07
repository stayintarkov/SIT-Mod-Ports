using System.Reflection;
using StayInTarkov;
using EFT;

namespace SamSWAT.WatchAnims
{
	public class GameWorldDisposePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GameWorld).GetMethod(nameof(GameWorld.Dispose));
		}

		[PatchPrefix]
		private static void PatchPrefix()
		{
			Plugin.Controllers.Clear();
		}
	}
}
using System;
using System.Reflection;
using StayInTarkov;
using EFT.InventoryLogic;

namespace InspectionlessMalfs
{
	public class KnowMalf : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(Weapon.WeaponMalfState).GetMethod("IsKnownMalfType", BindingFlags.Instance | BindingFlags.Public);
		}
		[PatchPostfix]
		private static void PatchPostfix(ref bool __result)
		{
			__result = true;
		}
	}
}
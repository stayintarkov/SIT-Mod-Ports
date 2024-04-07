using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace SamSWAT.WatchAnims
{
	[BepInPlugin("com.samswat.watchanims", "SamSWAT.WatchAnims", "1.1.0")]
	public class Plugin : BaseUnityPlugin
	{
		internal static Dictionary<IAnimator, AnimatorOverrideController> Controllers;
		internal static Dictionary<string, int> SuitsLookup;
		internal static AnimationClip[] AnimationClips;

		private void Awake()
		{
			new GamePlayerOwnerPatch().Enable();
			new GameWorldDisposePatch().Enable();
			Controllers = new Dictionary<IAnimator, AnimatorOverrideController>();
			SuitsLookup = new Dictionary<string, int>
			{
				//bear
				{"5cc0858d14c02e000c6bea66", 0},    // BEAR standard upper (DefaultBearBody)
				{"5fce3e47fe40296c1d5fd784", 0},    // bear_top_borey
				{"6377266693a3b4967208e42b", 0},    // bear_upper_SpNa
				{"5d1f565786f7743f8362bcd5", 0},    // BEAR contractor t-shirt (bear_upper_contractortshirt)
				{"5fce3e0cfe40296c1d5fd782", 0},    // bear_tshirt_termo
				{"5d1f566d86f7744bcd13459a", 0},    // BEAR Black Lynx (bear_upper_blacklynx)
				{"5d1f568486f7744bca3f0b98", 0},    // FSB Fast Response (bear_upper_fsbfastresponse)
				{"5f5e401747344c2e4f6c42c5", 0},    // bear_upper_g99
				{"5d1f567786f7744bcc04874f", 0},    // BEAR Ghost Marksman (bear_upper_ghostmarksman)
				{"5d1f564b86f7744bcb0acd16", 0},    // BEAR Summer Field (bear_upper_summerfield)
				{"6295e698e9de5e7b3751c47a", 0},    // bear_upper_tactical_long
				{"5e4bb31586f7740695730568", 0},    // bear_upper_telnik
				{"5e9d9fa986f774054d6b89f2", 0},    // bear_upper_tiger
				{"5df89f1f86f77412631087ea", 0},    // bear_upper_zaslon
				{"617bca4b4013b06b0b78df2a", 0},    // top_bear_sumrak
				{"6033a31e9ec839204e6a2f3e", 0},    // top_bear_voin
				{"657058fddf9b3231400e9188", 0},    // BEAR OPS MGS (bear_top_ops_windshirt) (new, added in 1.1.0)

				//usec
				{"5cde95d97d6c8b647a3769b0", 1},    // USEC standard upper (DefaultUsecBody)
				{"5d1f56f186f7744bcb0acd1a", 0},    // USEC Woodland Infiltrator (usec_upper_infiltrator)
				{"637b945722e2a933ed0e33c8", 1},    // usec_upper_carinthia_softshell
				{"6033a35f80ae5e2f970ba6bb", 0},    // usec_top_beltstaff
				{"5fd3e9f71b735718c25cd9f8", 1},    // usec_upper_acu
				{"5d1f56c686f7744bcd13459c", 0},    // USEC Aggressor TAC (usec_upper_aggressor)
				{"618109c96d7ca35d076b3363", 1},    // usec_upper_cereum
				{"6295e8c3e08ed747e64aea00", 1},    // usec_upper_chameleon_softshell
				{"5d4da0cb86f77450fe0a6629", 0},    // usec_upper_commando
				{"5d1f56e486f7744bce0ee9ed", 0},    // USEC Softshell Flexion (usec_upper_flexion)
				{"5e9da17386f774054b6f79a3", 0},    // usec_upper_hoody
				{"5d1f56ff86f7743f8362bcd7", 0},    // USEC PCS MultiCam (usec_upper_pcsmulticam)
				{"5d1f56a686f7744bce0ee9eb", 0},    // USEC PCU Ironsight (usec_upper_pcuironsight)
				{"5fcf63da5c287f01f22bf245", 1},    // usec_upper_tier3
				{"5e4bb35286f77406a511c9bc", 1},    // usec_upper_tier_2
				{"5f5e4075df4f3100376a8138", 1},    // user_upper_NightPatrol
				{"6571cb0923aa6d72760a7f8f", 1},    // USEC BOSS Delta (usec_upper_velocity) (new, added in 1.1.0)

				// usec and bear
				{"64ef3efdb63b74469b6c1499", 1}     // USEC Predator (pmc_upper_blackknight_quest) (new, added in 1.1.0)

				//???
				//{"5cdea33e7d6c8b0474535dac", 0}
				//default = 0
			};
			var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			AnimationClips = AssetBundle.LoadFromFile($"{directory}/bundles/watch animations.bundle").LoadAllAssets<AnimationClip>();
			//GameWorld.OnDispose += () => Controllers.Clear(); doesn't exist yet in eft 3.4.x
		}
	}
}
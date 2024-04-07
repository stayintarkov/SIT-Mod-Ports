using System.Collections;
using System.Reflection;
using StayInTarkov;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using UnityEngine;

namespace SamSWAT.WatchAnims
{
	public class GamePlayerOwnerPatch : ModulePatch
	{
		private const string STATE_NAME = "hand_nv_on";
		private static AnimationClip _defaultClip;
		private static Coroutine _coroutine;

		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(GamePlayerOwner), "TranslateCommand");
		}

		[PatchPostfix]
		private static void PatchPostfix(ECommand command)
		{
			if (command != ECommand.DisplayTimer && command != ECommand.DisplayTimerAndExits) return;

			var player = Singleton<GameWorld>.Instance.MainPlayer;

			if (player.Side == EPlayerSide.Savage) return;

			var animator = player.HandsController.FirearmsAnimator.Animator;
			var animIndex = GetAnimIndex(player);

			if (Plugin.Controllers.TryGetValue(animator, out var overrideController))
			{
				StaticManager.KillCoroutine(_coroutine);
				overrideController[STATE_NAME] = Plugin.AnimationClips[animIndex];
			}
			else
			{
				overrideController = CreateOverrideController(animator, animIndex);
			}

			player.HandsController.Interact(true, 21);
			_coroutine = StaticManager.BeginCoroutine(Wait(Plugin.AnimationClips[animIndex].length, overrideController));
		}

		private static AnimatorOverrideController CreateOverrideController(IAnimator animator, int animIndex)
		{
			var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
			_defaultClip = overrideController[STATE_NAME];
			overrideController[STATE_NAME] = Plugin.AnimationClips[animIndex];
			Plugin.Controllers.Add(animator, overrideController);
			animator.runtimeAnimatorController = overrideController;
			return overrideController;
		}

		private static int GetAnimIndex(Player player)
		{
			var suit = Traverse.Create(player.PlayerBody.BodyCustomizationId).Method("get_Value").GetValue<string>();
			return Plugin.SuitsLookup.TryGetValue(suit, out var index) ? index : 0;
		}

		private static IEnumerator Wait(float time, AnimatorOverrideController controller)
		{
			yield return new WaitForSecondsRealtime(time + 0.3f);
			controller[STATE_NAME] = _defaultClip;
		}
	}
}
using Comfort.Common;
using EFT;
using ThatsLit.Components;
using System.Collections.Generic;
using UnityEngine;

namespace ThatsLit
{
    public class GameWorldHandler
    {
        public static void Update()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null && ThatsLitGameWorld != null)
            {
                Object.Destroy(ThatsLitGameWorld);
            }
            else if (gameWorld != null && ThatsLitGameWorld == null)
            {
                ThatsLitGameWorld = gameWorld.GetOrAddComponent<ThatsLitGameworldComponent>();
            }
        }

        public static ThatsLitGameworldComponent ThatsLitGameWorld { get; private set; }
        public static ThatsLitMainPlayerComponent ThatsLitMainPlayer => ThatsLitGameWorld?.SAINMainPlayer;
    }
}

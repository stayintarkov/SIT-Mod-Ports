using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAIN.Helpers;
using UnityEngine;

namespace SAIN.Components
{
    public class DebugData : MonoBehaviour
    {
        private Dictionary<BotOwner, OverlayData> botInfo = new Dictionary<BotOwner, OverlayData>();

        private readonly float markerRadius = 0.5f;
        private float screenScale = 1.0f;
        private GUIStyle guiStyle;

        public void RegisterBot(BotOwner bot)
        {
            OverlayData overlayData = new OverlayData();
            botInfo.Add(bot, overlayData);
        }

        private void Awake()
        {
            
        }

        private void Update()
        {
            updateBotInfo();
        }

        private void OnGUI()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                return;
            }

            if (guiStyle == null)
            {
                guiStyle = DebugHelpers.CreateGuiStyle();
            }
            
            updateBotOverlays();
        }

        private void updateBotInfo()
        {
            foreach (BotOwner bot in botInfo.Keys.ToArray())
            {
                if ((bot == null) || bot.IsDead)
                {
                    botInfo.Remove(bot);
                    continue;
                }

                // Don't update the overlay too often or performance and RAM usage will be affected
                if (botInfo[bot].LastUpdateElapsedTime < 100)
                {
                    continue;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLabeledValue("Name", bot.Profile.Info.Nickname, Color.magenta, Color.magenta);
                sb.AppendLabeledValue("Brain", bot.Brain.BaseBrain.ShortName(), Color.cyan, Color.cyan);
                sb.AppendLabeledValue("Layer", bot.Brain.ActiveLayerName(), Color.yellow, Color.yellow);
                sb.AppendLabeledValue("Reason", bot.Brain.GetActiveNodeReason(), Color.white, Color.white);

                botInfo[bot].StaticText = sb.ToString();

                botInfo[bot].ResetUpdateTime();
            }
        }

        private static Color getColorForBotType(BotOwner bot)
        {
            if (bot == null)
            {
                return Color.white;
            }

            Color botTypeColor = Color.green;

            // If you're dead, there's no reason to worry about overlay colors
            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                return botTypeColor;
            }

            // Check if the bot doesn't like you
            if (bot.EnemiesController?.EnemyInfos?.Any(i => i.Value.ProfileId == mainPlayer.ProfileId) == true)
            {
                botTypeColor = Color.red;
            }

            return botTypeColor;
        }

        private void updateBotOverlays()
        {

            foreach (BotOwner bot in botInfo.Keys.ToArray())
            {
                Vector3 botHeadPosition = bot.Position + new Vector3(0, 1.5f, 0);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(botHeadPosition);
                if (screenPos.z <= 0)
                {
                    continue;
                }

                Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                double distanceToBot = Math.Round(Vector3.Distance(bot.Position, mainPlayer.Position), 1);

                StringBuilder sb = new StringBuilder();
                sb.Append(botInfo[bot].StaticText);
                sb.AppendLabeledValue("Distance", distanceToBot + "m", Color.white, Color.white);
                botInfo[bot].GuiContent.text = sb.ToString();

                Vector2 guiSize = guiStyle.CalcSize(botInfo[bot].GuiContent);
                float x = (screenPos.x * screenScale) - (guiSize.x / 2);
                float y = Screen.height - ((screenPos.y * screenScale) + guiSize.y);
                Rect rect = new Rect(new Vector2(x, y), guiSize);
                botInfo[bot].GuiRect = rect;

                GUI.Box(botInfo[bot].GuiRect, botInfo[bot].GuiContent, guiStyle);
            }
        }

        internal class OverlayData
        {
            public ActorDataStruct Data { get; set; }
            public GUIContent GuiContent { get; set; }
            public Rect GuiRect { get; set; }
            public Vector3 Position { get; set; }
            public string StaticText { get; set; } = "";

            private Stopwatch updateTimer = Stopwatch.StartNew();

            public long LastUpdateElapsedTime => updateTimer.ElapsedMilliseconds;

            public OverlayData()
            {
                GuiContent = new GUIContent();
                GuiRect = new Rect();
            }

            public OverlayData(Vector3 _position) : this()
            {
                Position = _position;
            }

            public OverlayData(Vector3 _position, string _staticText) : this(_position)
            {
                StaticText = _staticText;
            }

            public void ResetUpdateTime()
            {
                updateTimer.Restart();
            }
        }
    }
}
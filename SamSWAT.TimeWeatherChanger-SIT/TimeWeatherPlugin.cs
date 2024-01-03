using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Console.Core;
using EFT.UI;
using EFT.Weather;
using SamSWAT.TimeWeatherChanger.Utils;
using StayInTarkov;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SamSWAT.TimeWeatherChanger
{
    [BepInPlugin("com.samswat.timeweatherchanger", "SamSWAT.TimeWeatherChanger", "2.3.0")]
    public class TimeWeatherPlugin : BaseUnityPlugin
    {
        internal static ConfigEntry<KeyboardShortcut> TogglePanel;

        private static GameObject input;
        private static WeatherController weatherController;
        private static GameWorld gameWorld;

        private static Type gameDateTime;
        private static MethodInfo calculateTime;
        private static MethodInfo resetTime;

        private static DateTime modifiedDateTime;
        private static DateTime currentDateTime;
        private static Rect windowRect = new Rect(50, 50, 460, 365);
        private static bool guiStatus = false;
        private static bool weatherDebug = false;

        private static float cloudDensity;
        private static float fog;
        private static float rain;
        private static float lightningThunderProb;
        private static float temperature;
        private static float windMagnitude;
        private static int windDir = 2;
        private static WeatherDebug.Direction windDirection;
        private static int topWindDir = 2;
        private static Vector2 topWindDirection;

        private static string weatherDebugTex;
        private static int targetTimeHours;
        private static int targetTimeMinutes;

        public void Awake()
        {
            //Getting type responsible for time in the current world for later use
            gameDateTime = StayInTarkovHelperConstants.EftTypes.Single(x => x.GetMethod("CalculateTaxonomyDate") != null);
            calculateTime = gameDateTime.GetMethod("Calculate", BindingFlags.Public | BindingFlags.Instance);
            resetTime = gameDateTime.GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(x => x.Name == "Reset" && x.GetParameters().Length == 1);
            ConsoleScreen.Processor.RegisterCommand("twc", new Action(OpenPanel));

            TogglePanel = Config.Bind(
                "Main Settings",
                "Time Weather Panel Toggle Key",
                new KeyboardShortcut(KeyCode.Home),
                "The keyboard shortcut that toggles control panel");
        }

        [ConsoleCommand("Open Time&Weather panel")]
        public static void OpenPanel()
        {
            gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                string log = "In order to change the weather, you have to go in a raid first.";
                Notifier.DisplayWarningNotification(log, ENotificationDurationType.Long);
                ConsoleScreen.Log($"[TWChanger]: {log}");
            }
            else
            {
                if (GameObject.Find("Weather") == null)
                {
                    string log = "An error occurred when executing command, seems like you are either in the hideout or factory.";
                    Notifier.DisplayWarningNotification(log);
                    ConsoleScreen.Log($"[TWChanger]: {log}");
                }
                else
                {
                    if (input == null)
                    {
                        input = GameObject.Find("___Input");
                    }

                    guiStatus = !guiStatus;
                    Cursor.visible = guiStatus;
                    if (guiStatus)
                    {
                        CursorSettings.SetCursor(ECursorType.Idle);
                        Cursor.lockState = CursorLockMode.None;
                        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
                    }
                    else
                    {
                        CursorSettings.SetCursor(ECursorType.Invisible);
                        Cursor.lockState = CursorLockMode.Locked;
                        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuDropdown);
                    }
                    input.SetActive(!guiStatus);
                    ConsoleScreen.Log("[TWChanger]: Switching control panel...");
                }
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(TogglePanel.Value.MainKey))
            {
                //Obtaining current GameWorld for later time change
                gameWorld = Singleton<GameWorld>.Instance;
                //If GameWorld is null, it means that player currently is not in the raid. We notify the player with an error screen that he first needs to go in a raid
                if (gameWorld == null)
                {
                    if (GameObject.Find("ErrorScreen"))
                        PreloaderUI.Instance.CloseErrorScreen();
                    PreloaderUI.Instance.ShowErrorScreen("Time & Weather Changer Error", "In order to change the weather, you have to go in a raid first.");
                }
                else
                {
                    //Looking for the weather GameObject to which the WeatherController script is attached. If it's null, means that the player is either in a hideout or factory where dynamic weather and time are not available 
                    if (GameObject.Find("Weather") == null)
                    {
                        //Notify player with bottom-right error popup
                        Notifier.DisplayWarningNotification("An error occurred when opening weather panel, seems like you are either in the hideout or factory.");
                    }
                    else
                    {
                        //Caching input manager GameObject which script is responsible for reading the player inputs
                        if (input == null)
                        {
                            input = GameObject.Find("___Input");
                        }

                        guiStatus = !guiStatus;
                        Cursor.visible = guiStatus;
                        if (guiStatus)
                        {
                            //Changing the default windows cursor to an EFT-style one and playing a sound when the menu appears
                            CursorSettings.SetCursor(ECursorType.Idle);
                            Cursor.lockState = CursorLockMode.None;
                            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
                        }
                        else
                        {
                            //Hiding cursor and playing a sound when the menu disappears
                            CursorSettings.SetCursor(ECursorType.Invisible);
                            Cursor.lockState = CursorLockMode.Locked;
                            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuDropdown);
                        }
                        //Disabling the input manager so the player won't move
                        input.SetActive(!guiStatus);
                    }
                }
            }
        }
        public void OnGUI()
        {
            if (guiStatus)
                windowRect = GUI.Window(0, windowRect, WindowFunction, "Time & Weather Changer by SamSWAT v2.3");
        }
        void WindowFunction(int TWCWindowID)
        {
            if (weatherDebug)
                weatherDebugTex = "ON";
            else
                weatherDebugTex = "OFF";

            //Caching WeatherController script for the first time
            if (weatherController == null)
            {
                weatherController = GameObject.Find("Weather").GetComponent<WeatherController>();
            }

            //Shit ton of different sliders and buttons

            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            //---------------------------------------------\\

            GUI.Box(new Rect(15, 23, 140, 60), "");
            weatherDebug = GUI.Toggle(new Rect(33, 37, 110, 25), weatherDebug, "Weather debug");
            GUI.Label(new Rect(74, 52, 110, 25), weatherDebugTex);

            currentDateTime = (DateTime)calculateTime.Invoke(typeof(GameWorld).GetField("GameDateTime").GetValue(gameWorld), null);
            GUI.Box(new Rect(160, 23, 285, 60), "Current time: " + currentDateTime.ToString("HH:mm:ss"));

            GUI.Label(new Rect(190, 42, 40, 20), "Hours");
            targetTimeHours = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(165, 60, 80, 15), targetTimeHours, 0, 23));
            GUI.TextField(new Rect(248, 50, 20, 20), targetTimeHours.ToString());

            GUI.Label(new Rect(295, 42, 50, 20), "Minutes");
            targetTimeMinutes = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(272, 60, 90, 15), targetTimeMinutes, 0, 59));
            GUI.TextField(new Rect(365, 50, 20, 20), targetTimeMinutes.ToString());

            if (GUI.Button(new Rect(390, 40, 50, 30), "Set"))
            {
                modifiedDateTime = currentDateTime.AddHours((double)targetTimeHours - currentDateTime.Hour);
                modifiedDateTime = modifiedDateTime.AddMinutes((double)targetTimeMinutes - currentDateTime.Minute);
                resetTime.Invoke(typeof(GameWorld).GetField("GameDateTime").GetValue(gameWorld), new object[] { modifiedDateTime });
                Notifier.DisplayMessageNotification("Time was set to: " + modifiedDateTime.ToString("HH:mm"));
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInspectorWindowClose);
            }

            //---------------------------------------------\\
            //---------------------------------------------\\

            GUILayout.BeginArea(new Rect(15, 100, 140, 250));
            GUILayout.BeginVertical();

            GUILayout.Box("Cloud Density: " + Math.Round(cloudDensity * 1000) / 1000);
            cloudDensity = GUILayout.HorizontalSlider(cloudDensity, -1f, 1f);

            GUILayout.Box("Wind Magnitude: " + Math.Round(windMagnitude * 100) / 100);
            windMagnitude = GUILayout.HorizontalSlider(windMagnitude, 0f, 1f);

            GUILayout.Box("Wind Direction: " + windDirection.ToString());
            windDir = Mathf.RoundToInt(GUILayout.HorizontalSlider(windDir, 1, 8));
            windDirection = (WeatherDebug.Direction)windDir;

            if (GUILayout.Button("Clear"))
            {
                cloudDensity = -0.7f;
                fog = 0.004f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Clear Wind"))
            {
                cloudDensity = -0.7f;
                fog = 0.004f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0.4f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Clear Fog"))
            {
                cloudDensity = -0.4f;
                fog = 0.02f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Partly Cloud"))
            {
                cloudDensity = -0.2f;
                fog = 0.004f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Mostly Cloud"))
            {
                cloudDensity = 0f;
                fog = 0.004f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0f;
                topWindDir = Random.Range(0, 5);
            }


            GUILayout.EndVertical();
            GUILayout.EndArea();

            //---------------------------------------------\\
            //---------------------------------------------\\

            GUILayout.BeginArea(new Rect(160, 100, 140, 160));
            GUILayout.BeginVertical();

            GUILayout.Box("Fog: " + Math.Round(fog * 1000) / 1000);
            fog = GUILayout.HorizontalSlider(fog, 0f, 0.35f);

            GUILayout.Box("Thunder prob: " + Math.Round(lightningThunderProb * 1000) / 1000);
            lightningThunderProb = GUILayout.HorizontalSlider(lightningThunderProb, 0f, 1f);

            if (GUILayout.Button("Randomize"))
            {
                float num = Random.Range(-1f, 1f);
                int num3;
                rain = 0f;
                cloudDensity = num;
                if (num > 0.5f)
                {
                    num3 = Random.Range(0, 5);
                    rain = Random.Range(0f, 1f);
                    fog = 0.004f;
                    switch (num3)
                    {
                        case 0:
                            break;
                        case 1:
                            fog = 0.008f;
                            goto IL_C5;
                        case 2:
                            fog = 0.012f;
                            goto IL_C5;
                        case 3:
                            fog = 0.02f;
                            goto IL_C5;
                        case 4:
                            fog = 0.03f;
                            goto IL_C5;
                        default:
                            goto IL_C5;
                    }
                }
                fog = Random.Range(0.003f, 0.006f);
            IL_C5:
                windDir = Random.Range(1, 8);
                topWindDir = Random.Range(0, 5);
                windMagnitude = Random.Range(0f, 1f);
                lightningThunderProb = Random.Range(0f, 1f);
                temperature = Random.Range(-50f, 50f);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            //---------------------------------------------\\
            //---------------------------------------------\\

            GUILayout.BeginArea(new Rect(160, 229, 140, 160));
            GUILayout.BeginVertical();

            if (GUILayout.Button("Full Cloud"))
            {
                cloudDensity = 1f;
                fog = 0.004f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Cloud Wind"))
            {
                cloudDensity = 0.2f;
                fog = 0.003f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0.66f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Cloud Fog"))
            {
                cloudDensity = 0.2f;
                fog = 0.02f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Thunder Cloud"))
            {
                cloudDensity = 1f;
                fog = 0.004f;
                lightningThunderProb = 0.8f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0.4f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Cloud Wind Rain"))
            {
                cloudDensity = 1f;
                fog = 0.004f;
                lightningThunderProb = 0.5f;
                rain = 0.8f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0.6f;
                topWindDir = Random.Range(0, 5);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            //---------------------------------------------\\
            //---------------------------------------------\\

            GUILayout.BeginArea(new Rect(305, 100, 140, 250));
            GUILayout.BeginVertical();

            GUILayout.Box("Rain: " + Math.Round(rain * 1000) / 1000);
            rain = GUILayout.HorizontalSlider(rain, 0f, 1f);

            GUILayout.Box("Temperature: " + Math.Round(temperature * 10) / 10);
            temperature = GUILayout.HorizontalSlider(temperature, -50f, 50f);

            GUILayout.Box("TopWind dir " + topWindDirection.ToString());
            topWindDir = Mathf.RoundToInt(GUILayout.HorizontalSlider(topWindDir, 1, 6));

            switch (topWindDir)
            {
                case 1:
                    topWindDirection = Vector2.down;
                    break;
                case 2:
                    topWindDirection = Vector2.left;
                    break;
                case 3:
                    topWindDirection = Vector2.one;
                    break;
                case 4:
                    topWindDirection = Vector2.right;
                    break;
                case 5:
                    topWindDirection = Vector2.up;
                    break;
                case 6:
                    topWindDirection = Vector2.zero;
                    break;
            }

            if (GUILayout.Button("Light Rain"))
            {
                cloudDensity = -0.1f;
                fog = 0.004f;
                lightningThunderProb = 0f;
                rain = 0.5f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Rain"))
            {
                cloudDensity = 0.05f;
                fog = 0.004f;
                lightningThunderProb = 0.3f;
                rain = 1f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0.15f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Fog"))
            {
                cloudDensity = -0.4f;
                fog = 0.1f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 22f;
                windDir = Random.Range(1, 8);
                windMagnitude = 0f;
                topWindDir = Random.Range(0, 5);
            }
            if (GUILayout.Button("Default"))
            {
                cloudDensity = -0.3f;
                fog = 0.004f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 20f;
                windDir = 7;
                windMagnitude = 0.75f;
                topWindDir = 2;
            }
            if (GUILayout.Button("BSG Preset"))
            {
                cloudDensity = -0.371f;
                fog = 0.009f;
                lightningThunderProb = 0f;
                rain = 0f;
                temperature = 0f;
                windDir = 8;
                windMagnitude = 0.125f;
                topWindDir = 2;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            //---------------------------------------------\\

            if (!weatherDebug)
                GUI.Box(new Rect(10, 95, 440, 260), "");

            //Writing all the parameters selected by the player to the WeatherDebug script
            weatherController.WeatherDebug.Enabled = weatherDebug;
            weatherController.WeatherDebug.CloudDensity = cloudDensity;
            weatherController.WeatherDebug.Fog = fog;
            weatherController.WeatherDebug.LightningThunderProbability = lightningThunderProb;
            weatherController.WeatherDebug.Rain = rain;
            weatherController.WeatherDebug.Temperature = temperature;
            weatherController.WeatherDebug.TopWindDirection = topWindDirection;
            weatherController.WeatherDebug.WindDirection = windDirection;
            weatherController.WeatherDebug.WindMagnitude = windMagnitude;

        }
    }
}

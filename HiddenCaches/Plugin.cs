using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using System.Collections;
using EFT.Interactive;
using System.Linq;

namespace RaiRai.HiddenCaches
{
    [BepInPlugin("com.rairai.hiddencaches.eft", "HiddenCaches", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        internal static ConfigEntry<Color> configColor;
        internal static ConfigEntry<Boolean> configAudio;
        internal static ConfigEntry<Boolean> configLight;
        internal static ConfigEntry<Boolean> configSmoke;

        public Plugin()
        {
            Plugin.Log = base.Logger;
            Plugin.Log.LogInfo("Loading plugin!");
            try
            {
                InitConfig();

                new CachePatch().Enable();
                new RaidEndPatch().Enable();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex.ToString());
            }
            Plugin.Log.LogInfo("Loaded plugin!");
        }

        private void InitConfig()
        {
            configAudio = Config.Bind("", "Audio", enabled, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 4 }));
            configLight = Config.Bind("", "Light", enabled, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 3 }));
            configSmoke = Config.Bind("", "Smoke", enabled, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 2 }));
            configColor = Config.Bind("Color", "", new Color(1.0f, 0.75f / 2, 0.0f), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));
            
            Config.Bind("Color", "Apply", "", new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0, HideDefaultButton = true, CustomDrawer = new Action<ConfigEntryBase>(ApplyDrawer) }));
        }
        private void ApplyDrawer(ConfigEntryBase configEntry)
        {
            if (GUILayout.Button("Apply", GUILayout.ExpandWidth(true)))
            {
                Color chosenColor = new Color (configColor.Value.r * 2, configColor.Value.g * 2, configColor.Value.b * 2);
                if (CachePatch.hiddenCacheList != null)
                {
                    StartCoroutine(UpdateColors(chosenColor));
                }
            }

            IEnumerator UpdateColors(Color chosenColor)
            {
                foreach (LootableContainer container in CachePatch.hiddenCacheList)
                {
                    AudioSource audioSource = container.GetComponent<AudioSource>();
                    audioSource.enabled = configAudio.Value;
                    audioSource.Play();
                    
                    Light componentLightObject = container.GetComponent<Light>();
                    componentLightObject.Reset();
                    componentLightObject.color = chosenColor;
                    componentLightObject.range = 3f;
                    componentLightObject.enabled = configLight.Value;

                    ParticleSystem particleSystem = container.GetOrAddComponent<ParticleSystem>();
                    ParticleSystemRenderer componentParticleSys = container.GetComponent<ParticleSystemRenderer>();
                    componentParticleSys.enabled = configSmoke.Value;
                    componentParticleSys.material.SetColor("_TintColor", chosenColor);
                    particleSystem.Play();
                }
                yield return null;
            }
        }
    }
}

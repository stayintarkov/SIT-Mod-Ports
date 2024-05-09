using Comfort.Common;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace RaiRai.HiddenCaches
{
    internal static class BundleLoader
    {
        internal static AudioClip audioClip;
        internal static Material material;
        internal static ParticleSystem particleSystem;

        internal static async Task PopulateComponentsAsync()
        {
            var loadedBundle = await LoadBundle();
            audioClip = loadedBundle.Item1;
            material = loadedBundle.Item2;
            particleSystem = loadedBundle.Item3;
        }

        internal static async Task<Tuple<AudioClip, Material, ParticleSystem>> LoadBundle()
        {
            var PATH = "assets/content/location_objects/lootable/prefab/scontainer_crate.bundle";

            var easyAssets = Singleton<PoolManager>.Instance.EasyAssets;
            await easyAssets.Retain(PATH, null, null).LoadingJob;


            try
            {
                var allComponents = easyAssets.GetAsset<GameObject>(PATH).GetComponentsInChildren<Component>();

                foreach (var component in allComponents)
                {
                    if (component.name == "Flare_Smoke" && component.GetType().Name == "ParticleSystemRenderer")
                    {
                        ParticleSystemRenderer renderer = component.GetComponent<ParticleSystemRenderer>();
                        material = renderer.material;
                        continue;
                    }

                    if (component.name == "Flare_Audio" && component.GetType().Name == "AudioSource")
                    {
                        AudioSource audioSource = component.GetComponent<AudioSource>();
                        audioClip = audioSource.clip;
                        continue;
                    }

                    if (component.name == "Flare_Smoke" && component.GetType().Name == "ParticleSystem")
                    {
                        particleSystem = component.GetComponent<ParticleSystem>();
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }

            return Tuple.Create(audioClip, material, particleSystem);

        }
    }
}

using UnityEngine;

namespace RaiRai.HiddenCaches
{
    internal class FlareComponent : MonoBehaviour
    {
        public async void Start()
        {
            if (BundleLoader.material == null ||
                BundleLoader.audioClip == null ||
                BundleLoader.particleSystem == null)
            {
                await BundleLoader.PopulateComponentsAsync();
            }

            Color chosenColor = new Color(Plugin.configColor.Value.r * 2, Plugin.configColor.Value.g * 2, Plugin.configColor.Value.b * 2);

            Light lightObject = this.GetOrAddComponent<Light>();
            lightObject.Reset();
            lightObject.color = chosenColor;
            lightObject.range = 3f;
            lightObject.enabled = Plugin.configLight.Value;

            UnityEngine.AudioSource audioSource = this.GetOrAddComponent<UnityEngine.AudioSource>();
            audioSource.clip = BundleLoader.audioClip;
            audioSource.loop = true;
            audioSource.maxDistance = 8;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.182f;
            audioSource.enabled = Plugin.configAudio.Value;

            ParticleSystem particleSystem = this.GetOrAddComponent<ParticleSystem>();
            ParticleSystem.MainModule mainModule = particleSystem.main;
            ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
            emissionModule.rateOverTime = 15f;
            mainModule.gravityModifier = -0.01f;
            mainModule.maxParticles = 150;
            particleSystem.time = 0.4902f;
            mainModule.startColor = new Color(1, 1, 1, 0.2f);
            mainModule.startLifetime = 10;
            mainModule.startSpeed = 3;
            mainModule.startSize = 1;
            mainModule.scalingMode = ParticleSystemScalingMode.Shape;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetimeModule = particleSystem.colorOverLifetime;
            colorOverLifetimeModule.enabled = true;
            colorOverLifetimeModule.color = BundleLoader.particleSystem.colorOverLifetime.color;

            ParticleSystem.ShapeModule shapeModule = particleSystem.shape;
            shapeModule.radius = 0.01f;

            ParticleSystem.TextureSheetAnimationModule textureSheetAnimationModule = particleSystem.textureSheetAnimation;
            textureSheetAnimationModule.enabled = true;
            textureSheetAnimationModule.numTilesX = 8;
            textureSheetAnimationModule.numTilesY = 8;

            ParticleSystem.LimitVelocityOverLifetimeModule limitVelocityModule = particleSystem.limitVelocityOverLifetime;
            limitVelocityModule.enabled = true;
            limitVelocityModule.dampen = 1f;
            limitVelocityModule.limitMultiplier = 0.4f;

            mainModule.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            ParticleSystemRenderer particleSystemRenderer = this.GetOrAddComponent<ParticleSystemRenderer>();
            particleSystemRenderer.enableGPUInstancing = false;
            particleSystemRenderer.maxParticleSize = 20f;
            particleSystemRenderer.receiveShadows = true;
            particleSystemRenderer.material = BundleLoader.material;
            particleSystemRenderer.material.SetColor("_LocalMinimalAmbientLight", new Color(1f, 1f, 1f, 1f));
            particleSystemRenderer.material.SetColor("_TintColor", chosenColor);
            particleSystemRenderer.enabled = Plugin.configSmoke.Value;

            audioSource.Play();
            particleSystem.Play();
        }
    }
}
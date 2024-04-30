using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using UnityEngine;
using static EFT.InventoryLogic.Weapon;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class Recoil : SAINBase, ISAINClass
    {
        public Recoil(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (_shot)
            {
                _count++;
                CurrentRecoilOffset = Vector3.Lerp(CurrentRecoilOffset, _recoilOffsetTarget, _count / 3f);
                if (_count >= 3)
                {
                    _shot = false;
                    _recoilOffsetTarget = Vector3.zero;
                    _count = 0;
                }
            }

            if (CurrentRecoilOffset != Vector3.zero)
            {
                Vector3 decayedRecoil = Vector3.Lerp(CurrentRecoilOffset, Vector3.zero, SAINPlugin.LoadedPreset.GlobalSettings.Shoot.RecoilDecay);
                if ((decayedRecoil -  CurrentRecoilOffset).sqrMagnitude < 0.05f)
                {
                    decayedRecoil = Vector3.zero;
                }
                CurrentRecoilOffset = decayedRecoil;
            }
        }

        private int _count;

        public void Dispose()
        {
        }

        public void WeaponShot()
        {
            _shot = true;
            _count = 0;
            _recoilOffsetTarget = CalculateRecoil(_recoilOffsetTarget);
        }

        private bool _shot;

        private Vector3 _recoilOffsetTarget;
        public Vector3 CurrentRecoilOffset { get; private set; } = Vector3.zero;

        public Vector3 CalculateRecoil(Vector3 currentRecoil)
        {
            float distance = SAIN.DistanceToAimTarget;

            // Reduces scatter recoil at very close range. Clamps Distance between 3 and 20 then scale to 0.25 to 1.
            // So if a target is 3m or less Distance, their recoil scaling will be 25% its original value
            distance = Mathf.Clamp(distance, 3f, 20f);
            distance /= 20f;
            distance = distance * 0.75f + 0.25f;

            float weaponhorizrecoil = CalcHorizRecoil(SAIN.Info.WeaponInfo.RecoilForceUp);
            float weaponvertrecoil = CalcVertRecoil(SAIN.Info.WeaponInfo.RecoilForceBack);

            float addRecoil = SAINPlugin.LoadedPreset.GlobalSettings.Shoot.AddRecoil;
            float horizRecoil = (1f * (weaponhorizrecoil + addRecoil));
            float vertRecoil = (1f * (weaponvertrecoil + addRecoil));

            float maxrecoil = SAINPlugin.LoadedPreset.GlobalSettings.Shoot.MaxRecoil;

            float randomHorizRecoil = Random.Range(-horizRecoil, horizRecoil);
            float randomvertRecoil = Random.Range(-vertRecoil, vertRecoil);
            Vector3 newRecoil = new Vector3(randomHorizRecoil, randomvertRecoil, randomHorizRecoil);
            newRecoil = MathHelpers.VectorClamp(newRecoil, -maxrecoil, maxrecoil) * RecoilMultiplier;

            if (Player.IsInPronePose)
            {
                newRecoil *= 0.8f;
            }
            var shootController = BotOwner.WeaponManager.ShootController;
            if (shootController != null && shootController.IsAiming == true)
            {
                newRecoil *= 0.8f;
            }

            Vector3 vector = newRecoil + currentRecoil;
            return vector;
        }

        private float RecoilMultiplier => Mathf.Round(SAIN.Info.FileSettings.Shoot.RecoilMultiplier * GlobalSettings.Shoot.GlobalRecoilMultiplier * 100f) / 100f;

        float CalcVertRecoil(float recoilVal)
        {
            float result = recoilVal / 100;
            if (ModDetection.RealismLoaded)
            {
                result = recoilVal / 150;
            }
            result *= SAIN.Info.WeaponInfo.FinalModifier;
            result *= UnityEngine.Random.Range(0.8f, 1.2f);
            return result;
        }

        float CalcHorizRecoil(float recoilVal)
        {
            float result = recoilVal / 200;
            if (ModDetection.RealismLoaded)
            {
                result = recoilVal / 300;
            }
            result *= SAIN.Info.WeaponInfo.FinalModifier;
            result *= UnityEngine.Random.Range(0.8f, 1.2f);
            return result;
        }

        public Vector3 CalculateDecay(Vector3 oldVector)
        {
            if (oldVector == Vector3.zero) return oldVector;

            Vector3 decayed = Vector3.Lerp(Vector3.zero, oldVector, SAINPlugin.LoadedPreset.GlobalSettings.Shoot.RecoilDecay);
            if ((decayed - Vector3.zero).sqrMagnitude < 0.01f)
            {
                decayed = Vector3.zero;
            }
            return decayed;
        }

        private float RecoilBaseline
        {
            get
            {
                if (ModDetection.RealismLoaded)
                {
                    return 225f;
                }
                else
                {
                    return 112f;
                }
            }
        }
    }
}

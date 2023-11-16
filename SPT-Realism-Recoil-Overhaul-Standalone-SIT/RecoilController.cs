using Comfort.Common;
using EFT.Animations;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RecoilStandalone
{


    public class RecoilController
    {
        public static float GetRecoilAngle(Weapon weap)
        {
            switch (weap.WeapClass)
            {
                case "pistol":
                    return 95f;
                case "shotgun":
                    return 75f;
                case "sniperRifle":
                    if (!weap.WeapFireType.Contains(Weapon.EFireMode.fullauto) && weap.Template.BoltAction)
                    {
                        return 80f;
                    }
                    return 75f;
                case "marksmanRifle":
                    return 75f;
                default:
                    return 85;
            }
        }

        public static float GetConvergenceMulti(Weapon weap) 
        {
           switch (weap.WeapClass)
            {
                case "smg":
                    return 1.25f;
                case "pistol":
                    if (weap.Template.Convergence >= 4)
                    {
                        return 0.25f;
                    }
                    return 1f;
                case "shotgun":
                    return 0.25f;
                case "sniperRifle":
                    if (!weap.WeapFireType.Contains(Weapon.EFireMode.fullauto) && weap.Template.BoltAction)
                    {
                        return 0.25f;
                    }
                    return 1f;
                case "marksmanRifle":
                    if (!weap.WeapFireType.Contains(Weapon.EFireMode.fullauto)) 
                    {
                        return 0.9f;
                    }
                    return 1f;
                default:
                    return 1;
            }
        }

        public static float GetVRecoilMulti(Weapon weap)
        {
            switch (weap.WeapClass)
            {
                case "smg":
                    return 1f;
                case "pistol":
                    return 0.65f;
                case "shotgun":
                    return 2f;
                case "sniperRifle":
                    if (!weap.WeapFireType.Contains(Weapon.EFireMode.fullauto) && weap.Template.BoltAction)
                    {
                        return 1.65f;
                    }
                    return 1f;
                case "marksmanRifle":
                    if (!weap.WeapFireType.Contains(Weapon.EFireMode.fullauto))
                    {
                        return 1.15f;
                    }
                    return 1f;
                default:
                    return 1;
            }
        }

        public static float GetCamRecoilMulti(Weapon weap)
        {
            switch (weap.WeapClass)
            {
                case "pistol":
                    return 0.5f;
                case "shotgun":
                    return 0.8f;
                case "sniperRifle":
                    return 1.1f;
                case "marksmanRifle":
                    if (!weap.WeapFireType.Contains(Weapon.EFireMode.fullauto))
                    {
                        return 1.1f;
                    }
                    return 1f;
                default:
                    return 1;
            }
        }

        public static void DoCantedRecoil(ref Vector3 targetRecoil, ref Vector3 currentRecoil, ref Quaternion weapRotation)
        {
            if (Plugin.IsFiringWiggle)
            {
                float recoilAmount = Plugin.TotalHRecoil / 35f;
                float recoilSpeed = Plugin.TotalConvergence * 0.75f;
                float totalRecoil = Mathf.Lerp(-recoilAmount, recoilAmount, Mathf.PingPong(Time.time * recoilSpeed, 1.0f));
                targetRecoil = new Vector3(0f, totalRecoil, 0f);
            }
            else
            {
                targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, 0.1f);
            }

            currentRecoil = Vector3.Lerp(currentRecoil, targetRecoil, 1f);
            Quaternion recoilQ = Quaternion.Euler(currentRecoil);
            weapRotation *= recoilQ;
        }

        public static void SetRecoilParams(ProceduralWeaponAnimation pwa, Weapon weap, bool isMoving) 
        {
            pwa.HandsContainer.Recoil.Damping = (float)Math.Round(Plugin.RecoilDamping.Value, 3);
            
            if (Plugin.EnableHybridRecoil.Value && (Plugin.HybridForAll.Value || (!Plugin.HybridForAll.Value && !Plugin.HasStock)))
            {
                pwa.HandsContainer.Recoil.ReturnSpeed = Mathf.Clamp((Plugin.TotalConvergence - Mathf.Clamp(25f + Plugin.ShotCount, 0, 100f)) + Mathf.Clamp(15f + Plugin.PlayerControl, 0f, 100f), 2f, Plugin.TotalConvergence);
            }
            else 
            {
                pwa.HandsContainer.Recoil.ReturnSpeed = Plugin.TotalConvergence;
            }
            pwa.HandsContainer.HandsPosition.Damping = (float)Math.Round(Plugin.HandsDamping.Value * (isMoving ? 0.5f : 1f) , 3);
        }
    }
}

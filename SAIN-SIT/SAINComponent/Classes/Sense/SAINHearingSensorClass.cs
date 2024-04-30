using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Interpolation;
using RootMotion.FinalIK;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static RootMotion.FinalIK.AimPoser;

using BotEventHandler = GClass603;

namespace SAIN.SAINComponent.Classes
{
    public class SAINHearingSensorClass : SAINBase, ISAINClass
    {
        public SAINHearingSensorClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            Singleton<BotEventHandler>.Instance.OnSoundPlayed += HearSound;
            Singleton<BotEventHandler>.Instance.OnKill += CleanupKilledPlayer;
        }

        public void Update()
        {
        }

        private float cleanupTimer;

        private void CleanupKilledPlayer(IPlayer killer, IPlayer target)
        {
            if (target != null && SoundsHeardFromPlayer.ContainsKey(target.ProfileId))
            {
                var list = SoundsHeardFromPlayer[target.ProfileId].SoundList;
                if (SAINPlugin.EditorDefaults.DebugHearing)
                {
                    Logger.LogDebug($"Cleaned up {list.Count} sounds from killed player: {target.Profile?.Nickname}");
                }
                list.Clear();
                SoundsHeardFromPlayer.Remove(target.ProfileId);
            }
        }

        private void ManualCleanup(bool force = false)
        {
            if (SoundsHeardFromPlayer.Count == 0)
            {
                return;
            }
            foreach (var kvp in SoundsHeardFromPlayer)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Cleanup(force);
                }
            }
        }

        public void Dispose()
        {
            ManualCleanup(true);
            Singleton<BotEventHandler>.Instance.OnSoundPlayed -= HearSound;
            Singleton<BotEventHandler>.Instance.OnKill -= CleanupKilledPlayer;
        }

        public void HearSound(IPlayer iPlayer, Vector3 position, float power, AISoundType type)
        {
            if (BotOwner == null || SAIN == null)
            {
                return;
            }
            if (iPlayer != null && BotOwner.ProfileId == iPlayer.ProfileId)
            {
                return;
            }

            const float freq = 1f;
            if (cleanupTimer < Time.time)
            {
                cleanupTimer = Time.time + freq;
                ManualCleanup();
            }

            if (!SAIN.GameIsEnding)
            {
                EnemySoundHeard(iPlayer, position, power, type);
            }
        }

        private readonly Dictionary<string, SAINSoundCollection> SoundsHeardFromPlayer = new Dictionary<string, SAINSoundCollection>();

        private void EnemySoundHeard(IPlayer iPlayer, Vector3 soundPosition, float power, AISoundType type)
        {
            bool wasHeard = ProcessSound(iPlayer, soundPosition, power, type, out float distance);

            if (IsPlayerFriendly(iPlayer))
            {
                if (wasHeard && type != AISoundType.step)
                {
                    try { BotOwner.BotsGroup.LastSoundsController.AddNeutralSound(iPlayer, soundPosition); }
                    catch { /* empty because bsgs code is bad */ }
                }
                return;
            }

            bool bulletFelt = BulletFelt(iPlayer, type, soundPosition);

            if (iPlayer == null)
            {
                ReactToSound(null, soundPosition, power, wasHeard, bulletFelt, type);
                return;
            }

            if (wasHeard || bulletFelt)
            {
                string profileID = iPlayer.ProfileId;

                if (!SoundsHeardFromPlayer.ContainsKey(profileID))
                {
                    SoundsHeardFromPlayer.Add(profileID, new SAINSoundCollection(iPlayer));
                }

                SAINSound sainSound = new SAINSound
                {
                    Position = soundPosition,
                    SourcePlayerProfileId = profileID,
                    SoundPower = power,
                    WasHeard = wasHeard,
                    BulletFelt = bulletFelt,
                    DistanceAtCreation = distance,
                    AISoundType = type,
                };

                SAINSoundCollection collection = SoundsHeardFromPlayer[profileID];
                collection.SoundList.Add(sainSound);

                ReactToSound(iPlayer, soundPosition, distance, wasHeard, bulletFelt, type);
            }
        }

        private bool IsPlayerFriendly(IPlayer iPlayer)
        {
            if (iPlayer != null)
            {
                // Checks if the player is not an active enemy and that they are a neutral party
                if (!BotOwner.BotsGroup.IsPlayerEnemy(iPlayer)
                    && BotOwner.BotsGroup.Neutrals.ContainsKey(iPlayer))
                {
                    return true;
                }
                // Double check that the source isn't from a member of the bot's group.
                if (iPlayer.AIData.IsAI
                    && BotOwner.BotsGroup.Contains(iPlayer.AIData.BotOwner))
                {
                    return true;
                }
                // Check that the source isn't an ally
                if (BotOwner.BotsGroup.Allies.Contains(iPlayer))
                {
                    return true;
                }
                // Checks if the player is an enemy by their role.
                if (!BotOwner.BotsGroup.IsPlayerEnemy(iPlayer))
                {
                    return true;
                }
                // One Final Check because my brain is bad.
                if (!BotOwner.EnemiesController.EnemyInfos.ContainsKey(iPlayer))
                {
                    //return true;
                }
            }
            return false;
        }

        private bool ProcessSound(IPlayer player, Vector3 position, float power, AISoundType type, out float distance)
        {
            float range = power;

            var globalHearing = GlobalSettings.Hearing;
            bool isFootstep = type == AISoundType.step;
            if (isFootstep)
            {
                range *= globalHearing.FootstepAudioMultiplier;
            }
            else
            {
                range *= globalHearing.GunshotAudioMultiplier;
            }
            range = Mathf.Round(range * 10) / 10;

            bool wasHeard = DoIHearSound(player, position, range, type, out distance, true);

            if (wasHeard && isFootstep)
            {
                if (!SAIN.Equipment.HasEarPiece)
                {
                    range *= 0.6f;
                }
                if (SAIN.Equipment.HasHeavyHelmet)
                {
                    range *= 0.75f;
                }
                if (SAIN.Memory.HealthStatus == ETagStatus.Dying)
                {
                    range *= 0.75f;
                }
                if (Player.IsSprintEnabled)
                {
                    range *= 0.66f;
                }

                range *= SAIN.Info.FileSettings.Core.HearingSense;

                range = Mathf.Round(range * 10f) / 10f;

                range = Mathf.Clamp(range, 0, power);

                return DoIHearSound(player, position, range, type, out distance, false);
            }

            return false;
        }

        private bool GetShotDirection(IPlayer person, out Vector3 result)
        {
            Player player = EFTInfo.GetPlayer(person);
            if (player != null)
            {
                result = player.LookDirection;
                return true;
            }
            result = Vector3.zero;
            return false;
        }

        private bool BulletFelt(IPlayer person, AISoundType type, Vector3 pos)
        {
            const float bulletfeeldistance = 500f;
            const float DotThreshold = 0.85f;

            bool isGunSound = type == AISoundType.gun || type == AISoundType.silencedGun;

            Vector3 shooterDirectionToBot = BotOwner.Transform.position - pos;
            float shooterDistance = shooterDirectionToBot.magnitude;

            if (isGunSound && shooterDistance <= bulletfeeldistance && GetShotDirection(person, out Vector3 shotDirection))
            {
                Vector3 shotDirectionNormal = shotDirection.normalized;
                Vector3 botDirectionNormal = shooterDirectionToBot.normalized;

                float dot = Vector3.Dot(shotDirectionNormal, botDirectionNormal);

                if (dot >= DotThreshold)
                {
                    if (SAINPlugin.EditorDefaults.DebugHearing)
                    {
                        Logger.LogInfo($"Got Shot Direction from [{person.Profile?.Nickname}] to [{BotOwner?.name}] : Dot Product Result: [{dot}]");

                        Vector3 start = person.MainParts[BodyPartType.head].Position;
                        DebugGizmos.Ray(start, shotDirectionNormal, Color.red, 3f, 0.05f, true, 3f);
                        DebugGizmos.Ray(start, botDirectionNormal, Color.yellow, 3f, 0.05f, true, 3f);
                    }
                    return true;
                }
            }
            return false;
        }

        private void ReactToSound(IPlayer person, Vector3 pos, float power, bool wasHeard, bool bulletFelt, AISoundType type)
        {
            bool isGunSound = type == AISoundType.gun || type == AISoundType.silencedGun;
            float shooterDistance = (BotOwner.Transform.position - pos).magnitude;

            Vector3 vector = GetSoundDispersion(person, pos, type);

            if ((wasHeard || bulletFelt) && shooterDistance < BotOwner.Settings.FileSettings.Hearing.RESET_TIMER_DIST)
            {
                BotOwner.LookData.ResetUpdateTime();
            }

            bool firedAtMe = false;

            if (person != null && bulletFelt && isGunSound)
            {
                Vector3 to = vector + person.LookDirection;
                firedAtMe = DidShotFlyByMe(vector, to, 10f);

                if (firedAtMe)
                {
                    SAIN?.Suppression?.AddSuppression();
                    SAIN.Memory.UnderFireFromPosition = vector;
                    try
                    {
                        BotOwner.Memory.SetUnderFire(person);
                    }
                    catch { }

                    SAIN.EnemyController.CheckAddEnemy(person)?.SetEnemyAsSniper(shooterDistance > 100f);
                }
            }

            if (wasHeard)
            {
                try
                {
                    SAIN.Squad.SquadInfo.AddPointToSearch(vector, power, BotOwner, type, pos, person);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
                CheckCalcGoal();
            }
            else if (isGunSound && bulletFelt)
            {
                Vector3 estimate = GetEstimatedPoint(vector);
                SAIN.Memory.UnderFireFromPosition = estimate;

                try
                {
                    SAIN.Squad.SquadInfo.AddPointToSearch(vector, power, BotOwner, type, pos, person);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
                CheckCalcGoal();
            }
        }

        private Vector3 GetSoundDispersion(IPlayer person, Vector3 pos, AISoundType soundType)
        {
            const float dispGun = 15f;
            const float dispSuppGun = 10;
            const float dispStep = 5;

            Vector3 shooterDirectionToBot = BotOwner.Transform.position - pos;
            float shooterDistance = shooterDirectionToBot.magnitude;

            float dispersion;
            if (soundType == AISoundType.gun)
            {
                dispersion = shooterDistance / dispGun;
            }
            else if (soundType == AISoundType.silencedGun)
            {
                dispersion = shooterDistance / dispSuppGun;
            }
            else
            {
                dispersion = shooterDistance / dispStep;
            }

            // If a bot is hearing multiple sounds from a iPlayer, they will now be more accurate at finding the source soundPosition based on how many sounds they have heard from this particular iPlayer
            int soundCount = 0;
            float finalDispersion = dispersion;
            if (person != null && SoundsHeardFromPlayer.ContainsKey(person.ProfileId))
            {
                SAINSoundCollection collection = SoundsHeardFromPlayer[person.ProfileId];
                if (collection != null)
                {
                    soundCount = collection.Count;
                }
            }
            if (soundCount > 1)
            {
                finalDispersion = (dispersion / soundCount).Round100();
            }

            float dispersionRandomized = EFTMath.Random(-finalDispersion, finalDispersion);
            Vector3 vector = new Vector3(pos.x + dispersionRandomized, pos.y, pos.z + dispersionRandomized);

            if (SAINPlugin.EditorDefaults.DebugHearing)
            {
                Logger.LogDebug($"Dispersion: [{(vector - pos).magnitude}] :: Distance: [{shooterDistance}] : Sounds Heard: [{soundCount}] : PreCount Dispersion Num: [{dispersion}] : PreRandomized Dispersion Result: [{finalDispersion}] : SoundType: [{soundType}]");
            }
            return vector;
        }

        private void CheckCalcGoal()
        {
            if (!BotOwner.Memory.GoalTarget.HavePlaceTarget() && BotOwner.Memory.GoalEnemy == null)
            {
                try
                {
                    BotOwner.BotsGroup.CalcGoalForBot(BotOwner);
                }
                catch { }
            }
        }

        private Vector3 GetEstimatedPoint(Vector3 source)
        {
            Vector3 randomPoint = UnityEngine.Random.onUnitSphere * (Vector3.Distance(source, BotOwner.Transform.position) / 10f);
            randomPoint.y = Mathf.Clamp(randomPoint.y, -5f, 5f);
            return source + randomPoint;
        }

        private bool DidShotFlyByMe(Vector3 from, Vector3 to, float maxDist)
        {
            Vector3 projectionPoint = GetProjectionPoint(BotOwner.Position + Vector3.up, from, to);
            Vector3 direction = projectionPoint - from;

            bool firedAtMe = false;
            if ((projectionPoint - BotOwner.Position).sqrMagnitude < maxDist * maxDist)
            {
                firedAtMe = !Physics.Raycast(from, direction, direction.magnitude * 0.95f, LayerMaskClass.HighPolyWithTerrainMask);
            }

            if (SAINPlugin.DebugMode && SAINPlugin.EditorDefaults.DebugDrawProjectionPoints)
            {
                if (firedAtMe)
                {
                    DebugGizmos.Sphere(projectionPoint, 0.1f, Color.red, true, 3f);
                    DebugGizmos.Ray(projectionPoint, from - projectionPoint, Color.red, 5f, 0.05f, true, 3f, true);
                }
                else
                {
                    DebugGizmos.Sphere(projectionPoint, 0.1f, Color.white, true, 3f);
                    DebugGizmos.Ray(projectionPoint, from - projectionPoint, Color.white, 5f, 0.05f, true, 3f, true);
                }
            }

            return firedAtMe;
        }

        public static Vector3 GetProjectionPoint(Vector3 p, Vector3 p1, Vector3 p2)
        {
            float num = p1.z - p2.z;

            if (num == 0f)
            {
                return new Vector3(p.x, p1.y, p1.z);
            }

            float num2 = p2.x - p1.x;

            if (num2 == 0f)
            {
                return new Vector3(p1.x, p1.y, p.z);
            }

            float num3 = p1.x * p2.z - p2.x * p1.z;
            float num4 = num2 * p.x - num * p.z;
            float num5 = -(num2 * num3 + num * num4) / (num2 * num2 + num * num);

            return new Vector3(-(num3 + num2 * num5) / num, p1.y, num5);
        }

        private bool CheckFootStepDetectChance(float d)
        {
            float closehearing = 10f;
            float farhearing = SAIN.Info.FileSettings.Hearing.MaxFootstepAudioDistance;

            if (d <= closehearing)
            {
                return true;
            }

            if (d > farhearing)
            {
                return false;
            }

            float num = farhearing - closehearing;
            float num2 = d - closehearing;
            float num3 = 1f - num2 / num;

            return EFTMath.Random(0f, 1f) < num3;
        }

        public bool DoIHearSound(IPlayer iPlayer, Vector3 position, float power, AISoundType type, out float soundDistance, bool withOcclusionCheck)
        {
            soundDistance = (BotOwner.Transform.position - position).magnitude;

            // Is sound within hearing Distance at all?
            if (soundDistance < power)
            {
                if (!withOcclusionCheck)
                {
                    if (type == AISoundType.step)
                    {
                        return CheckFootStepDetectChance(soundDistance);
                    }
                    return true;
                }
                // if so, is sound blocked by obstacles?
                float occludedPower = GetOcclusion(iPlayer, position, power, type);
                if (soundDistance < occludedPower)
                {
                    if (type == AISoundType.step)
                    {
                        return CheckFootStepDetectChance(soundDistance);
                    }
                    return true;
                }
            }

            // Sound not heard
            soundDistance = 0f;
            return false;
        }

        private float GetOcclusion(IPlayer player, Vector3 position, float power, AISoundType type)
        {
            Vector3 botheadpos = BotOwner.MyHead.position;

            if (type == AISoundType.step)
            {
                position.y += 0.1f;
            }

            Vector3 direction = (botheadpos - position).normalized;
            float soundDistance = direction.magnitude;

            float result = power;
            // Checks if something is within line of sight
            if (Physics.Raycast(botheadpos, direction, power, LayerMaskClass.HighPolyWithTerrainNoGrassMask))
            {
                if (player == null)
                {
                    result *= 0.8f;
                }
                // If the sound source is the iPlayer, raycast and find number of collisions
                else if (player.IsYourPlayer)
                {
                    // Check if the sound originates from an environment other than the BotOwner's
                    float environmentmodifier = EnvironmentCheck(player);

                    // Raycast check
                    float finalmodifier = RaycastCheck(botheadpos, position, environmentmodifier);

                    // Reduce occlusion for unsuppressed guns
                    if (type == AISoundType.gun) finalmodifier = Mathf.Sqrt(finalmodifier);

                    // Apply Modifier
                    result = power * finalmodifier;
                }
                else
                {
                    // Only check environment for bots vs bots
                    result = power * EnvironmentCheck(player);
                }
            }
            return result;
        }

        private float EnvironmentCheck(IPlayer enemy)
        {
            int botlocation = BotOwner.AIData.EnvironmentId;
            int enemylocation = enemy.AIData.EnvironmentId;
            return botlocation == enemylocation ? 1f : 0.5f;
        }

        private float RaycastCheck(Vector3 botpos, Vector3 enemypos, float environmentmodifier)
        {
            if (raycasttimer < Time.time)
            {
                raycasttimer = Time.time + 0.25f;

                occlusionmodifier = 1f;

                LayerMask mask = LayerMaskClass.HighPolyWithTerrainNoGrassMask;

                // AddColor a RaycastHit array and set it to the Physics.RaycastAll
                var direction = botpos - enemypos;
                RaycastHit[] hits = Physics.RaycastAll(enemypos, direction, direction.magnitude, mask);

                int hitCount = 0;

                // Loop through each hit in the hits array
                for (int i = 0; i < hits.Length; i++)
                {
                    // Check if the hitCount is 0
                    if (hitCount == 0)
                    {
                        // If the hitCount is 0, set the occlusionmodifier to 0.8f multiplied by the environmentmodifier
                        occlusionmodifier *= 0.85f;
                    }
                    else
                    {
                        // If the hitCount is not 0, set the occlusionmodifier to 0.95f multiplied by the environmentmodifier
                        occlusionmodifier *= 0.95f;
                    }

                    // Increment the hitCount
                    hitCount++;
                }
                occlusionmodifier *= environmentmodifier;
            }

            return occlusionmodifier;
        }

        private float occlusionmodifier = 1f;

        private float raycasttimer = 0f;

        public delegate void GDelegate4(Vector3 vector, float bulletDistance, AISoundType type);

        public event GDelegate4 OnEnemySounHearded;
    }
}
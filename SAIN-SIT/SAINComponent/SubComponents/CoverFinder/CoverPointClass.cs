using EFT;
using EFT.Game.Spawning;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverPointController : MonoBehaviour
    {
        public CoverPointComponent AddCoverPoint(Collider collider, SAINComponentClass sain, Vector3 point, NavMeshPath path)
        {
            if (collider == null) return null;
            CoverPointComponent coverPoint = collider.gameObject.GetOrAddComponent<CoverPointComponent>();
            coverPoint?.AddOrUpdateCoverPoint(sain, point, path);
            return coverPoint;
        }

        public Dictionary<Collider, CoverPointComponent> WorldCoverPoints = new Dictionary<Collider, CoverPointComponent>();
    }


    public class CoverPointComponent : MonoBehaviour
    {
        public void Awake()
        {
            Collider = this.GetComponent<Collider>();
        }

        public CoverPointNew AddOrUpdateCoverPoint(SAINComponentClass sain, Vector3 point, NavMeshPath pathToPoint)
        {
            if (!CoverPoints.ContainsKey(sain.ProfileId))
            {
                sain.OnSAINDisposed += RemoveCoverPoint;
                CoverPoints.Add(sain.ProfileId, new CoverPointNew(sain, point, Collider, pathToPoint));
            }
            else
            {
                CoverPoints[sain.ProfileId].Position = point;
            }
            if (!_initialized)
            {
                _initialized = true;
            }
            return CoverPoints[sain.ProfileId];
        }

        private void RemoveCoverPoint(string profileId, BotOwner bot)
        {
            if (CoverPoints.ContainsKey(profileId))
            {
                CoverPointNew coverPoint = CoverPoints[profileId];
                if (coverPoint?.SAIN != null)
                {
                    coverPoint.SAIN.OnSAINDisposed -= RemoveCoverPoint;
                }
                CoverPoints.Remove(profileId);
            }
        }

        public void Update()
        {
            if (!_initialized)
            {
                return;
            }

            // If this component has had no coverpoints cached for bots for 10 seconds, destroy the component.
            if (CoverPoints.Count == 0)
            {
                if (_timeDictEmpty == 0)
                {
                    _timeDictEmpty = Time.time + 10f;
                }
                if (_timeDictEmpty < Time.time)
                {
                    Dispose();
                    return;
                }
            }
            else if (_timeDictEmpty != 0)
            {
                _timeDictEmpty = 0;
            }
        }

        private float _timeDictEmpty;

        public void Dispose()
        {
            CoverPoints.Clear();
            Destroy(this);
        }

        public readonly Dictionary<string, CoverPointNew> CoverPoints = new Dictionary<string, CoverPointNew> ();

        public Collider Collider { get; private set; }
        private bool _initialized = false;
    }

    public sealed class CoverPointNew
    {
        public readonly SAINComponentClass SAIN;
        public readonly Collider Collider;

        public Vector3 Position 
        { 
            get 
            {
                return _position;
            }
            set 
            {
                TimeUpdated = Time.time;
                _position = value;
            }
        }

        private Vector3 _position;

        public float TimeSinceUpdated => Time.time - TimeUpdated;
        public float TimeUpdated { get; private set; }

        public NavMeshPath PathToCover;
        public readonly float CoverValue;
        public readonly float CoverHeight;
        public float PathLengthToCover;
        public readonly float TimeCreated;

        public CoverPointNew(SAINComponentClass sain, Vector3 point, Collider collider, NavMeshPath path)
        {
            SAIN = sain;
            Position = point;
            Collider = collider;
            PathToCover = path;
            TimeCreated = Time.time;

            Vector3 size = collider.bounds.size;
            CoverHeight = size.y;
            CoverValue = (size.x + size.y + size.z).Round10();
        }
    }

    public class CoverPoint
    {
        public readonly Dictionary<string, SAINBotCoverInfo> BotCoverInfos = new Dictionary<string, SAINBotCoverInfo>();

        public CoverPoint(SAINComponentClass sain, Vector3 point, Collider collider, NavMeshPath pathToPoint)
        {
            TimeCreated = Time.time;
            TimeLastUpdated = TimeCreated;

            Collider = collider;
            Vector3 size = collider.bounds.size;
            CoverHeight = size.y;
            CoverValue = (size.x + size.y + size.z).Round10();

            var info = GetInfo(sain);
            SetPosition(info, point);


            PathToPoint = pathToPoint;
        }

        public float TimeLastUpdated;

        public void UpdateInfo(SAINComponentClass sain)
        {
            var info = GetInfo(sain);
        }

        public SAINBotCoverInfo GetInfo(SAINComponentClass sain)
        {
            if (!BotCoverInfos.ContainsKey(sain.ProfileId))
            {
                BotCoverInfos.Add(sain.ProfileId, new SAINBotCoverInfo(sain));
            }
            return BotCoverInfos[sain.ProfileId];
        }

        public bool IsBad;

        public float CoverValue { get; private set; }

        public NavMeshPath PathToPoint { get; private set; }

        public void ResetHitInCoverCount(SAINBotCoverInfo info)
        {
            info.HitInCoverUnknownCount = 0;
            info.HitInCoverCantSeeCount = 0;
            info.HitInCoverCount = 0;
        }

        public void GetHit(SAINComponentClass sain, int hitUnknown, int hitCantSee, int hitInCover)
        {
            var info = GetInfo(sain);
            GetHit(info, hitUnknown, hitCantSee, hitInCover);
        }

        public void GetHit(SAINBotCoverInfo info, int hitUnknown, int hitCantSee, int hitInCover)
        {
            info.HitInCoverCantSeeCount += hitCantSee;
            info.HitInCoverCount += hitInCover;
            info.HitInCoverUnknownCount += hitUnknown;
        }

        public bool IsSafePath(SAINComponentClass sain)
        {
            return GetInfo(sain).IsSafePath;
        }

        public void SetIsSafePath(bool value, SAINComponentClass sain)
        {
            GetInfo(sain).IsSafePath = value;
        }

        public float CoverHeight { get; private set; }

        public bool CheckPathSafety(SAINComponentClass sain)
        {
            var info = GetInfo(sain);
            return CheckPathSafety(info);
        }

        public bool CheckPathSafety(SAINBotCoverInfo info)
        {
            if (info.nextPathSafetyTime < Time.time)
            {
                info.nextPathSafetyTime = Time.time + 3f;

                Vector3 target;
                if (info.SAIN.HasEnemy)
                {
                    target = info.SAIN.Enemy.EnemyHeadPosition;
                }
                else if (info.SAIN.CurrentTargetPosition != null)
                {
                    target = info.SAIN.CurrentTargetPosition.Value;
                }
                else
                {
                    info.IsSafePath = true;
                    return true;
                }

                CalcPathLength(info);
                info.IsSafePath = SAINBotSpaceAwareness.CheckPathSafety(PathToPoint, target);
            }
            return info.IsSafePath;
        }

        private bool botIsUsingThis;

        public bool GetBotIsUsingThis()
        {
            return botIsUsingThis;
        }

        public void SetBotIsUsingThis(bool value)
        {
            botIsUsingThis = value;
        }

        public bool BotInThisCover(SAINComponentClass sain)
        {
            var info = GetInfo(sain);
            return BotInThisCover(info);
        }

        public bool BotInThisCover(SAINBotCoverInfo info)
        {
            return GetCoverStatus(info) == CoverStatus.InCover;
        }

        public bool GetSpotted(SAINComponentClass sain)
        {
            var info = GetInfo(sain);
            return BotSpotted(info);
        }

        public bool BotSpotted(SAINBotCoverInfo info)
        {
            return info.HitInCoverCount > 2 || info.HitInCoverUnknownCount > 0 || info.HitInCoverCantSeeCount > 1;
        }

        public bool PointIsVisible(SAINComponentClass sain)
        {
            var info = GetInfo(sain);
            return PointIsVisible(info);
        }

        public bool PointIsVisible(SAINBotCoverInfo info)
        {
            if (info.nextCheckVisTime < Time.time)
            {
                info.nextCheckVisTime = Time.time + 0.5f;

                info.PointIsVisible = false;
                if (info.SAIN.Enemy != null)
                {
                    Vector3 coverPos = info.Position;
                    coverPos += Vector3.up * 0.5f;
                    Vector3 start = info.SAIN.Enemy.EnemyHeadPosition;
                    Vector3 direction = coverPos - start;
                    info.PointIsVisible = !Physics.Raycast(start, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);
                }
            }
            return info.PointIsVisible;
        }

        public float CalcPathLength(SAINComponentClass sain)
        {
            var info = GetInfo(sain);
            return CalcPathLength(info);
        }

        public float CalcPathLength(SAINBotCoverInfo info)
        {
            if (info.nextCalcPathTime < Time.time)
            {
                info.nextCalcPathTime = Time.time + 2;

                PathToPoint.ClearCorners();
                if (NavMesh.CalculatePath(info.SAIN.Position, info.Position, -1, PathToPoint))
                {
                    info.pathLength = PathToPoint.CalculatePathLength();
                }
                else
                {
                    info.pathLength = float.MaxValue;
                }
            }
            return info.pathLength;
        }

        public float GetPathLength(SAINComponentClass sain)
        {
            return GetInfo(sain).pathLength;
        }

        public CoverStatus GetCoverStatus(SAINComponentClass sain)
        {
            var info = GetInfo(sain);
            return GetCoverStatus(info);
        }

        public CoverStatus GetCoverStatus(SAINBotCoverInfo info)
        {
            if (info.nextCheckStatusTime < Time.time)
            {
                info.nextCheckStatusTime = Time.time + 0.25f;
                float sqrMagnitude = (info.SAIN.Position - info.Position).sqrMagnitude;
                info.SqrMagnitude = sqrMagnitude;

                CoverStatus status;
                if (info.LastStatus == CoverStatus.InCover && sqrMagnitude <= InCoverStayDist * InCoverStayDist)
                {
                    status = CoverStatus.InCover;
                }
                else if (sqrMagnitude <= InCoverDist * InCoverDist)
                {
                    status = CoverStatus.InCover;
                }
                else if (sqrMagnitude <= CloseCoverDist * CloseCoverDist)
                {
                    status = CoverStatus.CloseToCover;
                }
                else if (sqrMagnitude <= MidCoverDist * MidCoverDist)
                {
                    status = CoverStatus.MidRangeToCover;
                }
                else
                {
                    status = CoverStatus.FarFromCover;
                }

                info.LastStatus = info.Status;
                info.Status = status;
            }
            return info.Status;
        }

        private const float InCoverDist = 0.75f;
        private const float InCoverStayDist = 1f;
        private const float CloseCoverDist = 8f;
        private const float MidCoverDist = 20f;

        public Collider Collider { get; private set; }

        public Vector3 GetPosition(SAINComponentClass sain)
        {
            return GetInfo(sain).Position;
        }

        public void SetPosition(SAINComponentClass sain, Vector3 value)
        {
            GetInfo(sain).Position = value;
        }

        public Vector3 GetPosition(SAINBotCoverInfo info)
        {
            return info.Position;
        }

        public void SetPosition(SAINBotCoverInfo info, Vector3 value)
        {
            TimeLastUpdated = Time.time;
            info.Position = value;
        }

        public float TimeCreated { get; private set; }

    }

    public sealed class SAINBotCoverInfo
    {
        public SAINBotCoverInfo(SAINComponentClass sain)
        {
            SAIN = sain;
        }
        public readonly SAINComponentClass SAIN;

        public bool BotIsUsingThis = false;
        public float TimeLastUsed = 0f;
        public Vector3 Position = Vector3.zero;
        public float nextCalcPathTime = 0f;
        public float pathLength = float.MaxValue;
        public float nextPathSafetyTime = 0f;
        public CoverStatus Status = CoverStatus.None;
        public CoverStatus LastStatus = CoverStatus.None;
        public bool PointIsVisible = false;
        public float nextCheckVisTime = 0f;
        public bool Spotted = false;
        public int HitInCoverUnknownCount = 0;
        public int HitInCoverCantSeeCount = 0;
        public int HitInCoverCount = 0;
        public bool IsSafePath = false;
        public float SqrMagnitude = float.MaxValue;
        public float nextCheckStatusTime = 0f;
    }
}
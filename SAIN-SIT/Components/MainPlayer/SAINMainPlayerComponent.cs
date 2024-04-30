using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.BaseClasses;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Mover;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components
{
    public class SAINMainPlayerComponent : MonoBehaviour
    {
        public void Awake()
        {
            if (SAINPersonComponent.TryAddSAINPersonToPlayer(MainPlayer, out var component))
            {
                SAINPersonComponent = component;
                SAINPerson = component.SAINPerson;
            }
            MainPlayerLight = MainPlayer.GetOrAddComponent<SAINFlashLightComponent>();
            CamoClass = new SAINCamoClass(this);
        }

        public SAINCamoClass CamoClass { get; private set; }
        public SAINPersonClass SAINPerson { get; private set; }
        public SAINPersonComponent SAINPersonComponent { get; private set; }
        public SAINFlashLightComponent MainPlayerLight { get; private set; }

        private void Start()
        {
            CamoClass.Start();
            //NavMesh.SetAreaCost(1, 69);
            //NavMesh.SetAreaCost(2, 6969);
            //NavMesh.SetAreaCost(3, 696969);
        }

        private void Update()
        {
            if (MainPlayer == null)
            {
                Dispose();
                return;
            }
            if (debugtimer < Time.time)
            {
                debugtimer = Time.time + 1f;
                float speedRatio = MainPlayer.MovementContext.ClampedSpeed / MainPlayer.MovementContext.MaxSpeed;
                //Logger.LogDebug(MainPlayer.MovementContext.ClampedSpeed);
            }

            //Logger.LogDebug(NavMesh.GetAreaCost(0));
            //Logger.LogDebug(NavMesh.GetAreaCost(1));
            //Logger.LogDebug(NavMesh.GetAreaCost(2));
            //Logger.LogDebug(NavMesh.GetAreaCost(3));

            //FindPlacesToShoot(PlacesToShootMe);
        }

        public readonly List<Vector3> PlacesToShootMe = new List<Vector3>();

        public sealed class FindPlacesToShootParameters
        {
            public float minPointDist = 5f;
            public float maxPointDist = 300f;
            public int iterationMax = 100;
            public int successMax = 5;
            public float yVal = 0.25f;
            public float navSampleRange = 0.25f;
            public float downDirDist = 10f;
        }

        public IEnumerator FindPlaceToShoot(FindPlacesToShootParameters parameters)
        {
            yield return null;
        }

        public void FindPlacesToShoot(List<Vector3> places, Vector3 directionToBot, FindPlacesToShootParameters parameters)
        {
            float minPointDist = parameters.minPointDist;
            float maxPointDist = parameters.maxPointDist;
            int iterationMax = parameters.iterationMax;
            int successMax = parameters.successMax;
            float yVal = parameters.yVal;
            float navSampleRange = parameters.navSampleRange;
            float downDirDist = parameters.downDirDist;

            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;

            int successCount = 0;
            places.Clear();
            for (int i = 0; i < iterationMax; i++)
            {
                Vector3 start = SAINPerson.Transform.Head;
                Vector3 randomDirection = UnityEngine.Random.onUnitSphere;
                randomDirection.y = UnityEngine.Random.Range(-yVal, yVal);
                float distance = UnityEngine.Random.Range(minPointDist, maxPointDist);

                if (!Physics.Raycast(start, randomDirection, distance, mask))
                {
                    Vector3 openPoint = start + randomDirection * distance;

                    if (Physics.Raycast(openPoint, Vector3.down, out var rayHit2, downDirDist, mask)
                        && (rayHit2.point - start).sqrMagnitude > minPointDist * minPointDist
                        && NavMesh.SamplePosition(rayHit2.point, out var navHit2, navSampleRange, -1))
                    {
                        DebugGizmos.Sphere(navHit2.position, 0.1f, Color.blue, true, 3f);
                        DebugGizmos.Line(navHit2.position, start, 0.025f, Time.deltaTime, true);
                        places.Add(navHit2.position);
                        successCount++;
                    }
                }
                if (successCount >= successMax)
                {
                    break;
                }
            }
        }

        private float debugtimer;

        private void OnDestroy()
        {
            CamoClass.OnDestroy();
        }

        private void OnGUI()
        {
            //CamoClass.OnGUI();
        }

        public RaycastHit CurrentHit { get; private set; }
        public RaycastHit LastHit { get; private set; }

        private void CheckPlayerLook()
        {

        }

        private void Dispose()
        {
            try
            {
                ComponentHelpers.DestroyComponent(SAINPersonComponent);
                ComponentHelpers.DestroyComponent(MainPlayerLight);
                Destroy(this);
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose Component Error: [{e}]");
            }
        }

        public Player MainPlayer => Singleton<GameWorld>.Instance?.MainPlayer;
    }
}
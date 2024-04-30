using UnityEngine;
using System;
using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using static UnityEngine.UI.Image;
using UnityEngine.UI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class ColliderFinder
    {
        public ColliderFinder(CoverFinderComponent component)
        {
            CoverFinderComponent = component;
        }

        private CoverFinderComponent CoverFinderComponent;
        private Vector3 OriginPoint => CoverFinderComponent.OriginPoint;
        private Vector3 TargetPoint => CoverFinderComponent.TargetPoint;

        public void GetNewColliders(out int hits, Collider[] array, int iterationMax = 10, float startRadius = 2f, int hitThreshold = 100, LayerMask colliderMask = default)
        {
            const float StartBoxHeight = 0.25f;
            const float HeightIncreasePerIncrement = 1f;
            const float HeightDecreasePerIncrement = 1f;
            const float LengthIncreasePerIncrement = 3f;

            ClearColliders(array);

            if (colliderMask == default)
            {
                colliderMask = LayerMaskClass.HighPolyWithTerrainMask;
            }

            float boxLength = startRadius;
            float boxHeight = StartBoxHeight;

            var orientation = Quaternion.identity;
            Vector3 boxOrigin = OriginPoint + Vector3.up * StartBoxHeight;

            for (int i = 0; i < debugObjects.Count; i++)
            {
                GameObject.Destroy(debugObjects[i]);
            }
            debugObjects.Clear();

            hits = 0;
            for (int i = 0; i < iterationMax; i++)
            {
                Vector3 box = new Vector3(boxLength, boxHeight, boxLength);
                int rawHits = Physics.OverlapBoxNonAlloc(boxOrigin, box, array, orientation, colliderMask);
                hits = FilterColliders(array, rawHits);

                if (hits > hitThreshold)
                {
                    //debugObjects.Add(DebugGizmos.Box(boxOrigin, boxLength, boxHeight, Color.red));
                    break;
                }
                else
                {
                    //debugObjects.Add(DebugGizmos.Box(boxOrigin, boxLength, boxHeight, Color.white));
                    boxOrigin += Vector3.down * HeightDecreasePerIncrement;
                    boxHeight += HeightIncreasePerIncrement + HeightDecreasePerIncrement;
                    boxLength += LengthIncreasePerIncrement;
                    continue;
                }
            }

            for (int i = 0; i < hits; i++)
            {
                Collider collider = array[i];
                if (collider != null)
                {
                    //debugObjects.Add(DebugGizmos.Line(OriginPoint + Vector3.up, collider.transform.position, DebugGizmos.RandomColor, 0.01f, false, -1));
                }
            }
        }

        private List<GameObject> debugObjects = new List<GameObject>();

        private void ClearColliders(Collider[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = null;
            }
        }

        /// <summary>
        /// Sorts an array of Colliders based on their Distance from bot's Position. 
        /// </summary>
        /// <param value="array">The array of Colliders to be sorted.</param>
        public void SortArrayBotDist(Collider[] array)
        {
            Array.Sort(array, ColliderArrayBotDistComparer);
        }

        private int FilterColliders(Collider[] array, int hits)
        {
            float minHeight = CoverFinderComponent.CoverMinHeight;
            const float minX = 0.1f;
            const float minZ = 0.1f;

            int hitReduction = 0;
            for (int i = 0; i < hits; i++)
            {
                Vector3 size = array[i].bounds.size;
                if (size.y < CoverFinderComponent.CoverMinHeight
                    || size.x < minX && size.z < minZ 
                    || ColliderAlreadyUsed(array[i], CoverFinderComponent.CoverPoints))
                {
                    array[i] = null;
                    hitReduction++;
                }
            }
            return hits - hitReduction;
        }

        private bool ColliderAlreadyUsed(Collider collider, List<CoverPoint> coverPoints)
        {
            for (int i = 0; i < coverPoints.Count;i++)
            {
                if (collider == coverPoints[i].Collider)
                {
                    return true;
                }
            }
            return false;
        }

        public int ColliderArrayBotDistComparer(Collider A, Collider B)
        {
            if (A == null && B != null)
            {
                return 1;
            }
            else if (A != null && B == null)
            {
                return -1;
            }
            else if (A == null && B == null)
            {
                return 0;
            }
            else
            {
                float AMag = (OriginPoint - A.transform.position).sqrMagnitude;
                float BMag = (OriginPoint - B.transform.position).sqrMagnitude;
                return AMag.CompareTo(BMag);
            }
        }
    }
}
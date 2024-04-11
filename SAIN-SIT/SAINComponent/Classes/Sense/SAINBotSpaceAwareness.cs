using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotSpaceAwareness
    {
        public static bool CheckPathSafety(NavMeshPath path, Vector3 enemyHeadPos, float ratio = 0.5f)
        {
            if (!SAINPlugin.EditorDefaults.DebugEnablePathTester)
            {
                return true;
            }

            Vector3[] corners = path.corners;
            int max = corners.Length - 1;

            for (int i = 0; i < max - 1; i++)
            {
                Vector3 pointA = corners[i];
                Vector3 pointB = corners[i + 1];

                float ratioResult = RaycastAlongDirection(pointA, pointB, enemyHeadPos);

                if (ratioResult < ratio)
                {
                    return false;
                }
            }

            return true;
        }

        private static float RaycastAlongDirection(Vector3 pointA, Vector3 pointB, Vector3 rayOrigin, int SegmentCount = 5)
        {
            const float RayHeight = 1.1f;
            const float debugExpireTime = 20f;

            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;

            Vector3 direction = pointB - pointA;
            float segmentLength = direction.magnitude / SegmentCount;
            Vector3 dirNormal = direction.normalized;
            Vector3 dirSegment = dirNormal * segmentLength;

            Vector3 testPoint = pointA + (Vector3.up * RayHeight);

            if (SAINPlugin.EditorDefaults.DebugDrawSafePaths)
            {
                DebugGizmos.Sphere(pointA, 0.1f, Color.red, true, debugExpireTime);
                DebugGizmos.Line(pointA, rayOrigin, Color.red, 0.05f, true, debugExpireTime);
                DebugGizmos.Sphere(pointB, 0.1f, Color.red, true, debugExpireTime);
                DebugGizmos.Line(pointB, rayOrigin, Color.red, 0.05f, true, debugExpireTime);
            }

            int i = 0;
            int hits = 0;

            for (i = 0; (i < SegmentCount); i++)
            {
                testPoint += dirSegment;

                Vector3 enemyDir = testPoint - rayOrigin;
                float rayLength = enemyDir.magnitude;

                Color debugColor = Color.red;
                if (Physics.Raycast(rayOrigin, enemyDir, rayLength, mask))
                {
                    debugColor = Color.white;
                    hits++;
                }

                if (SAINPlugin.EditorDefaults.DebugDrawSafePaths)
                {
                    DebugGizmos.Line(rayOrigin, testPoint, debugColor, 0.05f, true, debugExpireTime, true);
                    DebugGizmos.Sphere(testPoint, 0.1f, Color.green, true, debugExpireTime);
                    DebugGizmos.Sphere(rayOrigin, 0.1f, Color.red, true, debugExpireTime);
                }
            }

            float result = hits / i;
            return result;
        }
    }
}

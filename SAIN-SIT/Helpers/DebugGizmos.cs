using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using Color = UnityEngine.Color;

using CameraClass = FPSCamera;

namespace SAIN.Helpers
{
    public sealed class GUIObject
    {
        public Vector3 WorldPos;
        public string Text;
        public GUIStyle Style;
        public StringBuilder StringBuilder = new StringBuilder();
    }

    public class DebugGizmos
    {
        public static void Update()
        {
            if (!DrawGizmos)
            {
                if (DrawnGizmos.Count > 0)
                {
                    for (int i = 0; i < DrawnGizmos.Count; i++)
                    {
                        if (DrawnGizmos[i] != null)
                            Object.Destroy(DrawnGizmos[i]);
                    }
                    DrawnGizmos.Clear();
                }
            }
        }

        public static void OnGUI()
        {
            if (!SAINPlugin.EditorDefaults.DrawDebugLabels)
            {
                GUIObjects.Clear();
            }
            else
            {
                foreach (var obj in GUIObjects)
                {
                    string text = obj.Text.IsNullOrEmpty() ? obj.StringBuilder.ToString() : obj.Text;
                    OnGUIDrawLabel(obj.WorldPos, text, obj.Style);
                }
            }
        }

        private static readonly List<GameObject> DrawnGizmos = new List<GameObject>();

        public static bool DrawGizmos => SAINPlugin.DrawDebugGizmos;

        private static GUIStyle DefaultStyle;

        public static GUIObject CreateLabel(Vector3 worldPos, string text, GUIStyle guiStyle = null)
        {
            GUIObject obj = new GUIObject { WorldPos = worldPos, Text = text, Style = guiStyle };
            AddGUIObject(obj);
            return obj;
        }

        public static void AddGUIObject(GUIObject obj)
        {
            if (GUIObjects.Contains(obj))
            {
                return;
            }
            GUIObjects.Add(obj);
        }

        public static void DestroyLabel(GUIObject obj)
        {
            GUIObjects.Remove(obj);
        }

        public static void OnGUIDrawLabel(Vector3 worldPos, string text, GUIStyle guiStyle = null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0)
            {
                return;
            }

            if (guiStyle == null)
            {
                if (DefaultStyle == null)
                {
                    DefaultStyle = new GUIStyle(GUI.skin.box);
                    DefaultStyle.alignment = TextAnchor.MiddleLeft;
                    DefaultStyle.fontSize = 14;
                    DefaultStyle.margin = new RectOffset(3, 3, 3, 3);
                }
                guiStyle = DefaultStyle;
            }

            GUIContent content = new GUIContent(text);

            float screenScale = GetScreenScale();
            Vector2 guiSize = guiStyle.CalcSize(content);
            float x = (screenPos.x * screenScale) - (guiSize.x / 2);
            float y = Screen.height - ((screenPos.y * screenScale) + guiSize.y);
            Rect rect = new Rect(new Vector2(x, y), guiSize);
            GUI.Label(rect, content);
        }

        private static readonly List<GUIObject> GUIObjects = new List<GUIObject>();

        private static float GetScreenScale()
        {
            if (_nextCheckScreenTime < Time.time && CameraClass.Instance.SSAA.isActiveAndEnabled)
            {
                _nextCheckScreenTime = Time.time + 10f;
                _screenScale = (float)CameraClass.Instance.SSAA.GetOutputWidth() / (float)CameraClass.Instance.SSAA.GetInputWidth();
            }
            return _screenScale;
        }

        private static float _screenScale = 1.0f;
        private static float _nextCheckScreenTime;

        public static GameObject Sphere(Vector3 position, float size, Color color, bool temporary = false, float expiretime = 1f)
        {
            if (!DrawGizmos)
            {
                return null;
            }
            if (!SAINPlugin.DebugMode)
            {
                return null;
            }

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Renderer>().material.color = color;
            sphere.GetComponent<Collider>().enabled = false;
            sphere.transform.position = new Vector3(position.x, position.y, position.z); ;
            sphere.transform.localScale = new Vector3(size, size, size);

            AddGizmo(sphere, expiretime);

            return sphere;
        }

        public static GameObject Box(Vector3 position, float length, float height, Color color, float expiretime = -1f)
        {
            if (!DrawGizmos)
            {
                return null;
            }
            if (!SAINPlugin.DebugMode)
            {
                return null;
            }

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);

            box.GetComponent<Renderer>().material.color = color;
            box.GetComponent<Collider>().enabled = false;
            box.transform.position = position;
            box.transform.localScale = new Vector3(length * 2, height * 2, length * 2);
            AddGizmo(box, expiretime);

            return box;
        }

        private static void AddGizmo(GameObject obj, float expireTime)
        {
            if (expireTime > 0)
            {
                TempCoroutine.DestroyAfterDelay(obj, expireTime);
            }
            else
            {
                DrawnGizmos.Add(obj);
            }
        }

        public static GameObject Sphere(Vector3 position, float size, float expiretime = 1f)
        {
            return Sphere(position, size, RandomColor, expiretime > 0, expiretime);
        }

        public static GameObject Sphere(Vector3 position, float expiretime = 1f)
        {
            return Sphere(position, 0.25f, RandomColor, expiretime > 0, expiretime);
        }

        public static GameObject Line(Vector3 startPoint, Vector3 endPoint, Color color, float lineWidth = 0.1f, bool temporary = false, float expiretime = 1f, bool taperLine = false)
        {
            if (!DrawGizmos)
            {
                return null;
            }
            if (!SAINPlugin.DebugMode)
            {
                return null;
            }

            var lineObject = new GameObject();
            var lineRenderer = lineObject.AddComponent<LineRenderer>();

            // Modify the color and width of the line
            lineRenderer.material.color = color;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = taperLine ? lineWidth / 4f : lineWidth;

            // Modify the start and end points of the line
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);

            AddGizmo(lineObject, expiretime);

            return lineObject;
        }

        public static void UpdatePositionLine(Vector3 a, Vector3 b, GameObject gameObject)
        {
            var lineRenderer = gameObject.GetComponent<LineRenderer>();
            lineRenderer?.SetPosition(0, a);
            lineRenderer?.SetPosition(1, b);
        }

        public static GameObject Line(Vector3 startPoint, Vector3 endPoint, float lineWidth = 0.1f, float expiretime = 1f, bool taperLine = false)
        {
            return Line(startPoint, endPoint, RandomColor, lineWidth, expiretime > 0, expiretime, taperLine);
        }

        public static GameObject Ray(Vector3 startPoint, Vector3 direction, Color color, float length = 0.35f, float lineWidth = 0.1f, bool temporary = false, float expiretime = 1f, bool taperLine = false)
        {
            Vector3 endPoint = startPoint + direction.normalized * length;
            return Line(startPoint, endPoint, color, lineWidth, expiretime > 0, expiretime, taperLine);
        }

        public static List<GameObject> DrawLinesBetweenPoints(float lineSize, float raisePoints, params Vector3[] points)
        {
            return DrawLinesBetweenPoints(lineSize, -1, raisePoints, points);
        }

        public static List<GameObject> DrawLinesBetweenPoints(params Vector3[] points)
        {
            return DrawLinesBetweenPoints(0.1f, -1, 0f, points);
        }

        private const float sphereMulti = 1.5f;
        private const float maxSphere = 10f;
        private const float minSphere = 0.15f;
        private const float minMag = 0.01f;

        public static void DrawLinesToPoint(List<GameObject> list, Vector3 origin, Color color, float lineSize, float expireTime, float raisePoints, params Vector3[] points)
        {
            if (!DrawGizmos)
            {
                return;
            }
            for (int j = 0; j < points.Length; j++)
            {
                Vector3 pointB = points[j];
                pointB.y += raisePoints;

                if (origin != points[j])
                {
                    Vector3 direction = origin - pointB;
                    float magnitude = direction.magnitude;
                    if (magnitude > minMag)
                    {
                        GameObject ray = Ray(pointB, direction, color, magnitude, lineSize, expireTime > 0, expireTime);
                        list.Add(ray);
                    }
                }
            }
        }

        public static List<GameObject> DrawLinesToPoint(Vector3 origin, Color color, float lineSize, float expireTime, float raisePoints, params Vector3[] points)
        {
            if (!DrawGizmos)
            {
                return null;
            }
            List<GameObject> list = new List<GameObject>();
            DrawLinesToPoint(list, origin, color, lineSize, expireTime, raisePoints, points);
            return list;
        }

        public static void DrawSpheresAtPoints(List<GameObject> list, Color color, float size, float expireTime, float raisePoints, params Vector3[] points)
        {
            if (!DrawGizmos)
            {
                return;
            }
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 pointA = points[i];
                pointA.y += raisePoints;
                GameObject sphere = Sphere(pointA, size, color, expireTime > 0, expireTime);
                list.Add(sphere);
            }
        }

        public static List<GameObject> DrawSpheresAtPoints(Color color, float size, float expireTime, float raisePoints, params Vector3[] points)
        {
            if (!DrawGizmos)
            {
                return null;
            }
            List<GameObject> list = new List<GameObject>();
            DrawSpheresAtPoints(list, color, size, expireTime, raisePoints, points);
            return list;
        }

        public static List<GameObject> DrawLinesBetweenPoints(float lineSize, float expireTime, float raisePoints, params Vector3[] points)
        {
            if (!DrawGizmos)
            {
                return null;
            }

            List<GameObject> list = new List<GameObject>();
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 pointA = points[i];
                pointA.y += raisePoints;

                Color color = RandomColor;

                float sphereSize = Mathf.Clamp(lineSize * sphereMulti, minSphere, maxSphere);
                GameObject sphere = Sphere(pointA, sphereSize, color, expireTime > 0, expireTime);
                list.Add(sphere);

                DrawLinesToPoint(list, pointA, color, lineSize, expireTime, raisePoints, points);
            }
            return list;
        }

        public static List<GameObject> DrawLinesBetweenPoints(float lineSize, float expireTime, float raisePoints, Color color, params Vector3[] points)
        {
            if (!DrawGizmos)
            {
                return null;
            }

            List<GameObject> list = new List<GameObject>();
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 pointA = points[i];
                pointA.y += raisePoints;

                float sphereSize = Mathf.Clamp(lineSize * sphereMulti, minSphere, maxSphere);
                GameObject sphere = Sphere(pointA, sphereSize, color, expireTime > 0, expireTime);
                list.Add(sphere);

                DrawLinesToPoint(list, pointA, color, lineSize, expireTime, raisePoints, points);
            }
            return list;
        }

        private static float RandomFloat => Random.Range(0.2f, 1f);
        public static Color RandomColor => new Color(RandomFloat, RandomFloat, RandomFloat);

        public class DrawLists
        {
            private static ManualLogSource Logger;
            private Color ColorA;
            private Color ColorB;

            public DrawLists(Color colorA, Color colorB, string LogName = "", bool randomColor = false)
            {
                LogName += "[Drawer]";

                if (randomColor)
                {
                    ColorA = new Color(Random.value, Random.value, Random.value);
                    ColorB = new Color(Random.value, Random.value, Random.value);
                }
                else
                {
                    ColorA = colorA;
                    ColorB = colorB;
                }

                Logger = BepInEx.Logging.Logger.CreateLogSource(LogName);
            }

            public void DrawTempPath(NavMeshPath Path, bool active, Color colorActive, Color colorInActive, float lineSize = 0.05f, float expireTime = 0.5f, bool useDrawerSetColors = false)
            {
                if (!DrawGizmos)
                {
                    return;
                }

                for (int i = 0; i < Path.corners.Length - 1; i++)
                {
                    Vector3 corner1 = Path.corners[i] + Vector3.up;
                    Vector3 corner2 = Path.corners[i + 1] + Vector3.up;

                    Color color;
                    if (useDrawerSetColors)
                    {
                        color = active ? ColorA : ColorB;
                    }
                    else
                    {
                        color = active ? colorActive : colorInActive;
                    }

                    Line(corner1, corner2, color, lineSize, true, expireTime);
                }
            }

            public void Draw(List<Vector3> list, bool destroy, float size = 0.1f, bool rays = false, float rayLength = 0.35f)
            {
                if (!DrawGizmos)
                {
                    DestroyDebug();
                    return;
                }
                if (destroy)
                {
                    DestroyDebug();
                }
                else if (list.Count > 0 && DebugObjects == null)
                {
                    Logger.LogWarning($"Drawing {list.Count} Vector3s");

                    DebugObjects = Create(list, size, rays, rayLength);
                }
            }

            public void Draw(Vector3[] array, bool destroy, float size = 0.1f, bool rays = false, float rayLength = 0.35f)
            {
                if (!DrawGizmos)
                {
                    DestroyDebug();
                    return;
                }
                if (destroy)
                {
                    DestroyDebug();
                }
                else if (array.Length > 0 && DebugObjects == null)
                {
                    Logger.LogWarning($"Drawing {array.Length} Vector3s");

                    DebugObjects = Create(array, size, rays, rayLength);
                }
            }

            private GameObject[] Create(List<Vector3> list, float size = 0.1f, bool rays = false, float rayLength = 0.35f)
            {
                List<GameObject> debugObjects = new List<GameObject>();
                foreach (var point in list)
                {
                    if (rays)
                    {
                        size *= Random.Range(0.5f, 1.5f);
                        rayLength *= Random.Range(0.5f, 1.5f);
                        var ray = Ray(point, Vector3.up, ColorA, rayLength, size);
                        debugObjects.Add(ray);
                    }
                    else
                    {
                        var sphere = Sphere(point, size, ColorA);
                        debugObjects.Add(sphere);
                    }
                }

                return debugObjects.ToArray();
            }

            private GameObject[] Create(Vector3[] array, float size = 0.1f, bool rays = false, float rayLength = 0.35f)
            {
                List<GameObject> debugObjects = new List<GameObject>();
                foreach (var point in array)
                {
                    if (rays)
                    {
                        size *= Random.Range(0.5f, 1.5f);
                        rayLength *= Random.Range(0.5f, 1.5f);
                        var ray = Ray(point, Vector3.up, ColorA, rayLength, size);
                        debugObjects.Add(ray);
                    }
                    else
                    {
                        var sphere = Sphere(point, size, ColorA);
                        debugObjects.Add(sphere);
                    }
                }

                return debugObjects.ToArray();
            }

            private void DestroyDebug()
            {
                if (DebugObjects != null)
                {
                    foreach (var point in DebugObjects)
                    {
                        Object.Destroy(point);
                    }

                    DebugObjects = null;
                }
            }

            private GameObject[] DebugObjects;
        }

        public class Components
        {
            /// <summary>
            /// Creates a line between two game objects and adds a script to update the line's DrawPosition and color every frame.
            /// </summary>
            /// <param value="startObject">The starting game object.</param>
            /// <param value="endObject">The ending game object.</param>
            /// <param value="lineWidth">The width of the line.</param>
            /// <param value="color">The color of the line.</param>
            /// <returns>The game object containing the line renderer.</returns>
            public static GameObject FollowLine(GameObject startObject, GameObject endObject, float lineWidth, Color color)
            {
                var lineObject = new GameObject();
                var lineRenderer = lineObject.AddComponent<LineRenderer>();

                // Modify the color and width of the line
                lineRenderer.material.color = color;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;

                // Modify the initial start and end points of the line
                lineRenderer.SetPosition(0, startObject.transform.position);
                lineRenderer.SetPosition(1, endObject.transform.position);

                // AddColor a script to update the line's DrawPosition and color every frame
                var followLineScript = lineObject.AddComponent<FollowLineScript>();
                followLineScript.startObject = startObject;
                followLineScript.endObject = endObject;
                followLineScript.lineRenderer = lineRenderer;

                return lineObject;
            }

            public class FollowLineScript : MonoBehaviour
            {
                public GameObject startObject;
                public GameObject endObject;
                public LineRenderer lineRenderer;
                public float yOffset = 1f;

                private void Update()
                {
                    lineRenderer.SetPosition(0, startObject.transform.position + new Vector3(0, yOffset, 0));
                    lineRenderer.SetPosition(1, endObject.transform.position + new Vector3(0, yOffset, 0));
                }

                /// <summary>
                /// Sets the color of the line renderer material.
                /// </summary>
                /// <param value="color">The color to set.</param>
                public void SetColor(Color color)
                {
                    lineRenderer.material.color = color;
                }
            }
        }

        internal class TempCoroutine : MonoBehaviour
        {
            /// <summary>
            /// Class to run coroutines on a MonoBehaviour.
            /// </summary>
            internal class TempCoroutineRunner : MonoBehaviour
            { }

            /// <summary>
            /// Destroys the specified GameObject after a given delay.
            /// </summary>
            /// <param value="obj">The GameObject to be destroyed.</param>
            /// <param value="delay">The delay before the GameObject is destroyed.</param>
            public static void DestroyAfterDelay(GameObject obj, float delay)
            {
                if (obj != null)
                {
                    var runner = new GameObject("TempCoroutineRunner").AddComponent<TempCoroutineRunner>();
                    runner?.StartCoroutine(RunDestroyAfterDelay(obj, delay));
                }
            }

            /// <summary>
            /// Runs a coroutine to destroy a GameObject after a delay.
            /// </summary>
            /// <param value="obj">The GameObject to destroy.</param>
            /// <param value="delay">The delay before destroying the GameObject.</param>
            /// <returns>The coroutine.</returns>
            private static IEnumerator RunDestroyAfterDelay(GameObject obj, float delay)
            {
                yield return new WaitForSeconds(delay);
                if (obj != null)
                {
                    TempCoroutineRunner runner = obj.GetComponentInParent<TempCoroutineRunner>();
                    if (runner != null)
                    {
                        Destroy(runner.gameObject);
                    }
                    Destroy(obj);
                }
            }

            private void OnDestroy()
            {
                StopAllCoroutines();
            }
        }
    }
}
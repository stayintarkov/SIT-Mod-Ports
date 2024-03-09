// #define DEBUG_DETAILS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.Weather;
using GPUInstancer;
using HarmonyLib;
using ThatsLit.Patches.Vision;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

using BaseCellClass = GClass964;
using CellClass = GClass965;
using SpatialPartitionClass = GClass979<GClass964>;

namespace ThatsLit.Components
{
    public class ThatsLitMainPlayerComponent : MonoBehaviour
    {
        public static bool IsDebugSampleFrame { get; set; }
        public bool disabledLit;
        readonly int RESOLUTION = 32 * ThatsLitPlugin.ResLevel.Value;
        public const int POWER = 3;
        public RenderTexture rt, envRt;
        public Camera cam, envCam;
        // public Texture2D envTex, envDebugTex;
        Unity.Collections.NativeArray<Color32> observed;
        public float lastCalcFrom, lastCalcTo, lastScore, lastFactor1, lastFactor2;
        public int calced = 0, calcedLastFrame = 0, encounter;
        public int lockPos = -1;
        public RawImage display;
        public RawImage displayEnv;

        public float foliageScore;
        internal int foliageCount;
        internal Vector2 foliageDir;
        internal float foliageDisH, foliageDisV;
        internal string foliage;
        internal bool foliageCloaking;
        Collider[] collidersCache;
        public LayerMask foliageLayerMask = 1 << LayerMask.NameToLayer("Foliage") | 1 << LayerMask.NameToLayer("Grass") | 1 << LayerMask.NameToLayer("PlayerSpiritAura");
        // PlayerSpiritAura is Visceral Bodies compat

        float awakeAt, lastCheckedLights, lastCheckedFoliages, lastCheckedDetails;
        // Note: If vLight > 0, other counts may be skipped

        // public Vector3 envCamOffset = new Vector3(0, 2, 0);

        public RaidSettings activeRaidSettings;
        internal bool skipFoliageCheck, skipDetailCheck;
        public float fog, rain, cloud;
        public float MultiFrameLitScore { get; private set; }
        public Vector3 lastTriggeredDetailCoverDirNearest;
        public float lastTiltAngle, lastRotateAngle, lastDisFactorNearest;
        public float lastNearest;
        public float lastFinalDetailScoreNearest;
        internal int recentDetailCount3x3;
        internal ScoreCalculator scoreCalculator;
        AsyncGPUReadbackRequest gquReq;
        internal float lastOutside;
        // float benchMark1, benchMark2;
        public void Awake()
        {
            if (!ThatsLitPlugin.EnabledMod.Value)
            {
                this.enabled = false;
                return;
            }

            awakeAt = Time.time;
            collidersCache = new Collider[16];

            Singleton<ThatsLitMainPlayerComponent>.Instance = this;
            MainPlayer = Singleton<GameWorld>.Instance.MainPlayer;

            var session = (TarkovApplication)Singleton<ClientApplication<ISession>>.Instance;
            if (session == null) throw new Exception("No session!");
            activeRaidSettings = (RaidSettings)(typeof(TarkovApplication).GetField("_raidSettings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session));

            if (ThatsLitPlugin.EnabledLighting.Value)
            {
                switch (activeRaidSettings?.LocationId)
                {
                    case "Lighthouse":
                        if (ThatsLitPlugin.EnableLighthouse.Value) scoreCalculator = new LighthouseScoreCalculator();
                        break;
                    case "Woods":
                        if (ThatsLitPlugin.EnableWoods.Value) scoreCalculator = new WoodsScoreCalculator();
                        break;
                    case "factory4_night":
                        if (ThatsLitPlugin.EnableFactoryNight.Value) scoreCalculator = GetInGameDayTime() > 12 ? null : new NightFactoryScoreCalculator();
                        skipFoliageCheck = true;
                        skipDetailCheck = true;
                        break;
                    case "factory4_day":
                        scoreCalculator = null;
                        skipFoliageCheck = true;
                        skipDetailCheck = true;
                        break;
                    case "bigmap": // Customs
                        if (ThatsLitPlugin.EnableCustoms.Value) scoreCalculator = new CustomsScoreCalculator();
                        break;
                    case "RezervBase": // Reserve
                        if (ThatsLitPlugin.EnableReserve.Value) scoreCalculator = new ReserveScoreCalculator();
                        break;
                    case "Interchange":
                        if (ThatsLitPlugin.EnableInterchange.Value) scoreCalculator = new InterchangeScoreCalculator();
                        break;
                    case "TarkovStreets":
                        if (ThatsLitPlugin.EnableStreets.Value) scoreCalculator = new StreetsScoreCalculator();
                        break;
                    case "Shoreline":
                        if (ThatsLitPlugin.EnableShoreline.Value) scoreCalculator = new ShorelineScoreCalculator();
                        break;
                    case "laboratory":
                        // scoreCalculator = new LabScoreCalculator();
                        scoreCalculator = null;
                        skipFoliageCheck = true;
                        skipDetailCheck = true;
                        break;
                    case null:
                        if (ThatsLitPlugin.EnableHideout.Value) scoreCalculator = new HideoutScoreCalculator();
                        skipFoliageCheck = true;
                        skipDetailCheck = true;
                        break;
                    default:
                        break;
                }
            }

            if (!ThatsLitPlugin.EnabledGrasses.Value)
                skipDetailCheck = true;

            if (scoreCalculator == null)
            {
                disabledLit = true;
                return;
            }

            rt = new RenderTexture(RESOLUTION, RESOLUTION, 0, RenderTextureFormat.ARGB32);
            rt.useMipMap = false;
            rt.filterMode = FilterMode.Point;
            rt.Create();

            //cam = GameObject.Instantiate<Camera>(Singleton<PlayerCameraController>.Instance.Camera);
            cam = new GameObject().AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;
            cam.transform.SetParent(MainPlayer.Transform.Original);

            cam.nearClipPlane = 0.001f;
            cam.farClipPlane = 10f;

            cam.cullingMask = LayerMaskClass.PlayerMask;
            cam.fieldOfView = 44;

            cam.targetTexture = rt;



            if (ThatsLitPlugin.DebugTexture.Value)
            {
                //debugTex = new Texture2D(RESOLUTION, RESOLUTION, TextureFormat.RGBA32, false);
                display = new GameObject().AddComponent<RawImage>();
                display.transform.SetParent(MonoBehaviourSingleton<GameUI>.Instance.RectTransform());
                display.RectTransform().sizeDelta = new Vector2(160, 160);
                display.texture = rt;
                display.RectTransform().anchoredPosition = new Vector2(-720, -360);


                //envRt = new RenderTexture(RESOLUTION, RESOLUTION, 0);
                //envRt.filterMode = FilterMode.Point;
                //envRt.Create();

                //envTex = new Texture2D(RESOLUTION / 2, RESOLUTION / 2);

                //envCam = new GameObject().AddComponent<Camera>();
                //envCam.clearFlags = CameraClearFlags.SolidColor;
                //envCam.backgroundColor = Color.white;
                //envCam.transform.SetParent(MainPlayer.Transform.Original);
                //envCam.transform.localPosition = Vector3.up * 3;

                //envCam.nearClipPlane = 0.01f;

                //envCam.cullingMask = ~LayerMaskClass.PlayerMask;
                //envCam.fieldOfView = 75;

                //envCam.targetTexture = envRt;

                //envDebugTex = new Texture2D(RESOLUTION / 2, RESOLUTION / 2);
                //displayEnv = new GameObject().AddComponent<RawImage>();
                //displayEnv.transform.SetParent(MonoBehaviourSingleton<GameUI>.Instance.RectTransform());
                //displayEnv.RectTransform().sizeDelta = new Vector2(160, 160);
                //displayEnv.texture = envDebugTex;
                //displayEnv.RectTransform().anchoredPosition = new Vector2(-560, -360);
            }
        }

        internal static System.Diagnostics.Stopwatch _benchmarkSW, _benchmarkSWGUI;

        private void Update()
        {
            if (!ThatsLitPlugin.EnabledMod.Value)
            {
                if (cam?.enabled ?? false) GameObject.Destroy(cam.gameObject);
                if (rt != null) rt.Release();
                if (display?.enabled ?? false) GameObject.Destroy(display);
                this.enabled = false;
                return;
            }

            #region BENCHMARK
            if (ThatsLitPlugin.EnableBenchmark.Value)
            {
                if (_benchmarkSW == null) _benchmarkSW = new System.Diagnostics.Stopwatch();
                if (_benchmarkSW.IsRunning) throw new Exception("Wrong assumption");
                _benchmarkSW.Start();
            }
            else if (_benchmarkSW != null)
                _benchmarkSW = null;
            #endregion

            if (!MainPlayer.AIData.IsInside) lastOutside = Time.time;
            IsDebugSampleFrame = ThatsLitPlugin.DebugInfo.Value && Time.frameCount % 47 == 0;

            Vector3 bodyPos = MainPlayer.MainParts[BodyPartType.body].Position;
            if (!skipDetailCheck && Time.time > lastCheckedDetails + 0.5f)
            {
                if (GPUInstancerDetailManager.activeManagerList.Count == 0)
                {
                    skipDetailCheck = true;
                }
                else
                {
                    CheckTerrainDetails();
                    lastCheckedDetails = Time.time;
                    if (ThatsLitPlugin.TerrainInfo.Value)
                    {
                        CalculateDetailScore(Vector3.zero, 0, 0, out terrainScoreHintProne, out terrainScoreHintRegular);
                        terrainScoreHintRegular /= (MainPlayer.PoseLevel / MainPlayer.Physical.MaxPoseLevel * 0.6f + 0.4f + 0.1f);
                    }
                }
            }
            if (Time.time > lastCheckedFoliages + (ThatsLitPlugin.LessFoliageCheck.Value ? 0.75f : 0.4f))
            {
                UpdateFoliageScore(bodyPos);
            }
            if (disabledLit)
            {
                return;
            }
            var camPos = 0;
            if (lockPos != -1) camPos = lockPos;
            else camPos = Time.frameCount % 6;
            var camHeight = MainPlayer.IsInPronePose ? 0.45f : 2.2f;
            var targetHeight = MainPlayer.IsInPronePose ? 0.2f : 0.7f;
            var horizontalScale = MainPlayer.IsInPronePose ? 1.2f : 1;
            switch (Time.frameCount % 6)
            {
                case 0:
                    {
                        if (MainPlayer.IsInPronePose)
                        {
                            cam.transform.localPosition = new Vector3(0, 2, 0);
                            cam.transform.LookAt(MainPlayer.Transform.Original.position);
                        }
                        else
                        {
                            cam.transform.localPosition = new Vector3(0, camHeight, 0);
                            cam.transform.LookAt(MainPlayer.Transform.Original.position);
                        }
                        break;
                    }
                case 1:
                    {
                        cam.transform.localPosition = new Vector3(0.7f * horizontalScale, camHeight, 0.7f * horizontalScale);
                        cam.transform.LookAt(MainPlayer.Transform.Original.position + Vector3.up * targetHeight);
                        break;
                    }
                case 2:
                    {
                        cam.transform.localPosition = new Vector3(0.7f * horizontalScale, camHeight, -0.7f * horizontalScale);
                        cam.transform.LookAt(MainPlayer.Transform.Original.position + Vector3.up * targetHeight);
                        break;
                    }
                case 3:
                    {
                        if (MainPlayer.IsInPronePose)
                        {
                            cam.transform.localPosition = new Vector3(0, 2f, 0);
                            cam.transform.LookAt(MainPlayer.Transform.Original.position);
                        }
                        else
                        {
                            cam.transform.localPosition = new Vector3(0, -0.5f, 0.35f);
                            cam.transform.LookAt(MainPlayer.Transform.Original.position + Vector3.up * 1f);
                        }
                        break;
                    }
                case 4:
                    {
                        cam.transform.localPosition = new Vector3(-0.7f * horizontalScale, camHeight, -0.7f * horizontalScale);
                        cam.transform.LookAt(MainPlayer.Transform.Original.position + Vector3.up * targetHeight);
                        break;
                    }
                case 5:
                    {
                        cam.transform.localPosition = new Vector3(-0.7f * horizontalScale, camHeight, 0.7f * horizontalScale);
                        cam.transform.LookAt(MainPlayer.Transform.Original.position + Vector3.up * targetHeight);
                        break;
                    }
            }

            if (gquReq.done) gquReq = AsyncGPUReadback.Request(rt, 0, req =>
            {
                if (req.hasError)
                    return;

                observed.Dispose();
                observed = req.GetData<Color32>();
                scoreCalculator?.PreCalculate(observed, GetInGameDayTime());
            });

            // if (ThatsLitPlugin.DebugTexture.Value && envCam)
            // {
            //     envCam.transform.localPosition = envCamOffset;
            //     switch (camPos)
            //     {
            //         case 0:
            //             {
            //                 envCam.transform.LookAt(bodyPos + Vector3.left * 25);
            //                 break;
            //             }
            //         case 1:
            //             {
            //                 envCam.transform.LookAt(bodyPos + Vector3.right * 25);
            //                 break;
            //             }
            //         case 2:
            //             {
            //                 envCam.transform.localPosition = envCamOffset;
            //                 envCam.transform.LookAt(bodyPos + Vector3.down * 10);
            //                 break;
            //             }
            //         case 3:
            //             {
            //                 envCam.transform.LookAt(bodyPos + Vector3.back * 25);
            //                 break;
            //             }
            //         case 4:
            //             {
            //                 envCam.transform.LookAt(bodyPos + Vector3.right * 25);
            //                 break;
            //             }
            //     }
            // }

            if (Time.time > lastCheckedLights + (ThatsLitPlugin.LessEquipmentCheck.Value ? 0.6f : 0.33f))
            {
                lastCheckedLights = Time.time;
                Utility.DetermineShiningEquipments(MainPlayer, out var vLight, out var vLaser, out var irLight, out var irLaser, out var vLightSub, out var vLaserSub, out var irLightSub, out var irLaserSub);
                if (scoreCalculator != null)
                {
                    scoreCalculator.vLight = vLight;
                    scoreCalculator.vLaser = vLaser;
                    scoreCalculator.irLight = irLight;
                    scoreCalculator.irLaser = irLaser;
                    scoreCalculator.vLightSub = vLightSub;
                    scoreCalculator.vLaserSub = vLaserSub;
                    scoreCalculator.irLightSub = irLightSub;
                    scoreCalculator.irLaserSub = irLaserSub;
                }
            }

            #region BENCHMARK
            _benchmarkSW?.Stop();
            #endregion
        }

        private void UpdateFoliageScore(Vector3 bodyPos)
        {
            lastCheckedFoliages = Time.time;
            foliageScore = 0;
            foliageDir = Vector2.zero;

            if (!skipFoliageCheck)
            {
                for (int i = 0; i < collidersCache.Length; i++)
                    collidersCache[i] = null;

                int count = Physics.OverlapSphereNonAlloc(bodyPos, 4f, collidersCache, foliageLayerMask);
                float closet = 9999f;
                foliage = null;

                for (int i = 0; i < count; i++)
                {
                    if (collidersCache[i].gameObject.transform.root.gameObject.layer == 8) continue; // Somehow sometimes player spines are tagged PlayerSpiritAura, VB or vanilla?
                    if (collidersCache[i].gameObject.GetComponent<Terrain>()) continue; // Somehow sometimes terrains can be casted
                    Vector3 dir = (collidersCache[i].transform.position - bodyPos);
                    float dis = dir.magnitude;
                    if (dis < 0.25f) foliageScore += 1f;
                    else if (dis < 0.35f) foliageScore += 0.9f;
                    else if (dis < 0.5f) foliageScore += 0.8f;
                    else if (dis < 0.6f) foliageScore += 0.7f;
                    else if (dis < 0.7f) foliageScore += 0.5f;
                    else if (dis < 1f) foliageScore += 0.3f;
                    else if (dis < 2f) foliageScore += 0.2f;
                    else foliageScore += 0.1f;

                    if (dis < closet)
                    {
                        closet = dis;
                        foliageDir = new Vector2(dir.x, dir.z);
                        foliage = collidersCache[i]?.gameObject.transform.parent.gameObject.name;
                    }
                }

                foliageCount = count;

                if (count > 0)
                {
                    // foliageScore /= (float) count;
                    foliageDisH = foliageDir.magnitude;
                    foliageDisV = Mathf.Abs(foliageDir.y);
                }
                switch (count)
                {
                    case 1:
                        foliageScore /= 3f;
                        break;
                    case 2:
                        foliageScore /= 2.7f;
                        break;
                    case 3:
                        foliageScore /= 2.3f;
                        break;
                    case 4:
                        foliageScore /= 1.8f;
                        break;
                    case 5:
                        foliageScore /= 1.2f;
                        break;
                }
            }
        }

        void LateUpdate()
        {
            if (disabledLit) return;
            GetWeatherStats(out fog, out rain, out cloud);

            //if (debugTex != null && Time.frameCount % 61 == 0) Graphics.CopyTexture(tex, debugTex);
            // if (envDebugTex != null && Time.frameCount % 61 == 0) Graphics.CopyTexture(envTex, envDebugTex);

            if (!observed.IsCreated) return;
            MultiFrameLitScore = scoreCalculator?.CalculateMultiFrameScore(observed, cloud, fog, rain, this, GetInGameDayTime(), activeRaidSettings.LocationId) ?? 0;
            observed.Dispose();
        }

        private void OnDestroy()
        {
            if (display) GameObject.Destroy(display);
            if (cam) GameObject.Destroy(cam);
            if (rt) rt.Release();

        }
        float litFactorSample, ambScoreSample;
        float benchmarkSampleSeenCoef, benchmarkSampleEncountering, benchmarkSampleExtraVisDis, benchmarkSampleScoreCalculator, benchmarkSampleUpdate, benchmarkSampleGUI;
        int guiFrame;
        private void OnGUI()
        {
            #region BENCHMARK
            if (ThatsLitPlugin.EnableBenchmark.Value)
            {
                if (_benchmarkSWGUI == null) _benchmarkSWGUI = new System.Diagnostics.Stopwatch();
                if (_benchmarkSWGUI.IsRunning) throw new Exception("Wrong assumption");
                _benchmarkSWGUI.Start();
            }
            else if (_benchmarkSWGUI != null)
                _benchmarkSWGUI = null;
            #endregion
            bool skip = false;
            if (disabledLit && Time.time - awakeAt < 30f)
            {
                if (!ThatsLitPlugin.HideMapTip.Value) GUILayout.Label(" [That's Lit] Lit detection on this map is not supported or disabled in configs.");
                if (!ThatsLitPlugin.DebugInfo.Value) skip = true;
            }
            if (!skip)
            {
                if (ThatsLitPlugin.DebugInfo.Value || ThatsLitPlugin.ScoreInfo.Value)
                {
                    if (!disabledLit) Utility.GUILayoutDrawAsymetricMeter((int)(MultiFrameLitScore / 0.0999f));
                    if (!disabledLit) Utility.GUILayoutDrawAsymetricMeter((int)(Mathf.Pow(MultiFrameLitScore, POWER) / 0.0999f));
                    if (foliageScore > 0 && ThatsLitPlugin.FoliageInfo.Value)
                        Utility.GUILayoutFoliageMeter((int)(foliageScore / 0.0999f));
                    if (!skipDetailCheck && terrainScoreHintProne > 0.0998f && ThatsLitPlugin.TerrainInfo.Value)
                        if (MainPlayer.IsInPronePose) Utility.GUILayoutTerrainMeter((int)(terrainScoreHintProne / 0.0999f));
                        else Utility.GUILayoutTerrainMeter((int)(terrainScoreHintRegular / 0.0999f));
                    if (Time.time < awakeAt + 10)
                        GUILayout.Label(" [That's Lit HUD] Can be disabled in plugin settings.");
                }
            }
            if (!ThatsLitPlugin.DebugInfo.Value) skip = true;
            if (!skip)
            {
                scoreCalculator?.CalledOnGUI();
                if (IsDebugSampleFrame)
                {
                    litFactorSample = scoreCalculator?.litScoreFactor ?? 0;
                    ambScoreSample = scoreCalculator?.frame0.ambienceScore ?? 0;
                    if (ThatsLitPlugin.EnableBenchmark.Value && guiFrame < Time.frameCount) // The trap here is OnGUI is called multiple times per frame, make sure to reset the stopwatches only once
                    {
                        if (SeenCoefPatch._benchmarkSW != null) benchmarkSampleSeenCoef = (SeenCoefPatch._benchmarkSW.ElapsedMilliseconds / 47f);
                        if (EncounteringPatch._benchmarkSW != null) benchmarkSampleEncountering = (EncounteringPatch._benchmarkSW.ElapsedMilliseconds / 47f);
                        if (ExtraVisibleDistancePatch._benchmarkSW != null) benchmarkSampleExtraVisDis = (ExtraVisibleDistancePatch._benchmarkSW.ElapsedMilliseconds / 47f);
                        if (ScoreCalculator._benchmarkSW != null) benchmarkSampleScoreCalculator = (ScoreCalculator._benchmarkSW.ElapsedMilliseconds / 47f);
                        if (_benchmarkSW != null) benchmarkSampleUpdate = (_benchmarkSW.ElapsedMilliseconds / 47f);
                        if (_benchmarkSWGUI != null) benchmarkSampleGUI = (_benchmarkSWGUI.ElapsedMilliseconds / 47f);
                        SeenCoefPatch._benchmarkSW?.Reset();
                        EncounteringPatch._benchmarkSW?.Reset();
                        ExtraVisibleDistancePatch._benchmarkSW?.Reset();
                        ScoreCalculator._benchmarkSW?.Reset();
                        _benchmarkSW?.Reset();
                        _benchmarkSWGUI?.Reset();
                    }
                }
                GUILayout.Label(string.Format(" IMPACT: {0:0.00} -> {1:0.00} ({2:0.00} <- {3:0.00} <- {4:0.00}) AMB: {5:0.00} LIT: {6:0.00} (SAMPLE)", lastCalcFrom, lastCalcTo, lastFactor2, lastFactor1, lastScore, ambScoreSample, litFactorSample));
                //GUILayout.Label(text: "PIXELS:");
                //GUILayout.Label(lastValidPixels.ToString());
                GUILayout.Label(string.Format(" AFFECTED: {0} (+{1}) / ENCOUNTER: {2}", calced, calcedLastFrame, encounter));

                GUILayout.Label(string.Format(" FOLIAGE: {0:0.000} ({1}) (H{2:0.00} Y{3:0.00} to {4})", foliageScore, foliageCount, foliageDisH, foliageDisV, foliage));

                var poseFactor = MainPlayer.AIData.Player.PoseLevel / MainPlayer.AIData.Player.Physical.MaxPoseLevel * 0.6f + 0.4f; // crouch: 0.4f
                if (MainPlayer.AIData.Player.IsInPronePose) poseFactor -= 0.4f; // prone: 0
                poseFactor += 0.05f; // base -> prone -> 0.05f, crouch -> 0.45f
                                     // GUILayout.Label(string.Format(" POSE: {0:0.000} LOOK: {1} ({2})", poseFactor, MainPlayer.LookDirection, DetermineDir(MainPlayer.LookDirection)));
                                     // GUILayout.Label(string.Format(" {0} {1} {2}", collidersCache[0]?.gameObject.name, collidersCache[1]?.gameObject?.name, collidersCache[2]?.gameObject?.name));
                GUILayout.Label(string.Format(" FOG: {0:0.000} / RAIN: {1:0.000} / CLOUD: {2:0.000} / TIME: {3:0.000}", WeatherController.Instance?.WeatherCurve?.Fog ?? 0, WeatherController.Instance?.WeatherCurve?.Rain ?? 0, WeatherController.Instance?.WeatherCurve?.Cloudiness ?? 0, GetInGameDayTime()));
                if (scoreCalculator != null) GUILayout.Label(string.Format(" LIGHT: [{0}] / LASER: [{1}] / LIGHT2: [{2}] / LASER2: [{3}]", scoreCalculator.vLight ? "V" : scoreCalculator.irLight ? "I" : "-", scoreCalculator.vLaser ? "V" : scoreCalculator.irLaser ? "I" : "-", scoreCalculator.vLightSub ? "V" : scoreCalculator.irLightSub ? "I" : "-", scoreCalculator.vLaserSub ? "V" : scoreCalculator.irLaserSub ? "I" : "-"));
                // GUILayout.Label(string.Format(" {0} ({1})", activeRaidSettings?.LocationId, activeRaidSettings?.SelectedLocation?.Name));
                // GUILayout.Label(string.Format(" {0:0.00000}ms / {1:0.00000}ms", benchMark1, benchMark2));
                if (ThatsLitPlugin.EnableBenchmark.Value)
                {
                    GUILayout.Label(string.Format(" Update: {0,8:0.000}\n SeenCoef: {1,8:0.000}\n Encountering: {2,8:0.000}\n ExtraVisDis: {3,8:0.000}\n ScoreCalculator: {4,8:0.000}\n Info(+Debug): {5,8:0.000} ms", benchmarkSampleUpdate, benchmarkSampleSeenCoef, benchmarkSampleEncountering, benchmarkSampleExtraVisDis, benchmarkSampleScoreCalculator, benchmarkSampleGUI));
                    if (Time.frameCount % 6000 == 0)
                        EFT.UI.ConsoleScreen.Log($"[That's Lit Benchmark Sample] Update: {benchmarkSampleUpdate,8:0.000} / SeenCoef: {benchmarkSampleSeenCoef,8:0.000} / Encountering: {benchmarkSampleEncountering,8:0.000} / ExtraVisDis: {benchmarkSampleExtraVisDis,8:0.000} / ScoreCalculator: {benchmarkSampleScoreCalculator,8:0.000} / GUI: {benchmarkSampleGUI,8:0.000} ms");
                }
#if DEBUG_DETAILS
            GUILayout.Label(string.Format(" DETAIL (SAMPLE): {0:+0.00;-0.00;+0.00} ({1:0.000}df) 3x3: {2}", lastFinalDetailScoreNearest, lastDisFactorNearest, recentDetailCount3x3));
            GUILayout.Label(string.Format(" {0} {1:0.00}m {2} {3}", Utility.DetermineDir(lastTriggeredDetailCoverDirNearest), lastNearest, lastTiltAngle, lastRotateAngle));
            for (int i = GetDetailInfoIndex(2, 2, 0); i < GetDetailInfoIndex(3, 2, 0); i++)
                if (detailsHere5x5[i].casted)
                    GUILayout.Label($"  { detailsHere5x5[i].count } Detail#{i}({ detailsHere5x5[i].name }))");
            Utility.GUILayoutDrawAsymetricMeter((int)(lastFinalDetailScoreNearest / 0.0999f));
#endif
                // GUILayout.Label($"MID  DETAIL_LOW: { scoreCache[16] } DETAIL_MID: {scoreCache[17]}");
                // GUILayout.Label($"  N  DETAIL_LOW: { scoreCache[0] } DETAIL_MID: {scoreCache[1]}");
                // GUILayout.Label($" NE  DETAIL_LOW: { scoreCache[2] } DETAIL_MID: {scoreCache[3]}");
                // GUILayout.Label($"  E  DETAIL_LOW: { scoreCache[4] } DETAIL_MID: {scoreCache[5]}");
                // GUILayout.Label($" SE  DETAIL_LOW: { scoreCache[6] } DETAIL_MID: {scoreCache[7]}");
                // GUILayout.Label($"  S  DETAIL_LOW: { scoreCache[8] } DETAIL_MID: {scoreCache[9]}");
                // GUILayout.Label($" SW  DETAIL_LOW: { scoreCache[10] } DETAIL_MID: {scoreCache[11]}");
                // GUILayout.Label($"  W  DETAIL_LOW: { scoreCache[12] } DETAIL_MID: {scoreCache[13]}");
                // GUILayout.Label($" NW  DETAIL_LOW: { scoreCache[14] } DETAIL_MID: {scoreCache[15]}");

            }
            #region BENCHMARK
            _benchmarkSWGUI?.Stop();
            #endregion
            guiFrame = Time.frameCount;
        }


        public Player MainPlayer { get; private set; }

        float GetInGameDayTime()
        {
            if (Singleton<GameWorld>.Instance?.GameDateTime == null) return 19f;

            var GameDateTime = Singleton<GameWorld>.Instance.GameDateTime.Calculate();

            float minutes = GameDateTime.Minute / 59f;
            return GameDateTime.Hour + minutes;
        }

        void GetWeatherStats(out float fog, out float rain, out float cloud)
        {
            if (WeatherController.Instance?.WeatherCurve == null)
            {
                fog = rain = cloud = 0;
                return;
            }

            fog = WeatherController.Instance.WeatherCurve.Fog;
            rain = WeatherController.Instance.WeatherCurve.Rain;
            cloud = WeatherController.Instance.WeatherCurve.Cloudiness;
        }

        public DetailInfo[] detailsHere5x5 = new DetailInfo[MAX_DETAIL_TYPES * 25]; // MAX_DETAIL_TYPES(24) x 25;
        public struct DetailInfo
        {
            public bool casted;
            public string name;
            public int count;
        }

        Dictionary<Terrain, SpatialPartitionClass> terrainSpatialPartitions = new Dictionary<Terrain, SpatialPartitionClass>();
        Dictionary<Terrain, List<int[,]>> terrainDetailMaps = new Dictionary<Terrain, List<int[,]>>();
        float terrainScoreHintProne, terrainScoreHintRegular;
        // GameObject marker;
        // float[] scoreCache = new float[18];
        void CheckTerrainDetails()
        {
            Array.Clear(detailScoreFrameCache, 0, detailScoreFrameCache.Length);
            Array.Clear(detailsHere5x5, 0, detailsHere5x5.Length);
            recentDetailCount3x3 = 0;
            var ray = new Ray(MainPlayer.MainParts[BodyPartType.head].Position, Vector3.down);
            if (!Physics.Raycast(ray, out var hit, 100, LayerMaskClass.TerrainMask)) return;
            var terrain = hit.transform.GetComponent<Terrain>();
            GPUInstancerDetailManager manager = terrain?.GetComponent<GPUInstancerTerrainProxy>()?.detailManager;

            if (!terrain || !manager || !manager.isInitialized) return;
            if (!terrainDetailMaps.TryGetValue(terrain, out var detailMap))
            {
                if (gatheringDetailMap == null) gatheringDetailMap = StartCoroutine(BuildAllTerrainDetailMapCoroutine(terrain));
                return;
            }

            Vector3 hitRelativePos = hit.point - (terrain.transform.position + terrain.terrainData.bounds.min);
            var currentLocationOnTerrainmap = new Vector2(hitRelativePos.x / terrain.terrainData.size.x, hitRelativePos.z / terrain.terrainData.size.z);

            for (int d = 0; d < manager.prototypeList.Count; d++)
            {
                var resolution = (manager.prototypeList[d] as GPUInstancerDetailPrototype).detailResolution;
                Vector2Int resolutionPos = new Vector2Int((int)(currentLocationOnTerrainmap.x * resolution), (int)(currentLocationOnTerrainmap.y * resolution));
                // EFT.UI.ConsoleScreen.Log($"JOB: Calculating score for detail#{d} at detail pos ({resolutionPos.x},{resolutionPos.y})" );
                for (int x = 0; x < 5; x++)
                    for (int y = 0; y < 5; y++)
                    {
                        var posX = resolutionPos.x - 2 + x;
                        var posY = resolutionPos.y - 2 + y;
                        int count = 0;

                        if (posX < 0 && terrain.leftNeighbor && posY >= 0 && posY < resolution)
                        {
                            Terrain neighbor = terrain.leftNeighbor;
                            if (!terrainDetailMaps.TryGetValue(neighbor, out var neighborDetailMap))
                                if (gatheringDetailMap == null)
                                    gatheringDetailMap = StartCoroutine(BuildAllTerrainDetailMapCoroutine(neighbor));
                                else if (neighborDetailMap.Count > d) // Async job
                                    count = neighborDetailMap[d][resolution + posX, posY];
                        }
                        else if (posX >= resolution && terrain.rightNeighbor && posY >= 0 && posY < resolution)
                        {
                            Terrain neighbor = terrain.rightNeighbor;
                            if (!terrainDetailMaps.TryGetValue(neighbor, out var neighborDetailMap))
                                if (gatheringDetailMap == null)
                                    gatheringDetailMap = StartCoroutine(BuildAllTerrainDetailMapCoroutine(neighbor));
                                else if (neighborDetailMap.Count > d) // Async job
                                    count = neighborDetailMap[d][posX - resolution, posY];
                        }
                        else if (posY >= resolution && terrain.topNeighbor && posX >= 0 && posX < resolution)
                        {
                            Terrain neighbor = terrain.topNeighbor;
                            if (!terrainDetailMaps.TryGetValue(neighbor, out var neighborDetailMap))
                                if (gatheringDetailMap == null)
                                    gatheringDetailMap = StartCoroutine(BuildAllTerrainDetailMapCoroutine(neighbor));
                                else if (neighborDetailMap.Count > d) // Async job
                                    count = neighborDetailMap[d][posX, posY - resolution];
                        }
                        else if (posY < 0 && terrain.bottomNeighbor && posX >= 0 && posX < resolution)
                        {
                            Terrain neighbor = terrain.bottomNeighbor;
                            if (!terrainDetailMaps.TryGetValue(neighbor, out var neighborDetailMap))
                                if (gatheringDetailMap == null)
                                    gatheringDetailMap = StartCoroutine(BuildAllTerrainDetailMapCoroutine(neighbor));
                                else if (neighborDetailMap.Count > d) // Async job
                                    count = neighborDetailMap[d][posX, posY + resolution];
                        }
                        else if (posY >= resolution && terrain.topNeighbor.rightNeighbor && posX >= resolution)
                        {
                            Terrain neighbor = terrain.topNeighbor.rightNeighbor;
                            if (!terrainDetailMaps.TryGetValue(neighbor, out var neighborDetailMap))
                                if (gatheringDetailMap == null)
                                    gatheringDetailMap = StartCoroutine(BuildAllTerrainDetailMapCoroutine(neighbor));
                                else if (neighborDetailMap.Count > d) // Async job
                                    count = neighborDetailMap[d][posX - resolution, posY - resolution];
                        }
                        else if (posY >= resolution && terrain.topNeighbor.leftNeighbor && posX < 0)
                        {
                            Terrain neighbor = terrain.topNeighbor.leftNeighbor;
                            if (!terrainDetailMaps.TryGetValue(neighbor, out var neighborDetailMap))
                                if (gatheringDetailMap == null)
                                    gatheringDetailMap = StartCoroutine(BuildAllTerrainDetailMapCoroutine(neighbor));
                                else if (neighborDetailMap.Count > d) // Async job
                                    count = neighborDetailMap[d][posX + resolution, posY - resolution];
                        }
                        else if (posY < 0 && terrain.bottomNeighbor.rightNeighbor && posX >= resolution)
                        {
                            Terrain neighbor = terrain.bottomNeighbor.rightNeighbor;
                            if (!terrainDetailMaps.TryGetValue(neighbor, out var neighborDetailMap))
                                if (gatheringDetailMap == null)
                                    gatheringDetailMap = StartCoroutine(BuildAllTerrainDetailMapCoroutine(neighbor));
                                else if (neighborDetailMap.Count > d) // Async job
                                    count = neighborDetailMap[d][posX - resolution, posY + resolution];
                        }
                        else if (posY < 0 && terrain.bottomNeighbor.leftNeighbor && posX < 0)
                        {
                            Terrain neighbor = terrain.bottomNeighbor.leftNeighbor;
                            if (!terrainDetailMaps.TryGetValue(neighbor, out var neighborDetailMap))
                                if (gatheringDetailMap == null)
                                    gatheringDetailMap = StartCoroutine(BuildAllTerrainDetailMapCoroutine(neighbor));
                                else if (neighborDetailMap.Count > d) // Async job
                                    count = neighborDetailMap[d][posX + resolution, posY + resolution];
                        }
                        else if (detailMap.Count > d) // Async job
                        {
                            count = detailMap[d][posX, posY];
                        }

                        detailsHere5x5[GetDetailInfoIndex(x, y, d)] = new DetailInfo()
                        {
                            casted = true,
                            name = manager.prototypeList[d].name,
                            count = count,
                        };

                        if (x >= 1 && x <= 3 && y >= 1 && y <= 3) recentDetailCount3x3 += count;
                    }
            }

            // scoreCache[16] = 0;
            // scoreCache[17] = 0;
            // foreach (var pos in IterateDetailIndex3x3)
            // {
            //     for (int i = 0; i < MAX_DETAIL_TYPES; i++)
            //     {
            //         var info = detailsHere5x5[pos*MAX_DETAIL_TYPES + i];
            //         GetDetailCoverScoreByName(info.name, info.count, out var s1, out var s2);
            //         scoreCache[16] += s1;
            //         scoreCache[17] += s2;
            //     }
            // }
            // CalculateDetailScore(Vector3.forward, 31, 0, out scoreCache[0], out scoreCache[1]);
            // CalculateDetailScore(Vector3.forward + Vector3.right, 31, 0, out scoreCache[2], out scoreCache[3]);
            // CalculateDetailScore(Vector3.right, 31, 0, out scoreCache[4], out scoreCache[5]);
            // CalculateDetailScore(Vector3.right + Vector3.back, 31, 0, out scoreCache[6], out scoreCache[7]);
            // CalculateDetailScore(Vector3.back, 31, 0, out scoreCache[8], out scoreCache[9]);
            // CalculateDetailScore(Vector3.back + Vector3.left, 31, 0, out scoreCache[10], out scoreCache[11]);
            // CalculateDetailScore(Vector3.left, 31, 0, out scoreCache[12], out scoreCache[13]);
            // CalculateDetailScore(Vector3.left + Vector3.forward, 31, 0, out scoreCache[14], out scoreCache[15]);

        }

        Coroutine gatheringDetailMap;
        IEnumerator BuildAllTerrainDetailMapCoroutine(Terrain priority = null)
        {
            yield return new WaitForSeconds(1); // Grass Cutter
            // EFT.UI.ConsoleScreen.Log($"JOB: Staring gathering terrain details..." );
            bool allDisabled = true;
            var mgr = priority.GetComponent<GPUInstancerTerrainProxy>()?.detailManager;
            if (mgr != null && mgr.enabled)
            {
                allDisabled = false;
                if (!terrainDetailMaps.ContainsKey(priority))
                {
                    terrainDetailMaps[priority] = new List<int[,]>(mgr.prototypeList.Count);
                    yield return BuildTerrainDetailMapCoroutine(priority, terrainDetailMaps[priority]);

                }
            }
            else terrainDetailMaps[priority] = null;
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                mgr = terrain.GetComponent<GPUInstancerTerrainProxy>()?.detailManager;
                if (mgr != null && mgr.enabled)
                {
                    allDisabled = false;
                    if (!terrainDetailMaps.ContainsKey(terrain))
                    {
                        terrainDetailMaps[terrain] = new List<int[,]>(mgr.prototypeList.Count);
                        yield return BuildTerrainDetailMapCoroutine(terrain, terrainDetailMaps[terrain]);

                    }
                }
                else terrainDetailMaps[terrain] = null;
            }
            if (allDisabled) skipDetailCheck = true;
        }
        IEnumerator BuildTerrainDetailMapCoroutine(Terrain terrain, List<int[,]> detailMapData)
        {
            var mgr = terrain.GetComponent<GPUInstancerTerrainProxy>()?.detailManager;
            if (mgr == null || !mgr.isInitialized) yield break;
            if (!terrainSpatialPartitions.TryGetValue(terrain, out var spData))
            {
                spData = terrainSpatialPartitions[terrain] = AccessTools.Field(typeof(GPUInstancerDetailManager), "spData").GetValue(mgr) as SpatialPartitionClass;
            }
            if (spData == null)
            {
                terrainSpatialPartitions.Remove(terrain);
            }
            var waitNextFrame = new WaitForEndOfFrame();

            if (detailMapData == null) detailMapData = new List<int[,]>(mgr.prototypeList.Count);
            else detailMapData.Clear();
            for (int layer = 0; layer < mgr.prototypeList.Count; ++layer)
            {
                var prototype = mgr.prototypeList[layer] as GPUInstancerDetailPrototype;
                if (prototype == null) detailMapData.Add(null);
                int[,] detailLayer = new int[prototype.detailResolution, prototype.detailResolution];
                detailMapData.Add(detailLayer);
                var resolutionPerCell = prototype.detailResolution / spData.cellRowAndCollumnCountPerTerrain;
                for (int terrainCellX = 0; terrainCellX < spData.cellRowAndCollumnCountPerTerrain; ++terrainCellX)
                {
                    for (int terrainCellY = 0; terrainCellY < spData.cellRowAndCollumnCountPerTerrain; ++terrainCellY)
                    {
                        BaseCellClass cell;
                        if (spData.GetCell(BaseCellClass.CalculateHash(terrainCellX, 0, terrainCellY), out cell))
                        {
                            CellClass gclass965 = (CellClass)cell;
                            if (gclass965.detailMapData != null)
                            {
                                for (int cellResX = 0; cellResX < resolutionPerCell; ++cellResX)
                                {
                                    for (int cellResY = 0; cellResY < resolutionPerCell; ++cellResY)
                                        detailLayer[cellResX + terrainCellX * resolutionPerCell, cellResY + terrainCellY * resolutionPerCell] = gclass965.detailMapData[layer][cellResX + cellResY * resolutionPerCell];
                                }
                            }
                        }

                        yield return waitNextFrame;
                    }
                }
            }
        }

        int GetDetailInfoIndex(int x5x5, int y5x5, int detailId) => (y5x5 * 5 + x5x5) * MAX_DETAIL_TYPES + detailId;

        (bool, float, float)[] detailScoreFrameCache = new (bool, float, float)[10];
        public void CalculateDetailScore(Vector3 enemyDirection, float dis, float verticalAxisAngle, out float scoreProne, out float scoreRegular)
        {
            bool TryGetCache(int index, out float low, out float mid)
            {
                (bool, float, float) cache = detailScoreFrameCache[index];
                if (cache.Item1)
                {
                    low = cache.Item2;
                    mid = cache.Item3;
                    return true;
                }
                low = 0;
                mid = 0;
                return false;
            }

            int dir = 0;
            IEnumerable<int> it = null;
            if (dis < 10f || verticalAxisAngle < -15f)
            {
                if (TryGetCache(dir = 5, out scoreProne, out scoreRegular)) return;
                it = IterateDetailIndex3x3;
            }
            else
            {
                scoreProne = scoreRegular = 0;
                var dirFlat = (new Vector2(enemyDirection.x, enemyDirection.z)).normalized;
                var angle = Vector2.SignedAngle(Vector2.up, dirFlat);
                if (angle >= -22.5f && angle <= 22.5f)
                {
                    if (TryGetCache(dir = 8, out scoreProne, out scoreRegular)) return;
                    it = IterateDetailIndex3x3N;
                }
                else if (angle >= 22.5f && angle <= 67.5f)
                {
                    if (TryGetCache(dir = 9, out scoreProne, out scoreRegular)) return;
                    it = IterateDetailIndex3x3NE;
                }
                else if (angle >= 67.5f && angle <= 112.5f)
                {
                    if (TryGetCache(dir = 6, out scoreProne, out scoreRegular)) return;
                    it = IterateDetailIndex3x3E;
                }
                else if (angle >= 112.5f && angle <= 157.5f)
                {
                    if (TryGetCache(dir = 3, out scoreProne, out scoreRegular)) return;
                    it = IterateDetailIndex3x3SE;
                }
                else if (angle >= 157.5f && angle <= 180f || angle >= -180f && angle <= -157.5f)
                {
                    if (TryGetCache(dir = 2, out scoreProne, out scoreRegular)) return;
                    it = IterateDetailIndex3x3S;
                }
                else if (angle >= -157.5f && angle <= -112.5f)
                {
                    if (TryGetCache(dir = 1, out scoreProne, out scoreRegular)) return;
                    it = IterateDetailIndex3x3SW;
                }
                else if (angle >= -112.5f && angle <= -67.5f)
                {
                    if (TryGetCache(dir = 4, out scoreProne, out scoreRegular)) return;
                    it = IterateDetailIndex3x3W;
                }
                else if (angle >= -67.5f && angle <= -22.5f)
                {
                    if (TryGetCache(dir = 7, out scoreProne, out scoreRegular)) return;
                    it = IterateDetailIndex3x3NW;
                }
                else throw new Exception($"[That's Lit] Invalid angle to enemy: {angle}");
            }

            foreach (var pos in it)
            {
                for (int i = 0; i < MAX_DETAIL_TYPES; i++)
                {
                    var info = detailsHere5x5[pos * MAX_DETAIL_TYPES + i];
                    if (!info.casted) continue;
                    Utility.CalculateDetailScore(info.name, info.count, out var s1, out var s2);
                    scoreProne += s1;
                    scoreRegular += s2;
                }
            }

            detailScoreFrameCache[dir] = (true, scoreProne, scoreRegular);
        }

        IEnumerable<int> IterateDetailIndex3x3N => IterateIndex3x3In5x5(0, 1);
        IEnumerable<int> IterateDetailIndex3x3E => IterateIndex3x3In5x5(1, 0);
        IEnumerable<int> IterateDetailIndex3x3W => IterateIndex3x3In5x5(-1, 0);
        IEnumerable<int> IterateDetailIndex3x3S => IterateIndex3x3In5x5(0, -1);
        IEnumerable<int> IterateDetailIndex3x3NE => IterateIndex3x3In5x5(1, 1);
        IEnumerable<int> IterateDetailIndex3x3NW => IterateIndex3x3In5x5(-1, 1);
        IEnumerable<int> IterateDetailIndex3x3SE => IterateIndex3x3In5x5(1, -1);
        IEnumerable<int> IterateDetailIndex3x3SW => IterateIndex3x3In5x5(-1, -1);
        IEnumerable<int> IterateDetailIndex3x3 => IterateIndex3x3In5x5(0, 0);
        /// <param name="xOffset">WestSide(-x) => -1, EstSide(+x) => 1</param>
        /// <param name="yOffset"></param>
        /// <returns></returns>
        IEnumerable<int> IterateIndex3x3In5x5(int xOffset, int yOffset)
        {
            yield return 5 * (1 + yOffset) + 1 + xOffset;
            yield return 5 * (1 + yOffset) + 2 + xOffset;
            yield return 5 * (1 + yOffset) + 3 + xOffset;

            yield return 5 * (2 + yOffset) + 1 + xOffset;
            yield return 5 * (2 + yOffset) + 2 + xOffset;
            yield return 5 * (2 + yOffset) + 3 + xOffset;

            yield return 5 * (3 + yOffset) + 1 + xOffset;
            yield return 5 * (3 + yOffset) + 2 + xOffset;
            yield return 5 * (3 + yOffset) + 3 + xOffset;
        }

        const int MAX_DETAIL_TYPES = 24;
    }
}
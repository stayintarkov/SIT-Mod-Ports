using EFT;
using HarmonyLib;
using SAIN.Helpers;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

using PathControllerClass = PathController;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SprintController : SAINBase, ISAINClass
    {
        public SprintController(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (_pathControllerField == null)
            {
                _pathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
            }
            if (_pathController == null && _pathControllerField != null)
            {
                _pathController = (PathControllerClass)_pathControllerField.GetValue(BotOwner.Mover);
            }

            if (SAINPlugin.DebugMode && trackSprintCoroutine == null)
            {
                if (_pathController == null)
                {
                    Logger.LogError("_pathController null");
                }
                else
                {
                    trackSprintCoroutine = SAIN.StartCoroutine(TrackSprint());
                }
            }
            else if (!SAINPlugin.DebugMode && trackSprintCoroutine != null)
            {
                SAIN.StopCoroutine(trackSprintCoroutine);
                trackSprintCoroutine = null;
            }
        }

        private Coroutine trackSprintCoroutine;
        private static FieldInfo _pathControllerField;

        public void Dispose()
        {
        }

        private IEnumerator TrackSprint()
        {
            while (true)
            {
                Vector3 botPosition = SAIN.Position;
                Vector3? currentCorner = CurrentCorner();

                if (currentCorner != null)
                {
                    Vector3 currentCornerDirection = (currentCorner.Value - botPosition).normalized;
                    if (CurrentCornerObject ==  null)
                    {
                        CurrentCornerObject = DebugGizmos.Line(botPosition, currentCorner.Value, 0.1f, -1f);
                        CurrentCornerGUIObject = new GUIObject
                        {
                            WorldPos = currentCorner.Value,
                            Text = $"Current Corner",
                        };
                        DebugGizmos.AddGUIObject(CurrentCornerGUIObject);
                    }
                    else
                    {
                        DebugGizmos.UpdatePositionLine(botPosition, currentCorner.Value, CurrentCornerObject);
                        CurrentCornerGUIObject.WorldPos = currentCorner.Value;
                    }

                    Vector3? nextCorner = NextCorner();

                    if (nextCorner != null)
                    {
                        Vector3 nextCornerDirection = (nextCorner.Value - currentCorner.Value).normalized;
                        currentCornerDirection.y = 0;
                        nextCornerDirection.y = 0;
                        float angle = Vector3.Angle(nextCornerDirection, currentCornerDirection);
                        if (CurrentCornerGUIObject != null)
                        {
                            CurrentCornerGUIObject.Text = $"Current Corner. Angle To Next: [{angle}]";
                        }
                        if (NextCornerObject == null)
                        {
                            NextCornerObject = DebugGizmos.Line(currentCorner.Value, nextCorner.Value, 0.1f, -1f);
                            NextCornerGUIObject = new GUIObject
                            {
                                WorldPos = nextCorner.Value,
                                Text = $"Next Corner",
                            };
                            DebugGizmos.AddGUIObject(NextCornerGUIObject);
                        }
                        else
                        {
                            DebugGizmos.UpdatePositionLine(currentCorner.Value, nextCorner.Value, NextCornerObject);
                            NextCornerGUIObject.WorldPos = nextCorner.Value;
                        }
                    }
                    else if (NextCornerObject != null)
                    {
                        GameObject.Destroy(NextCornerObject);
                        NextCornerObject = null;
                        DebugGizmos.DestroyLabel(NextCornerGUIObject);
                        NextCornerGUIObject = null;
                    }
                }
                else
                {
                    // We have no current corner, and no next corner, destroy both
                    if (CurrentCornerObject != null)
                    {
                        GameObject.Destroy(CurrentCornerObject);
                        CurrentCornerObject = null;
                        DebugGizmos.DestroyLabel(CurrentCornerGUIObject);
                        CurrentCornerGUIObject = null;
                    }
                    if (NextCornerObject != null)
                    {
                        GameObject.Destroy(NextCornerObject);
                        NextCornerObject = null;
                        DebugGizmos.DestroyLabel(NextCornerGUIObject);
                        NextCornerGUIObject = null;
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        private GUIObject CurrentCornerGUIObject;
        private GameObject CurrentCornerObject;
        private GUIObject NextCornerGUIObject;
        private GameObject NextCornerObject;

        private bool IsSprintEnabled => Player.IsSprintEnabled;
        private Vector3? CurrentCorner() => _pathController?.CurrentCorner();
        private Vector3? NextCorner()
        {
            if (_pathController.CurPath != null)
            {
                int i = _pathController.CurPath.CurIndex;
                int max = _pathController.CurPath.Length - 1;
                if (i < max)
                {
                    return _pathController.CurPath.GetPoint(i + 1);
                }
            }
            return null;
        }
        private PathControllerClass _pathController;
    }
}
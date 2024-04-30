using SAIN.Helpers;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class EnemyKnownPlaces
    {
        public EnemyKnownPlaces(SAINEnemy enemy)
        {
            _enemy = enemy;
        }

        private readonly SAINEnemy _enemy;

        public void Dispose()
        {
            foreach (var obj in GUIObjects)
            {
                DebugGizmos.DestroyLabel(obj.Value);
            }
            GUIObjects?.Clear();
            KnownPlaces?.Clear();
        }

        public void Update()
        {
            EnemyPlace lastPlace = null;
            if (_nextCheckArrived < Time.time)
            {
                _nextCheckArrived = Time.time + 0.25f;
                Vector3 myPosition = _enemy.SAIN.Position;
                foreach (var place in KnownPlaces)
                {
                    if (place?.Position != null 
                        && place.HasArrived == false 
                        && (myPosition - place.Position.Value).sqrMagnitude < 2f)
                    {
                        place.HasArrived = true;

                        if (EFTMath.RandomBool(15))
                        {
                            _enemy.SAIN.Talk.Say(EFTMath.RandomBool() ? EPhraseTrigger.Clear : EPhraseTrigger.LostVisual, null, true);
                        }
                    }
                }
            }
            if (_nextCheckSeen < Time.time)
            {
                lastPlace = GetPlaceHaventSeen();
                if (lastPlace?.Position != null)
                {
                    _nextCheckSeen = Time.time + 1f;
                    Vector3 lastknown = lastPlace.Position.Value + Vector3.up;
                    Vector3 botPos = _enemy.SAIN.Person.Transform.Head;
                    Vector3 direction = lastknown - botPos;
                    lastPlace.HasSeen = !Physics.Raycast(botPos, direction.normalized, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI);
                }
            }

            if (SAINPlugin.DebugMode)
            {
                foreach (var obj in GUIObjects)
                {
                    if (!KnownPlaces.Contains(obj.Key))
                    {
                        _debugPlacesToRemove.Add(obj.Key);
                    }
                }
                foreach (var debugPlace in _debugPlacesToRemove)
                {
                    DebugGizmos.DestroyLabel(GUIObjects[debugPlace]);
                    GUIObjects.Remove(debugPlace);
                }
                _debugPlacesToRemove.Clear();

                foreach (var place in KnownPlaces)
                {
                    if (place?.Position != null)
                    {
                        if (!GUIObjects.ContainsKey(place))
                        {
                            GUIObjects.Add(place, new GUIObject());
                        }
                        GUIObject obj = GUIObjects[place];
                        UpdateDebugString(place, obj);
                        DebugGizmos.AddGUIObject(obj);
                    }
                }
            }
        }

        private readonly List<EnemyPlace> _debugPlacesToRemove = new List<EnemyPlace>();

        private void UpdateDebugString(EnemyPlace place, GUIObject obj)
        {
            obj.WorldPos = place.Position.Value;

            StringBuilder stringBuilder = obj.StringBuilder;
            stringBuilder.Clear();

            stringBuilder.AppendLine($"Bot: {_enemy.BotOwner.name}");
            stringBuilder.AppendLine($"Known Location of {_enemy?.EnemyPlayer.Profile.Nickname}");

            if (LastKnownPlace == place)
            {
                stringBuilder.AppendLine($"Last Known Location.");
            }

            stringBuilder.AppendLine($"Time Since Position Updated: {Time.time - place.TimePositionUpdated}");

            stringBuilder.AppendLine($"Arrived? [{place.HasArrived}]" 
                + (place.HasArrived ? $"Time Since Arrived: [{Time.time - place.TimeArrived}]" : string.Empty));

            stringBuilder.AppendLine($"Seen? [{place.HasSeen}]"
                + (place.HasSeen ? $"Time Since Seen: [{Time.time - place.TimeSeen}]" : string.Empty));
        }

        private readonly Dictionary<EnemyPlace, GUIObject> GUIObjects = new Dictionary<EnemyPlace, GUIObject>();

        public bool SearchedAllKnownLocations 
        { 
            get 
            { 
                if (_nextCheckSearchTime < Time.time)
                {
                    _nextCheckSearchTime = Time.time + 1f;

                    bool allSearched = true;
                    foreach (var place in KnownPlaces)
                    {
                        if (!place.HasArrived)
                        {
                            allSearched = false;
                            break;
                        }
                    }

                    if (allSearched 
                        && !_searchedAllKnownLocations)
                    {
                        TimeAllLocationsSearched = Time.time;
                    }

                    _searchedAllKnownLocations = allSearched;
                }
                return _searchedAllKnownLocations;
            }
        }
        public float TimeSinceAllLocationsSearched => Time.time - TimeAllLocationsSearched;
        public float TimeAllLocationsSearched { get; private set; }
        private bool _searchedAllKnownLocations;
        private float _nextCheckSearchTime;

        private EnemyPlace GetPlaceHaventSeenOrArrived(bool skipTimer = false)
        {
            if (skipTimer || _getPlaceHaventArrivedOrSeenTimer < Time.time)
            {
                _getPlaceHaventArrivedOrSeenTimer = Time.time + 0.1f;
                _lastPlaceHaventArrivedOrSeen = null;

                for (int i = KnownPlaces.Count - 1; i >= 0; i--)
                {
                    EnemyPlace enemyPlace = KnownPlaces[i];
                    if (enemyPlace.HasSeen == false || enemyPlace.HasArrived == false)
                    {
                        _lastPlaceHaventArrivedOrSeen = enemyPlace;
                        break;
                    }
                }
            }
            return _lastPlaceHaventArrivedOrSeen;
        }

        private EnemyPlace _lastPlaceHaventArrivedOrSeen;
        private float _getPlaceHaventArrivedOrSeenTimer;

        public EnemyPlace GetPlaceHaventSeen(bool skipTimer = false)
        {
            if (skipTimer || _getPlaceHaventSeenTimer < Time.time)
            {
                _getPlaceHaventSeenTimer = Time.time + 0.1f;
                _lastPlaceHaventSeen = null;

                for (int i = KnownPlaces.Count - 1; i >= 0; i--)
                {
                    EnemyPlace enemyPlace = KnownPlaces[i];
                    if (enemyPlace.HasSeen == false)
                    {
                        _lastPlaceHaventSeen = enemyPlace;
                        break;
                    }
                }
            }
            return _lastPlaceHaventSeen;
        }

        private EnemyPlace _lastPlaceHaventSeen;
        private float _getPlaceHaventSeenTimer;

        public EnemyPlace GetPlaceHaventArrived(bool skipTimer = false)
        {
            if (skipTimer || _getPlaceHaventArrivedTimer < Time.time)
            {
                _getPlaceHaventArrivedTimer = Time.time + 0.1f;
                _lastPlaceHaventArrived = null;

                for (int i = KnownPlaces.Count - 1; i >= 0; i--)
                {
                    EnemyPlace enemyPlace = KnownPlaces[i];
                    if (enemyPlace.HasSeen == false)
                    {
                        _lastPlaceHaventArrived = enemyPlace;
                        break;
                    }
                }
            }
            return _lastPlaceHaventArrived;
        }

        private EnemyPlace _lastPlaceHaventArrived;
        private float _getPlaceHaventArrivedTimer;

        private float _nextCheckArrived;
        private float _nextCheckSeen;

        public void AddPosition(Vector3 position, bool arrived = false, bool seen = false)
        {
            _searchedAllKnownLocations = false;
            EnemyPlace lastKnown = LastKnownPlace;
            if (lastKnown != null)
            {
                float sqrMag = (lastKnown.Position.Value - position).sqrMagnitude;
                if (sqrMag < 2)
                {
                    lastKnown.Position = new Vector3?(position);
                    if (arrived)
                    {
                        lastKnown.HasArrived = true;
                    }
                    if (seen)
                    {
                        lastKnown.HasSeen = true;
                    }
                    return;
                }
            }

            lastKnown = new EnemyPlace(position);
            if (arrived)
            {
                lastKnown.HasArrived = true;
            }
            if (seen)
            {
                lastKnown.HasSeen = true;
            }


            if (KnownPlaces.Count >= MaxKnownPlaces)
            {
                KnownPlaces.RemoveAt(0);
            }

            KnownPlaces.Add(lastKnown);
        }

        private const int MaxKnownPlaces = 5;

        public List<EnemyPlace> KnownPlaces = new List<EnemyPlace>(MaxKnownPlaces);
        public EnemyPlace LastKnownPlace => KnownPlaces.Count > 0 ? KnownPlaces[KnownPlaces.Count - 1] : null;
    }
}
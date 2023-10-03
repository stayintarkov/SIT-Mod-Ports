using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Timers;
using SIT.Tarkov.Core;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
using NetCoreServer;
using Newtonsoft.Json;
using TechHappy.MinimapSender;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TechHappy.MinimapSender
{
    public sealed class MinimapSenderBroadcastService : IDisposable
    {
        private readonly GamePlayerOwner _gamePlayerOwner;
        //private readonly QuestController _questController;
        private readonly Timer _timer;
        private List<QuestMarkerInfo> _quests;
        private GameWorld _gameWorld;

        public MinimapSenderBroadcastService(GamePlayerOwner gamePlayerOwner)
        {
            _gameWorld = Singleton<GameWorld>.Instance;
            _gamePlayerOwner = gamePlayerOwner;

            _timer = new Timer
            {
                AutoReset = true,
                Interval = 250,
            };

            _timer.Elapsed += (sender, args) =>
            {
                try
                {
                    SendData();
                }
                catch (Exception e)
                {
                    MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
                }
            };
        }

        public void StartBroadcastingPosition(double interval = 250)
        {
            _timer.Interval = interval;
            _timer.Start();

            try
            {
                SendData();
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        public void StopBroadcastingPosition()
        {
            _timer.Stop();
        }

        public void ChangeInterval(double interval)
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
                _timer.Interval = interval;
                _timer.Start();
            }
            else
            {
                _timer.Interval = interval;
            }
        }

        private void SendData()
        {
            try
            {
                if (_gamePlayerOwner.Player != null)
                {
                    string mapName = _gamePlayerOwner.Player.Location;
                    Vector3 playerPosition = _gamePlayerOwner.Player.Position;
                    Vector2 playerRotation = _gamePlayerOwner.Player.Rotation;
                    Vector3[] airdrops = { };

                    if (MinimapSenderPlugin.ShowAirdrops.Value == true)
                    {
                        airdrops = MinimapSenderPlugin.airdrops.ToArray();
                    }

                    MinimapSenderPlugin._server.MulticastText(JsonConvert.SerializeObject(new
                    {
                        raidCounter = MinimapSenderPlugin.raidCounter,
                        mapName = mapName,
                        playerRotationX = playerRotation.x,
                        playerPositionX = playerPosition.x,
                        playerPositionZ = playerPosition.z,
                        playerPositionY = playerPosition.y,
                        airdrops = airdrops,
                        quests = MinimapSenderPlugin.ShowQuestMarkers.Value ? _quests : new List<QuestMarkerInfo>() { }
                    }));
                }
            }
            catch (Exception e)
            {
                MinimapSenderPlugin.MinimapSenderLogger.LogError($"Exception {e.GetType()} occured. Message: {e.Message}. StackTrace: {e.StackTrace}");
            }
        }

        public void UpdateQuestData(List<QuestMarkerInfo> questData)
        {
            _quests = questData;
        }
            

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}


class WebsocketSession : WsSession
{
    public WebsocketSession(WsServer server) : base(server) { }

    public override void OnWsConnected(HttpRequest request)
    {
        MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Minimap WebSocket session with Id {Id} connected!");
    }

    public override void OnWsDisconnected()
    {
        MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Minimap WebSocket session with Id {Id} disconnected!");
    }

    public override void OnWsReceived(byte[] buffer, long offset, long size)
    {
        string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
        MinimapSenderPlugin.MinimapSenderLogger.LogInfo("Incoming: " + message);
    }

    protected override void OnError(SocketError error)
    {
        MinimapSenderPlugin.MinimapSenderLogger.LogInfo($"Minimap WebSocket session caught an error with code {error}");
    }
}

class MinimapServer : WsServer
{
    public MinimapServer(IPAddress address, int port) : base(address, port) { }

    protected override TcpSession CreateSession() { return new WebsocketSession(this); }

    protected override void OnError(SocketError error)
    {
        MinimapSenderPlugin.MinimapSenderLogger.LogError($"Minimap WebSocket server caught an error with code {error}");
    }
}

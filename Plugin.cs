using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using BepInEx;
using BepInEx.Logging;
using GrimshireCoop.Messages.Server;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.UIElements.Collections;

namespace GrimshireCoop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static NetworkManager server;
    internal static NetManager client;
    internal static int PORT = 9050;
    internal static int serverPeerId;
    internal static int ClientPeerId;

    public static Dictionary<int, NetworkedBehaviour> NetworkedObjects = [];

    public static Dictionary<string, Type> MessageTypes = new Dictionary<string, Type>
    {
        { "Server.CreatePlayer", typeof(Messages.Server.CreatePlayer) },
        { "Server.CreateGameObject", typeof(Messages.Server.CreateGameObject) },
        { "Shared.Position", typeof(Messages.Shared.Position) },
        { "Shared.Movement", typeof(Messages.Shared.Movement) },
        { "Server.AssignPeerId", typeof(Messages.Server.AssignPeerId) },
    };

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} is loaded!");

        Harmony.CreateAndPatchAll(typeof(Plugin));
    }
    
    [HarmonyPatch(typeof(GameManager), "Start")]
    [HarmonyPrefix]
    static bool OnGameManagerStart(GameManager __instance)
    {
        if (server != null)
        {
            return true;
        }

        Logger.LogInfo("Starting server...");

        var manager = new NetworkManager();
        server = manager;

        // Check if port is in use
        bool isHosting = !IsPortInUse(PORT);
        if (isHosting)
        {
            manager.netManager.Start(PORT);
        }

        StartClient();

        return true;
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }

    private static void StartClient()
    {
        Logger.LogInfo("Starting client...");

        var listener = new EventBasedNetListener();
        var client = new NetManager(listener);
        client.Start();
        client.Connect("localhost", 9050, "GrimshireCoopKey");

        listener.PeerConnectedEvent += (peer) =>
        {
            Logger.LogInfo($"Client connected to server: {peer.Id}");
            Plugin.serverPeerId = peer.Id;
        };

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            Logger.LogInfo($"Client received data from server: {fromPeer.Id} bytes {dataReader.AvailableBytes}");

            var msgType = dataReader.GetString(100);
            Type msgClass = MessageTypes[msgType];

            Message msg = Activator.CreateInstance(msgClass, dataReader) as Message;
            
            // Handle the message
            Logger.LogInfo($"Deserialized message of type: {msg.MessageType}");
            switch (msg)
            {
                case Messages.Server.AssignPeerId assignPeerIdMsg:
                    Plugin.ClientPeerId = assignPeerIdMsg.PeerId;
                    Logger.LogInfo($"Assigned Client Peer ID: {Plugin.ClientPeerId}");
                    break;
                case Messages.Server.CreatePlayer createPlayerMsg:
                    break;
                case Messages.Server.CreateGameObject createGameObjectMsg:
                    CreateNetworkedObject(createGameObjectMsg);
                    break;
                case Messages.Shared.Position positionMsg:
                    NetworkedBehaviour netObj = NetworkedObjects[positionMsg.NetId];
                    netObj.transform.position = new Vector3(positionMsg.PositionX, positionMsg.PositionY, positionMsg.PositionZ);
                    break;
                case Messages.Shared.Movement movementMsg:
                    NetworkedBehaviour movingObj = NetworkedObjects[movementMsg.NetId];
                    
                    // Animate - should be done before setting pos so the facing dir vector is correct
                    if (movingObj is PeerPlayer peerPlayer)
                    {
                        peerPlayer.FaceTowards(new Vector2(movementMsg.NewPositionX, movementMsg.NewPositionY));
                    }

                    movingObj.transform.position = new Vector3(movementMsg.NewPositionX, movementMsg.NewPositionY, movementMsg.NewPositionZ);
                    break;
                default:
                    Logger.LogWarning($"Unknown message type received: {msgType}");
                    break;
            }

            dataReader.Recycle();
        };
        Plugin.client = client;
    }

    private static void CreateNetworkedObject(CreateGameObject createGameObjectMsg)
    {
        NetworkedBehaviour netObj = CreateGameObjectByType(createGameObjectMsg.GameObjectId);

        // Initialize ownership and position
        netObj.SetPeerID(createGameObjectMsg.OwnerPeerId);
        netObj.netId = createGameObjectMsg.NetId;
        netObj.transform.position = new Vector3(createGameObjectMsg.PositionX, createGameObjectMsg.PositionY, createGameObjectMsg.PositionZ);

        // Track it
        NetworkedObjects[netObj.netId] = netObj;
    }

    private static NetworkedBehaviour CreateGameObjectByType(string gameObjectId)
    {
        Logger.LogInfo($"Creating networked GameObject {gameObjectId}");
        switch (gameObjectId)
        {
            case "DummyPlayer":
                GameObject dummyPlayerPrefab = new GameObject("Coop.NetPlayerDummyPrefab");
                DummyPlayer player = dummyPlayerPrefab.AddComponent<DummyPlayer>();
                SpriteRenderer renderer = dummyPlayerPrefab.AddComponent<SpriteRenderer>();

                DontDestroyOnLoad(dummyPlayerPrefab);
                dummyPlayerPrefab.SetActive(true);

                return player;
            case "PeerPlayer":
                PlayerController clientPlayer = GameManager.Instance.Player;
                GameObject peerPlayerObj = new GameObject("Coop.NetPeerPlayer");
                DontDestroyOnLoad(peerPlayerObj);

                GameObject playerSprite = clientPlayer.transform.Find("PlayerSprite").gameObject;
                GameObject peerPlayerSprite = Instantiate(playerSprite, peerPlayerObj.transform);
                peerPlayerSprite.transform.parent = peerPlayerObj.transform;
                peerPlayerSprite.name = "PlayerSprite";

                GameObject playerPlacementDetection = clientPlayer.transform.Find("PlayerPlacementDetection").gameObject;
                GameObject peerPlayerPlacementDetection = Instantiate(playerPlacementDetection, peerPlayerObj.transform);
                peerPlayerPlacementDetection.transform.parent = peerPlayerObj.transform;
                peerPlayerPlacementDetection.name = "PlayerPlacementDetection";

                // TODO set skin

                return peerPlayerObj.AddComponent<PeerPlayer>();
            default:
                Logger.LogWarning($"Unknown GameObjectId to create: {gameObjectId}");
                return null;
        }
    }

    private void LateUpdate()
    {
        // Poll network events
        server?.PollEvents();
        client?.PollEvents();

        // Update networked objects
        foreach (var netObj in NetworkedObjects.Values)
        {
            if (netObj.isDirty)
            {
                netObj.Sync();
            }
        }
    }

    private void OnDestroy()
    {
        server?.Stop();
        client?.Stop();
    }
}

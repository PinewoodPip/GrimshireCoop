using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using BepInEx;
using BepInEx.Logging;
using GrimshireCoop.Messages.Server;
using GrimshireCoop.Messages.Shared;
using GrimshireCoop.Network.Messages;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    internal static int ClientPlayerNetId;
    private static int HomeSceneLoadCount = 0;

    public static string CurrentSceneID => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

    public static int NextFreeNetId
    {
        get
        {
            int maxNetId = 0;
            foreach (var sceneObjects in NetworkedObjects.Values)
            {
                foreach (var netObj in sceneObjects.Values)
                {
                    if (netObj.netId > maxNetId)
                    {
                        maxNetId = netObj.netId;
                    }
                }
            }
            return maxNetId + 1;
        }
    }

    // Maps scene ID to map of netId to NetworkedBehaviour
    public static Dictionary<string, Dictionary<int, NetworkedBehaviour>> NetworkedObjects = [];

    public static Dictionary<int, string> PeerScenes = new Dictionary<int, string>();

    public static Dictionary<string, Type> MessageTypes = new Dictionary<string, Type>
    {
        { "Server.CreatePlayer", typeof(Messages.Server.CreatePlayer) },
        { "Server.CreateGameObject", typeof(Messages.Server.CreateGameObject) },
        { "Shared.Position", typeof(Messages.Shared.Position) },
        { "Shared.Movement", typeof(Messages.Shared.Movement) },
        { "Shared.StoppedMoving", typeof(Messages.Shared.StoppedMoving) },
        { "Shared.ToolUsed", typeof(Messages.Shared.ToolUsed) },
        { "Shared.FaceDirection", typeof(Messages.Shared.FaceDirection) },
        { "Shared.SceneChanged", typeof(Messages.Shared.SceneChanged) },
        { "Shared.ReplicateObject", typeof(Messages.Shared.ReplicateObject) },
        { "Server.AssignPeerId", typeof(Messages.Server.AssignPeerId) },
    };

    // Messages that should only be handled if they come from a client in the same scene.
    public static HashSet<string> LocalSceneMessages = new HashSet<string>
    {
        "Server.CreatePlayer",
        "Server.CreateGameObject",
        "Shared.Position",
        "Shared.Movement",
        "Shared.StoppedMoving",
        "Shared.ToolUsed",
        "Shared.FaceDirection",
    };

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} is loaded!");

        Harmony.CreateAndPatchAll(typeof(Plugin));

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            LogClient($"Scene loaded: {scene.name}");
            if (scene.name == "Interior_Home_Scene")
            {
                HomeSceneLoadCount++;
                if (HomeSceneLoadCount == 2) // TODO reset on going back to main menu
                {
                    SetupServerAndClient();
                }
            }
        };
    }

    private void SetupServerAndClient()
    {
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
            LogClient($"Client connected to server: {peer.Id}");
            Plugin.serverPeerId = peer.Id;
        };

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            LogClient($"Received data from server: {dataReader.AvailableBytes} bytes");

            var msgType = dataReader.GetString(100);
            Type msgClass = MessageTypes[msgType];

            Message msg = Activator.CreateInstance(msgClass, dataReader) as Message;
            
            // Handle the message
            LogClient($"Deserialized message of type: {msg.MessageType}");

            // Interest management: ignore messages from clients in other scenes)
            if (LocalSceneMessages.Contains(msg.MessageType) && msg is OwnedMessage ownedMessage && PeerScenes[ownedMessage.OwnerPeerId] != CurrentSceneID)
            {
                LogClient($"Ignoring message {msg.MessageType} from peer {ownedMessage.OwnerPeerId} in scene {PeerScenes[ownedMessage.OwnerPeerId]} (current scene {CurrentSceneID})");
                return;
            }

            switch (msg)
            {
                case Messages.Server.AssignPeerId assignPeerIdMsg:
                    ClientPeerId = assignPeerIdMsg.PeerId;
                    LogClient($"Assigned Client Peer ID: {Plugin.ClientPeerId}");
                    foreach (var kvp in assignPeerIdMsg.PeerScenes)
                    {
                        PeerScenes[kvp.Key] = kvp.Value;
                        LogClient($"Peer {kvp.Key} is in scene {kvp.Value}");
                    }
                    break;
                case Messages.Server.CreatePlayer createPlayerMsg:
                    break;
                case Messages.Shared.ReplicateObject replicateObjectMsg:
                    LogClient($"Client received ReplicateObject message for netId {replicateObjectMsg.NetId} of type {replicateObjectMsg.GameObjectId} from peer {replicateObjectMsg.OwnerPeerId}");

                    // Only replicate if the message is for this client
                    if (replicateObjectMsg.TargetPeerId != Plugin.ClientPeerId)
                    {
                        LogClient($"Ignoring ReplicateObject message for peer {replicateObjectMsg.TargetPeerId} (current peer {Plugin.ClientPeerId})");
                        break;
                    }
                    LogClient($"[Client] Replicating object netId {replicateObjectMsg.NetId} of type {replicateObjectMsg.GameObjectId} from peer {replicateObjectMsg.OwnerPeerId}");
                    CreateNetworkedObject(new CreateGameObject
                    {
                        GameObjectId = replicateObjectMsg.GameObjectId,
                        NetId = replicateObjectMsg.NetId,
                        OwnerPeerId = replicateObjectMsg.OwnerPeerId,
                        PositionX = replicateObjectMsg.PositionX,
                        PositionY = replicateObjectMsg.PositionY,
                        PositionZ = replicateObjectMsg.PositionZ
                    });
                    break;
                case Messages.Server.CreateGameObject createGameObjectMsg:
                    CreateNetworkedObject(createGameObjectMsg);
                    break;
                case Messages.Shared.Position positionMsg:
                    NetworkedBehaviour netObj = Plugin.GetByNetID(positionMsg.NetId);
                    netObj.transform.position = new Vector3(positionMsg.PositionX, positionMsg.PositionY, positionMsg.PositionZ);
                    break;
                case Messages.Shared.Movement movementMsg:
                    NetworkedBehaviour movingObj = Plugin.GetByNetID(movementMsg.NetId);
                    // Animate - should be done before setting pos so the facing dir vector is correct
                    if (movingObj is PeerPlayer peerPlayer)
                    {
                        peerPlayer.AnimateWalkTowards(new Vector2(movementMsg.NewPositionX, movementMsg.NewPositionY));
                    }

                    movingObj.transform.position = new Vector3(movementMsg.NewPositionX, movementMsg.NewPositionY, movementMsg.NewPositionZ);
                    break;
                case Messages.Shared.StoppedMoving stoppedMovingMsg:
                    NetworkedBehaviour stoppedObj = GetByNetID(stoppedMovingMsg.NetId);
                    if (stoppedObj is PeerPlayer stoppedPeerPlayer)
                    {
                        stoppedPeerPlayer.OnStoppedMoving();
                    }
                    break;
                case Messages.Shared.ToolUsed toolUsedMsg:
                    NetworkedBehaviour toolUserObj = GetByNetID(toolUsedMsg.NetId);
                    if (toolUserObj is PeerPlayer toolUserPeerPlayer)
                    {
                        toolUserPeerPlayer.PlayToolUseAnimation(toolUsedMsg.ToolId);
                    }
                    break;
                case Messages.Shared.FaceDirection faceDirectionMsg:
                    NetworkedBehaviour facingObj = GetByNetID(faceDirectionMsg.NetId);
                    Debug.Log($"FaceDirection message received for netId {faceDirectionMsg.NetId} dirX {faceDirectionMsg.PosX} dirY {faceDirectionMsg.PosY}");
                    if (facingObj is PeerPlayer facingPeerPlayer)
                    {
                        facingPeerPlayer.FaceTowards(new Vector2(faceDirectionMsg.PosX, faceDirectionMsg.PosY));
                    }
                    break;
                case Messages.Shared.SceneChanged sceneChangedMsg:
                {
                    // Delete previous PeerPlayer
                    PeerPlayer[] allPeers = FindObjectsOfType<PeerPlayer>();
                    foreach (var peer in allPeers)
                    {
                        if (peer.peerId == sceneChangedMsg.OwnerPeerId)
                        {
                            Plugin.UnregisterNetObject(peer, SceneManager.GetActiveScene().name);
                            DestroyImmediate(peer.gameObject); // TODO extract method
                        }
                    }

                    // Recreate peer player if they moved to client's scene
                    if (sceneChangedMsg.SceneId == CurrentSceneID)
                    {
                        CreateNetworkedObject(new CreateGameObject
                        {
                            GameObjectId = "PeerPlayer",
                            NetId = sceneChangedMsg.ClientPlayerNetId,
                            OwnerPeerId = sceneChangedMsg.OwnerPeerId,
                            PositionX = sceneChangedMsg.PositionX,
                            PositionY = sceneChangedMsg.PositionY,
                            PositionZ = sceneChangedMsg.PositionZ
                        });
                    }

                    int peerId = sceneChangedMsg.OwnerPeerId;
                    PeerScenes[peerId] = sceneChangedMsg.SceneId;
                    LogClient($"Peer {peerId} changed to scene {sceneChangedMsg.SceneId}");

                    // Replicate owned objects to the peer
                    foreach (var ownedObj in GetOwnedSceneObjects())
                    {
                        // Send create game object message
                        string objectTypeID = ownedObj.NetTypeID == "ClientPlayer" ? "PeerPlayer" : ownedObj.NetTypeID;
                        ReplicateObject replicateMsg = new() // TODO special type of message
                        {
                            OwnerPeerId = ownedObj.peerId,
                            NetId = ownedObj.netId,
                            GameObjectId = objectTypeID,
                            PositionX = ownedObj.transform.position.x,
                            PositionY = ownedObj.transform.position.y,
                            PositionZ = ownedObj.transform.position.z,
                            SceneId = CurrentSceneID,
                            TargetPeerId = peerId,
                        };
                        LogClient($"SceneChanged client handler: Replicating object netId {ownedObj.netId} of type {objectTypeID} to peer {peerId} scene {CurrentSceneID}");

                        NetDataWriter writer = new NetDataWriter();
                        replicateMsg.Serialize(writer);
                        fromPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                    break;
                }
                default:
                    LogClientWarning($"Unknown message type received: {msgType}");
                    break;
            }

            dataReader.Recycle();
        };
        Plugin.client = client;
    }

    public static void LogClient(string message)
    {
        Logger.LogInfo($"[Client] {message}");
    }

    public static void LogClientWarning(string message)
    {
        Logger.LogWarning($"[Client] {message}");
    }

    private static void CreateNetworkedObject(CreateGameObject createGameObjectMsg)
    {
        NetworkedBehaviour netObj = CreateGameObjectByType(createGameObjectMsg.GameObjectId, createGameObjectMsg.OwnerPeerId);

        // Initialize ownership and position
        netObj.SetPeerID(createGameObjectMsg.OwnerPeerId);
        netObj.netId = createGameObjectMsg.NetId;
        netObj.transform.position = new Vector3(createGameObjectMsg.PositionX, createGameObjectMsg.PositionY, createGameObjectMsg.PositionZ);

        // Track net ID of the client player object
        if (netObj is ClientPlayer)
        {
            ClientPlayerNetId = netObj.netId;
        }

        // Track it
        RegisterNetObject(netObj, CurrentSceneID);
    }

    public static void ChangeNetObjectScene(NetworkedBehaviour netObj, string oldScene, string newScene)
    {
        Logger.LogInfo($"Changing net object netId {netObj.netId} {netObj.name} from scene {oldScene} to scene {newScene}");

        // Unregister from current scene
        UnregisterNetObject(netObj, oldScene);

        // Register in new scene
        RegisterNetObject(netObj, newScene);
    }

    public static void RegisterNetObject(NetworkedBehaviour netObj, string scene)
    {
        if (!NetworkedObjects.ContainsKey(scene))
        {
            NetworkedObjects[scene] = new Dictionary<int, NetworkedBehaviour>();
        }
        NetworkedObjects[scene][netObj.netId] = netObj;
    }

    public static void UnregisterNetObject(NetworkedBehaviour netObj, string scene)
    {
        // Print all keys
        foreach (var key in NetworkedObjects.Keys)
        {
            Logger.LogInfo($"Registered scene: {key}, amount of objects: {NetworkedObjects[key].Count}");
        }
        Debug.Log($"Unregistering net object netId {netObj.netId} {netObj.name} from scene {scene}");
        var sceneObjects = NetworkedObjects[scene];
        if (sceneObjects.ContainsKey(netObj.netId))
        {
            sceneObjects.Remove(netObj.netId);
        }
        else
        {
            LogClientWarning($"Attempted to unregister net object {netObj.netId} that is not registered in scene {scene}");
        }
    }

    private static NetworkedBehaviour CreateGameObjectByType(string gameObjectId, int ownerPeerId)
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
                if (ownerPeerId == Plugin.ClientPeerId)
                {
                    // Wrap the local PlayerController with the ClientPlayer component
                    PlayerController clientPlayer = GameManager.Instance.Player;
                    ClientPlayer clientPlayerNetObj = clientPlayer.gameObject.AddComponent<ClientPlayer>();
                    return clientPlayerNetObj;
                }
                else
                {
                    // Create a PeerPlayer for other peers
                    PlayerController clientPlayer = GameManager.Instance.Player;
                    GameObject peerPlayerObj = new GameObject("Coop.NetPeerPlayer");
                    // DontDestroyOnLoad(peerPlayerObj);

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
                }
            default:
                LogClientWarning($"Unknown GameObjectId to create: {gameObjectId}");
                return null;
        }
    }

    public static NetworkedBehaviour GetByNetID(int netId)
    {
        foreach (var sceneObjects in NetworkedObjects.Values)
        {
            if (sceneObjects.TryGetValue(netId, out NetworkedBehaviour netObj))
            {
                return netObj;
            }
        }
        return null;
    }

    public static Dictionary<int, NetworkedBehaviour> GetCurrentSceneNetworkedObjects()
    {
        if (!NetworkedObjects.ContainsKey(CurrentSceneID))
        {
            NetworkedObjects[CurrentSceneID] = new Dictionary<int, NetworkedBehaviour>();
        }
        return NetworkedObjects[CurrentSceneID];
    }

    public static List<NetworkedBehaviour> GetOwnedSceneObjects()
    {
        List<NetworkedBehaviour> ownedObjects = new List<NetworkedBehaviour>();
        var sceneObjects = GetCurrentSceneNetworkedObjects();
        foreach (var netObj in sceneObjects.Values)
        {
            if (netObj.peerId == ClientPeerId)
            {
                ownedObjects.Add(netObj);
            }
        }
        return ownedObjects;
    }

    private void LateUpdate()
    {
        // Poll network events
        server?.PollEvents();
        client?.PollEvents();

        // Update networked objects of the current scene
        // Only the owners of objects are expected to mark them as dirty,
        // thus this syncs the client's objects to server
        Dictionary<int, NetworkedBehaviour> currentSceneObjects = GetCurrentSceneNetworkedObjects();
        foreach (var netObj in currentSceneObjects.Values)
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

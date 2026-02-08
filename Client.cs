
using System;
using System.Collections.Generic;
using BepInEx.Logging;
using GrimshireCoop.Messages;
using GrimshireCoop.Messages.Client;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GrimshireCoop.Utils;

namespace GrimshireCoop;

public class Client
{
    public static string CurrentSceneID => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    public static NetId ClientPlayerNetId;

    public static Client Instance { get; private set; }
    public PeerId ClientPeerId { get; private set; }
    public static Dictionary<PeerId, string> PeerScenes => Plugin.PeerScenes;
    public NetPeer ServerPeer => netManager?.FirstPeer;

    private static readonly ManualLogSource Logger = Plugin.Logger;

    private NetManager netManager;

    public Client()
    {
        if (Instance != null)
        {
            throw new System.Exception("Client instance already exists");
        }
        Instance = this;
    }

    public void PollEvents()
    {
        netManager.PollEvents();
    }

    public void StartClient()
    {
        Log("Starting client...");

        var listener = new EventBasedNetListener();
        netManager = new NetManager(listener);
        netManager.Start();
        netManager.Connect("localhost", Server.PORT, Server.CONNECTION_KEY); // TODO configurable address

        listener.PeerConnectedEvent += (peer) =>
        {
            Log($"Client connected to server: {peer.Id}");
            Plugin.serverPeerId = peer.Id;
        };

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            Log($"Received data from server: {dataReader.AvailableBytes} bytes");

            var msgType = dataReader.GetString(100);
            Type msgClass = Plugin.MessageTypes[msgType];

            Message msg = Activator.CreateInstance(msgClass, dataReader) as Message;
            
            // Handle the message
            Log($"Deserialized message of type: {msg.MessageType}");

            // Sanity check for sync direction
            if (msg.SyncDirection == Message.Direction.ClientToServer)
            {
                throw new Exception($"[Client] Received message with ClientToServer direction: {msgType}");
            }

            // Interest management: ignore messages from clients in other scenes)
            if (Plugin.LocalSceneMessages.Contains(msg.MessageType) && msg is OwnedMessage ownedMessage && Plugin.PeerScenes[ownedMessage.OwnerPeerId] != CurrentSceneID)
            {
                Log($"Ignoring message {msg.MessageType} from peer {ownedMessage.OwnerPeerId} in scene {Plugin.PeerScenes[ownedMessage.OwnerPeerId]} (current scene {CurrentSceneID})");
                return;
            }

            // Fetch the target net object if applicable
            NetworkedBehaviour netObj = null;
            if (msg is NetObjectMessage netObjectMsg)
            {
                netObj = GetByNetID(netObjectMsg.NetId);
            }

            switch (msg)
            {
                case Messages.Server.AssignPeerId assignPeerIdMsg:
                    ClientPeerId = assignPeerIdMsg.PeerId;
                    Log($"Assigned Client Peer ID: {ClientPeerId}");
                    foreach (var kvp in assignPeerIdMsg.PeerScenes)
                    {
                        PeerScenes[kvp.Key] = kvp.Value;
                        Log($"Peer {kvp.Key} is in scene {kvp.Value}");
                    }
                    break;
                case Messages.Server.CreatePlayer createPlayerMsg:
                    break;
                case Messages.Client.ReplicateObject replicateObjectMsg:
                    // Only replicate if the message is for this client
                    int targetPeerId = replicateObjectMsg.TargetPeerId;
                    if (targetPeerId != -1 && targetPeerId != ClientPeerId)
                    {
                        Log($"Ignoring ReplicateObject message for peer {replicateObjectMsg.TargetPeerId} (current peer {ClientPeerId})");
                        break;
                    }
                    Log($"[Client] Replicating object netId {replicateObjectMsg.NetId} of type {replicateObjectMsg.GameObjectId} from peer {replicateObjectMsg.OwnerPeerId}");
                    NetworkedBehaviour replicatedObj = CreateNetworkedObject(new CreateGameObject
                    {
                        GameObjectId = replicateObjectMsg.GameObjectId,
                        NetId = replicateObjectMsg.NetId,
                        OwnerPeerId = replicateObjectMsg.OwnerPeerId,
                        Position = replicateObjectMsg.Position
                    });
                    replicatedObj.ApplyReplicationData(replicateObjectMsg.ReplicationData);
                    break;
                case Messages.Client.CreateGameObject createGameObjectMsg:
                    CreateNetworkedObject(createGameObjectMsg);
                    break;
                case Messages.Client.Position positionMsg:
                    netObj.transform.position = positionMsg.Pos;
                    break;
                case Messages.Client.Movement movementMsg:
                    // Animate - should be done before setting pos so the facing dir vector is correct
                    if (netObj is PeerPlayer peerPlayer)
                    {
                        peerPlayer.AnimateWalkTowards(new Vector2(movementMsg.NewPosition.x, movementMsg.NewPosition.y));
                    }

                    netObj.transform.position = movementMsg.NewPosition;
                    break;
                case Messages.Client.StoppedMoving stoppedMovingMsg:
                    if (netObj is PeerPlayer stoppedPeerPlayer)
                    {
                        stoppedPeerPlayer.OnStoppedMoving();
                    }
                    break;
                case Messages.Client.ToolUsed toolUsedMsg:
                    if (netObj is PeerPlayer toolUserPeerPlayer)
                    {
                        toolUserPeerPlayer.PlayToolUseAnimation(toolUsedMsg.ToolId);
                    }
                    break;
                case Messages.Client.FaceDirection faceDirectionMsg:
                    Debug.Log($"FaceDirection message received for netId {faceDirectionMsg.NetId} dirX {faceDirectionMsg.PosX} dirY {faceDirectionMsg.PosY}");
                    if (netObj is PeerPlayer facingPeerPlayer)
                    {
                        facingPeerPlayer.FaceTowards(new Vector2(faceDirectionMsg.PosX, faceDirectionMsg.PosY));
                    }
                    break;
                case Messages.Client.SetHeldItem setHeldItemMsg:
                    PeerPlayer heldItemPeerPlayer = netObj as PeerPlayer;
                    heldItemPeerPlayer.SetHeldItem(setHeldItemMsg.ItemId);
                    break;
                case Messages.Client.ObjectAction objectActionMsg: // Will handle derived msgs as well.
                    netObj.OnAction(objectActionMsg);
                    break;
                case Messages.Host.SetRandomSeed setRandomSeedMsg:
                    UnityEngine.Random.state = setRandomSeedMsg.RandomState;
                    break;
                case Messages.Client.TileMapAction tileMapActionMsg:
                    NetTileMapManager.HandleTileMapAction(tileMapActionMsg);
                    break;
                case Messages.Client.SceneChanged sceneChangedMsg:
                {
                    // Delete previous PeerPlayer
                    PeerPlayer[] allPeers = GameObject.FindObjectsOfType<PeerPlayer>();
                    foreach (var peer in allPeers)
                    {
                        if (peer.peerId == sceneChangedMsg.OwnerPeerId)
                        {
                            Plugin.UnregisterNetObject(peer, SceneManager.GetActiveScene().name);
                            GameObject.DestroyImmediate(peer.gameObject); // TODO extract method
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
                            Position = sceneChangedMsg.Position
                        });
                    }

                    int peerId = sceneChangedMsg.OwnerPeerId;
                    PeerScenes[peerId] = sceneChangedMsg.SceneId;
                    Log($"Peer {peerId} changed to scene {sceneChangedMsg.SceneId}");

                    // Replicate owned objects to the peer
                    if (sceneChangedMsg.SceneId == CurrentSceneID)
                    {
                        foreach (var ownedObj in Plugin.GetOwnedSceneObjects())
                        {
                            // Send create game object message
                            string objectTypeID = ownedObj.NetTypeID == "ClientPlayer" ? "PeerPlayer" : ownedObj.NetTypeID;
                            ReplicateObject replicateMsg = new() // TODO special type of message
                            {
                                OwnerPeerId = ownedObj.peerId,
                                NetId = ownedObj.netId,
                                GameObjectId = objectTypeID,
                                Position = ownedObj.transform.position,
                                SceneId = CurrentSceneID,
                                TargetPeerId = peerId,
                                ReplicationData = ownedObj.GetReplicationData()
                            };
                            Log($"SceneChanged: Replicating object netId {ownedObj.netId} of type {objectTypeID} to peer {peerId} scene {CurrentSceneID}");

                            NetDataWriter writer = new NetDataWriter();
                            replicateMsg.Serialize(writer);
                            fromPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                        }
                    }
                    break;
                }
                default:
                    LogWarning($"Unknown message type received: {msgType}");
                    break;
            }

            dataReader.Recycle();
        };
    }
    
    private NetworkedBehaviour CreateGameObjectByType(string gameObjectId, int ownerPeerId)
    {
        Log($"Creating networked GameObject {gameObjectId}");
        switch (gameObjectId)
        {
            case "DummyPlayer":
                GameObject dummyPlayerPrefab = new GameObject("Coop.NetPlayerDummyPrefab");
                DummyPlayer player = dummyPlayerPrefab.AddComponent<DummyPlayer>();
                SpriteRenderer renderer = dummyPlayerPrefab.AddComponent<SpriteRenderer>();

                GameObject.DontDestroyOnLoad(dummyPlayerPrefab);
                dummyPlayerPrefab.SetActive(true);

                return player;
            case "PeerPlayer":
                if (ownerPeerId == ClientPeerId)
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

                    GameObject playerSprite = clientPlayer.transform.Find("PlayerSprite").gameObject;
                    GameObject peerPlayerSprite = GameObject.Instantiate(playerSprite, peerPlayerObj.transform);
                    peerPlayerSprite.transform.parent = peerPlayerObj.transform;
                    peerPlayerSprite.name = "PlayerSprite";

                    GameObject playerPlacementDetection = clientPlayer.transform.Find("PlayerPlacementDetection").gameObject;
                    GameObject peerPlayerPlacementDetection = GameObject.Instantiate(playerPlacementDetection, peerPlayerObj.transform);
                    peerPlayerPlacementDetection.transform.parent = peerPlayerObj.transform;
                    peerPlayerPlacementDetection.name = "PlayerPlacementDetection";

                    // TODO set skin

                    return peerPlayerObj.AddComponent<PeerPlayer>();
                }
            case "TreeObject":
                // TODO track in TreeManager
                PersistentTreeManager treeManager = GameObject.FindObjectOfType<PersistentTreeManager>();
                GameObject prefab = GetField<GameObject>(treeManager, "treeObjPrefab");
                var instance = GameObject.Instantiate(prefab);
                return instance.AddComponent<Components.TreeObject>();
            case "CropObject":
                CropManager cropManager = GameManager.Instance.CropManager;
                GameObject cropPrefab = GetField<GameObject>(cropManager, "cropObjPrefab");
                var cropInstance = GameObject.Instantiate(cropPrefab);
                return cropInstance.AddComponent<Components.NetCropObject>();
            default:
                LogWarning($"Unknown GameObjectId to create: {gameObjectId}");
                return null;
        }
    }

    private NetworkedBehaviour CreateNetworkedObject(Messages.Client.CreateGameObject createGameObjectMsg)
    {
        NetworkedBehaviour netObj = CreateGameObjectByType(createGameObjectMsg.GameObjectId, createGameObjectMsg.OwnerPeerId);

        // Initialize ownership and position
        netObj.SetPeerID(createGameObjectMsg.OwnerPeerId);
        netObj.netId = createGameObjectMsg.NetId;
        netObj.transform.position = createGameObjectMsg.Position;

        // Track net ID of the client player object
        if (netObj is ClientPlayer)
        {
            ClientPlayerNetId = netObj.netId;
        }

        // Track it
        Plugin.RegisterNetObject(netObj, CurrentSceneID);

        return netObj;    
    }

    public static T CreateNetObject<T>(GameObject originalObj) where T : NetworkedBehaviour
    {
        T netObj = WrapNetObject(originalObj.AddComponent<T>(), Client.Instance.ClientPeerId, Plugin.NextFreeNetId) as T; // TODO have this consume the next free netId from the plugin
        ReplicateObject msg = new()
        {
            OwnerPeerId = netObj.peerId,
            NetId = netObj.netId,
            GameObjectId = netObj.NetTypeID,
            Position = originalObj.transform.position,
            SceneId = Client.CurrentSceneID,
            TargetPeerId = -1,
            ReplicationData = netObj.GetReplicationData(),
        };
        SendMsg(msg);
        return netObj;
    }

    // Wraps an existing object with a networked behaviour or returns its existing net behaviour.
    public static T TryCreateNetObject<T>(GameObject originalObj) where T : NetworkedBehaviour
    {
        T existingNetObj = originalObj.GetComponent<T>();
        return existingNetObj ?? CreateNetObject<T>(originalObj);
    }

    private static NetworkedBehaviour WrapNetObject(NetworkedBehaviour netObj, PeerId ownerPeerId, NetId netId)
    {
        netObj.SetPeerID(ownerPeerId);
        netObj.netId = netId;

        // Track it
        Plugin.RegisterNetObject(netObj, CurrentSceneID);

        return netObj;
    }

    public static void SendMsg(Message msg)
    {
        NetDataWriter writer = new NetDataWriter();
        msg.Serialize(writer);
        Plugin.client.ServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public NetworkedBehaviour GetByNetID(NetId netId)
    {
        return Plugin.GetByNetID(netId);
    }
    
    public static void Log(string message)
    {
        Logger.LogInfo($"[Client {Instance.ClientPeerId}] {message}");
    }

    public static void LogWarning(string message)
    {
        Logger.LogWarning($"[Client {Instance.ClientPeerId}] {message}");
    }

    public void Dispose()
    {
        netManager.Stop();
        Instance = null;
    }
}

using System;
using System.Collections.Generic;
using BepInEx.Logging;
using GrimshireCoop.Messages;
using GrimshireCoop.Messages.Client;
using GrimshireCoop.Messages.Server;
using GrimshireCoop.Messages.Host;
using GrimshireCoop.MessageHandlers;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GrimshireCoop.Utils;

namespace GrimshireCoop;

/// <summary>
/// Client singleton; handles net messages to act on the networked game objects.
/// </summary>
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
    private Dictionary<Type, Action<Client, Message>> messageHandlers = [];
    private Dictionary<string, Func<int, NetBehaviour>> netObjectCreators = [];

    public Client()
    {
        if (Instance != null)
        {
            throw new System.Exception("Client instance already exists");
        }
        Instance = this;

        // Register message handlers & net object factory methods
        RegisterSystemMessageHandlers();
        GameObjectHandlers.RegisterHandlers(this);
        PlayerHandlers.RegisterHandlers(this);
        GameManagerHandlers.RegisterHandlers(this);
        RegisterNetObjectCreators();
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
            var msgType = dataReader.GetString(100);
            Type msgClass = Plugin.MessageTypes[msgType];

            Message msg = Activator.CreateInstance(msgClass, dataReader) as Message;
            
            // Handle the message
            if (LoggingConfig.Instance.ShouldLogNetMsg(msg.MessageType))
            {
                Log($"Deserialized message of type: {msg.MessageType}");
            }

            // Sanity check for sync direction
            if (msg.SyncDirection == Message.Direction.ClientToServer)
            {
                throw new Exception($"[Client] Received message with ClientToServer direction: {msgType}");
            }

            // Fetch the target net object if applicable
            NetBehaviour netObj = null;
            if (msg is NetObjectMessage netObjectMsg)
            {
                netObj = GetByNetID(netObjectMsg.NetId);
            }

            // Handle the message
            if (messageHandlers.TryGetValue(msg.GetType(), out var handler))
            {
                handler(this, msg);
            }
            else
            {
                LogWarning($"Unknown message type received: {msgType}");
            }

            dataReader.Recycle();
        };
    }
    
    private NetBehaviour CreateGameObjectByType(string gameObjectId, PeerId ownerPeerId)
    {
        NetBehaviour netObj = null;
        if (netObjectCreators.TryGetValue(gameObjectId, out var creator))
        {
            netObj = creator(ownerPeerId);
        }
        else
        {
            LogWarning($"Unknown GameObjectId to create: {gameObjectId}; register a creator method for it with RegisterNetObjectCreator()");
        }
        return netObj;
    }

    /// <summary>
    /// Registers a handler that will be invoked when receiving a net message of the *exact* given message type (will not fire for base/derived net msgs).
    /// </summary>
    public void RegisterMessageHandler<T>(Action<Client, T> handler) where T : Message
    {
        messageHandlers[typeof(T)] = (client, msg) => handler(client, (T)msg); // Extra delegate is used just to avoid verbosity from explicit casts, overhead is minimal
    }

    /// <summary>
    /// Registers a factory method for instantiating a NetworkedBehaviour.
    /// </summary>
    /// <param name="creator">First param is owner peer ID.</param>
    public void RegisterNetObjectCreator(string gameObjectId, Func<PeerId, NetBehaviour> creator)
    {
        netObjectCreators[gameObjectId] = creator;
    }

    private void RegisterNetObjectCreators()
    {
        // Players are a special case: the client must create a ClientPlayer to wrap its PlayerController singleton, while other peers are represented by PeerPlayer.
        RegisterNetObjectCreator("PeerPlayer", ownerPeerId => {
            return ownerPeerId == ClientPeerId ? ClientPlayer.Instantiate() : PeerPlayer.Instantiate();
        });
        RegisterNetObjectCreator("TreeObject", ownerPeerId => Components.NetTreeObject.Instantiate());
        RegisterNetObjectCreator("CropObject", ownerPeerId => Components.NetCropObject.Instantiate());
        RegisterNetObjectCreator("VacuumItem", ownerPeerId => Components.NetVacuumItem.Instantiate());
    }

    private void RegisterSystemMessageHandlers()
    {
        // client param can be ignored here since it will always refer to `this`
        RegisterMessageHandler<AssignPeerId>((client, msg) => HandleAssignPeerIdMsg(msg));
        RegisterMessageHandler<ReplicateObject>((client, msg) => HandleReplicateObjectMsg(msg));
        RegisterMessageHandler<CreateGameObject>((client, msg) => HandleCreateGameObjectMsg(msg));
        RegisterMessageHandler<SetRandomSeed>((client, msg) => HandleSetRandomSeedMsg(msg));
        RegisterMessageHandler<SceneChanged>((client, msg) => HandleSceneChangedMsg(msg));
    }

    private void HandleAssignPeerIdMsg(AssignPeerId msg)
    {
        ClientPeerId = msg.PeerId;
        Log($"Assigned Client Peer ID: {ClientPeerId}");
    }

    private void HandleReplicateObjectMsg(ReplicateObject msg)
    {
        int targetPeerId = msg.TargetPeerId;
        if (targetPeerId != -1 && targetPeerId != ClientPeerId)
        {
            Log($"Ignoring ReplicateObject message for peer {msg.TargetPeerId} (current peer {ClientPeerId})");
            return;
        }
        Log($"Replicating object netId {msg.NetId} of type {msg.GameObjectId} from peer {msg.OwnerPeerId}");
        NetBehaviour replicatedObj = CreateNetworkedObject(new CreateGameObject
        {
            GameObjectId = msg.GameObjectId,
            NetId = msg.NetId,
            OwnerPeerId = msg.OwnerPeerId,
            Position = msg.Position
        });
        replicatedObj.ApplyReplicationData(msg.ReplicationData);
    }

    private void HandleCreateGameObjectMsg(CreateGameObject msg)
    {
        CreateNetworkedObject(msg);
    }

    private void HandleSetRandomSeedMsg(SetRandomSeed msg)
    {
        UnityEngine.Random.state = msg.RandomState;
    }

    private void HandleSceneChangedMsg(SceneChanged msg)
    {
        PeerPlayer[] allPeers = GameObject.FindObjectsOfType<PeerPlayer>();
        foreach (var peer in allPeers)
        {
            if (peer.peerId == msg.OwnerPeerId)
            {
                Plugin.UnregisterNetObject(peer, SceneManager.GetActiveScene().name);
                GameObject.DestroyImmediate(peer.gameObject);
            }
        }

        if (msg.SceneId == CurrentSceneID)
        {
            CreateNetworkedObject(new CreateGameObject
            {
                GameObjectId = "PeerPlayer",
                NetId = msg.ClientPlayerNetId,
                OwnerPeerId = msg.OwnerPeerId,
                Position = msg.Position
            });
        }

        int peerId = msg.OwnerPeerId;
        PeerScenes[peerId] = msg.SceneId;
        Log($"Peer {peerId} changed to scene {msg.SceneId}");

        if (msg.SceneId == CurrentSceneID)
        {
            foreach (var ownedObj in Plugin.GetOwnedSceneObjects())
            {
                string objectTypeID = ownedObj.NetTypeID == "ClientPlayer" ? "PeerPlayer" : ownedObj.NetTypeID;
                ReplicateObject replicateMsg = NetMessagePool.Get<ReplicateObject>();
                replicateMsg.OwnerPeerId = ownedObj.peerId;
                replicateMsg.NetId = ownedObj.netId;
                replicateMsg.GameObjectId = objectTypeID;
                replicateMsg.Position = ownedObj.transform.position;
                replicateMsg.SceneId = CurrentSceneID;
                replicateMsg.TargetPeerId = peerId;
                replicateMsg.ReplicationData = ownedObj.GetReplicationData();
                Log($"SceneChanged: Replicating object netId {ownedObj.netId} of type {objectTypeID} to peer {peerId} scene {CurrentSceneID}");

                NetDataWriter writer = new NetDataWriter();
                replicateMsg.Serialize(writer);
                ServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                NetMessagePool.Release(replicateMsg);
            }
        }
    }

    private NetBehaviour CreateNetworkedObject(Messages.Client.CreateGameObject createGameObjectMsg)
    {
        NetBehaviour netObj = CreateGameObjectByType(createGameObjectMsg.GameObjectId, createGameObjectMsg.OwnerPeerId);

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

    public static T CreateNetObject<T>(GameObject originalObj) where T : NetBehaviour
    {
        T netObj = WrapNetObject(originalObj.AddComponent<T>(), Client.Instance.ClientPeerId, Plugin.NextFreeNetId) as T; // TODO have this consume the next free netId from the plugin
        ReplicateObject msg = NetMessagePool.Get<ReplicateObject>();
        msg.GameObjectId = netObj.NetTypeID;
        msg.OwnerPeerId = netObj.peerId;
        msg.NetId = netObj.netId;
        msg.GameObjectId = netObj.NetTypeID;
        msg.Position = originalObj.transform.position;
        msg.SceneId = CurrentSceneID;
        msg.TargetPeerId = -1;
        msg.ReplicationData = netObj.GetReplicationData();
        SendMsg(msg);
        return netObj;
    }

    // Wraps an existing object with a networked behaviour or returns its existing net behaviour.
    public static T TryCreateNetObject<T>(GameObject originalObj) where T : NetBehaviour
    {
        T existingNetObj = originalObj.GetComponent<T>();
        return existingNetObj ?? CreateNetObject<T>(originalObj);
    }

    private static NetBehaviour WrapNetObject(NetBehaviour netObj, PeerId ownerPeerId, NetId netId)
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

    public NetBehaviour GetByNetID(NetId netId)
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

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using BepInEx.Logging;
using GrimshireCoop.Messages;
using GrimshireCoop.Messages.Client;
using GrimshireCoop.Messages.Server;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrimshireCoop;

public class Server : INetEventListener
{
    public static readonly string CONNECTION_KEY = "GrimshireCoop";
    public static readonly SceneId STARTING_SCENE = "Interior_Home_Scene"; // Scene new clients start in.
    public static readonly int PORT = 9050;
    public static readonly int MAX_PLAYERS = 2; // TODO allow more

    internal NetManager netManager;

    private Dictionary<PeerId, NetPeer> peers = [];

    private NetDataWriter writer;
    private ManualLogSource Logger = Plugin.Logger;

    public Server()
    {
        netManager = new NetManager(this);
        writer = new NetDataWriter();
    }

    public void PollEvents()
    {
        netManager.PollEvents();
    }

    // Connect to server
    public void ConnectToServer(string address)
    {
        netManager.Connect(address, PORT, CONNECTION_KEY);
        Logger.LogInfo($"Connecting to {address}:{PORT}");
    }

    public void Stop()
    {
        netManager.Stop();
        LogServer("NetworkManager stopped.");
    }

    // INetEventListener implementations
    public void OnPeerConnected(NetPeer peer)
    {
        LogServer($"Peer connected: {peer.Id}");
        peers[peer.Id] = peer;
        Plugin.PeerScenes[peer.Id] = STARTING_SCENE; // TODO do we want peers to be able to join from another scene?

        // Assign Peer ID
        SendMsg(peer.Id, new AssignPeerId
        {
            PeerId = peer.Id,
            PeerScenes = new Dictionary<PeerId, SceneId>(Plugin.PeerScenes)
        });

        // Replicate existing objects for the new peer
        // TODO replication utilities to sync state of those objects
        foreach (var netObj in Plugin.GetCurrentSceneNetworkedObjects().Values)
        {
            // Send create game object message
            string objectTypeID = netObj.NetTypeID == "ClientPlayer" ? "PeerPlayer" : netObj.NetTypeID;
            SendMsg(peer.Id, new CreateGameObject
            {
                GameObjectId = objectTypeID,
                NetId = netObj.netId,
                OwnerPeerId = netObj.peerId,
                Position = netObj.transform.position
            });
        }

        // Send create player message
        // Clients will track it, including the host through its client connection
        PlayerController playerController = GameManager.Instance.Player;
        Transform playerTransform = playerController.transform;
        SendMsgToAll(new CreateGameObject
        {
            GameObjectId = "PeerPlayer",
            NetId = Plugin.NextFreeNetId,
            OwnerPeerId = peer.Id,
            Position = playerTransform.position
        });
    }

    public void SendMsg(PeerId peerId, Message msg)
    {
        if (peers.TryGetValue(peerId, out NetPeer peer))
        {
            writer.Reset();
            msg.Serialize(writer);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            LogServer($"Sent message {msg.MessageType} to peer {peerId}");
        }
        else
        {
            LogWarning($"Peer {peerId} not found.");
        }
    }

    /// <summary>
    /// Sends a message to all connected peers.
    /// **The message will be freed to the pool afterwards**.
    /// </summary>
    public void SendMsgToAll(Message msg)
    {
        writer.Reset();
        msg.Serialize(writer);
        netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        LogServer($"Sent message {msg.MessageType} to all peers");
        NetMessagePool.Release(msg);
    }

    public void ForwardMsg(OwnedMessage msg)
    {
        string ownerScene = Plugin.GetPeerScene(msg.OwnerPeerId);
        writer.Reset();
        foreach (var peer in netManager.ConnectedPeerList)
        {
            string peerScene = Plugin.GetPeerScene(peer.Id);
            bool isSender = peer.Id == msg.OwnerPeerId;
            bool isPeerInOwnerScene = peerScene == ownerScene;
            bool canForward = !isSender && (!msg.IsLocal || isPeerInOwnerScene); // Interest management: only forward local messages to peers in the same scene as the owner
            if (canForward)
            {
                msg.Serialize(writer);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                if (LoggingConfig.Instance.ShouldLogNetMsg(msg.MessageType))
                {
                    LogServer($"Forwarded message {msg.MessageType} to peer {peer.Id}");
                }
            }
        }
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        LogServer($"Peer disconnected: {peer.Id}, reason: {disconnectInfo.Reason}");
        peers.Remove(peer.Id);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Logger.LogError($"Network error: {socketError}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        // Parse the message
        try
        {
            var msgType = reader.GetString(100);
            Type msgClass = Plugin.MessageTypes[msgType];
            Message msg = Activator.CreateInstance(msgClass, reader) as Message;

            // Forward messages that are meant to be resent to other peers
            if (msg is OwnedMessage ownedMsg && ownedMsg.SyncDirection == Message.Direction.ClientToPeers)
            {
                // Update tracking which scene peers are in
                if (msg is SceneChanged sceneChangedMsg)
                {
                    LogServer($"Peer {peer.Id} changed to scene {sceneChangedMsg.SceneId}");
                    Plugin.PeerScenes[peer.Id] = sceneChangedMsg.SceneId;
                }

                ForwardMsg(ownedMsg);
            }
            else
            {
                // Sanity check for sync direction
                if (msg.SyncDirection == Message.Direction.ServerToClient)
                {
                    throw new Exception($"[Server] Received message with ServerToClient direction: {msgType}");
                }
                
                // Handle client-to-server messages
                if (LoggingConfig.Instance.ShouldLogNetMsg(msg.MessageType))
                {
                    LogServer($"Deserialized message of type: {msg.MessageType}");
                }
                switch (msg)
                {
                    // Placeholder as there are no non-forwarded client-to-server messages yet.
                    default:
                        LogError($"Unknown message type received: {msgType}");
                        break;
                }
            }

            reader.Recycle();
        }
        catch (Exception ex)
        {
            LogError($"Failed to parse message from peer {peer.Id}: {ex}");
            // Need to clear the reader to prevent this message from getting stuck in the queue (if it were to trigger an exception again)
            reader.Recycle();
        }
    }

    public void LogServer(string message)
    {
        Logger.LogInfo($"[Server] {message}");
    }

    public void LogWarning(string message)
    {
        Logger.LogWarning($"[Server] {message}");
    }

    public void LogError(string message)
    {
        Logger.LogError($"[Server] {message}");
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        LogServer($"Received unconnected message from {remoteEndPoint}");
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (netManager.ConnectedPeersCount < MAX_PLAYERS)
        {
            request.AcceptIfKey(CONNECTION_KEY);
            LogServer($"Connection request accepted from {request.RemoteEndPoint}");
        }
        else
        {
            request.Reject();
        }
    }
}

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

public class NetworkManager : INetEventListener
{
    internal NetManager netManager;
    private NetDataWriter writer;
    private const int Port = 9050;
    private const string ConnectionKey = "GrimshireCoopKey";
    private ManualLogSource Logger = Plugin.Logger;

    private Dictionary<int, Vector3> playerPositions = new Dictionary<int, Vector3>();
    private Dictionary<int, NetPeer> peers = new Dictionary<PeerId, NetPeer>();

    public NetworkManager()
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
        netManager.Connect(address, Port, ConnectionKey);
        Logger.LogInfo($"Connecting to {address}:{Port}");
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

        Plugin.PeerScenes[peer.Id] = SceneManager.GetActiveScene().name; // TODO! peer might've joined from another scene

        // Assign Peer ID
        SendMsg(peer.Id, new AssignPeerId
        {
            PeerId = peer.Id,
            PeerScenes = new Dictionary<int, string>(Plugin.PeerScenes)
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

    public void SendMsgToAll(Message msg)
    {
        writer.Reset();
        msg.Serialize(writer);
        netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        LogServer($"Sent message {msg.MessageType} to all peers");
    }

    public void ForwardMsg(OwnedMessage msg)
    {
        writer.Reset();
        foreach (var peer in netManager.ConnectedPeerList)
        {
            if (peer.Id != msg.OwnerPeerId)
            {
                msg.Serialize(writer);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                LogServer($"Forwarded message {msg.MessageType} to peer {peer.Id}");
            }
        }
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        LogServer($"Peer disconnected: {peer.Id}, reason: {disconnectInfo.Reason}");
        playerPositions.Remove(peer.Id);
        peers.Remove(peer.Id);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Logger.LogError($"Network error: {socketError}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        LogServer($"Server received data from client {peer.Id} bytes {reader.AvailableBytes}");

        var msgType = reader.GetString(100);
        Type msgClass = Plugin.MessageTypes[msgType];

        Message msg = Activator.CreateInstance(msgClass, reader) as Message;
        
        // Handle the message (mostly forwarding)
        LogServer($"Deserialized message of type: {msg.MessageType}");
        switch (msg)
        {
            case Messages.Client.Position positionMsg:
                ForwardMsg(positionMsg);
                break;
            case Messages.Client.Movement movementMsg:
                ForwardMsg(movementMsg);
                break;
            case Messages.Client.StoppedMoving stoppedMovingMsg:
                ForwardMsg(stoppedMovingMsg);
                break;
            case Messages.Client.ToolUsed toolUsedMsg:
                ForwardMsg(toolUsedMsg);
                break;
            case Messages.Client.FaceDirection faceDirectionMsg:
                ForwardMsg(faceDirectionMsg);
                break;
            case Messages.Client.SetHeldItem setHeldItemMsg:
                ForwardMsg(setHeldItemMsg);
                break;
            case Messages.Host.SetRandomSeed setRandomSeedMsg:
                ForwardMsg(setRandomSeedMsg);
                break;
            case Messages.Client.TileMapAction tileMapActionMsg:
                ForwardMsg(tileMapActionMsg);
                break;
            case Messages.Client.ObjectAction objectActionMsg: // Will handle derived msgs as well.
                ForwardMsg(objectActionMsg);
                break;
            case Messages.Client.SceneChanged sceneChangedMsg:
                LogServer($"Peer {peer.Id} changed scene to {sceneChangedMsg.SceneId}");
                ForwardMsg(sceneChangedMsg);
                break;
            case Messages.Client.ReplicateObject replicateObjectMsg:
                LogServer($"Peer {peer.Id} replicated object {replicateObjectMsg.NetId} in scene {replicateObjectMsg.SceneId} to peer {replicateObjectMsg.TargetPeerId}");
                ForwardMsg(replicateObjectMsg);
                break;
            default:
                LogWarning($"Unknown message type received: {msgType}");
                break;
        }

        reader.Recycle();
    }

    public void LogServer(string message)
    {
        Logger.LogInfo($"[Server] {message}");
    }

    public void LogWarning(string message)
    {
        Logger.LogWarning($"[Server] {message}");
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
        if (netManager.ConnectedPeersCount < 2) // TODO
        {
            request.AcceptIfKey(ConnectionKey);
            LogServer($"Connection request accepted from {request.RemoteEndPoint}");
        }
        else
        {
            request.Reject();
        }
    }
}
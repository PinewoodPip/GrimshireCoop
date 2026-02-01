
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using BepInEx;
using BepInEx.Logging;
using GrimshireCoop.Messages.Server;
using GrimshireCoop.Network.Messages;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop;

public class NetworkManager : INetEventListener
{
    internal NetManager netManager;
    private NetDataWriter writer;
    private const int Port = 9050;
    private const string ConnectionKey = "GrimshireCoopKey";
    private ManualLogSource Logger = Plugin.Logger;

    private Dictionary<int, Vector3> playerPositions = new Dictionary<int, Vector3>();
    private Dictionary<int, NetPeer> peers = new Dictionary<int, NetPeer>();

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
        Logger.LogInfo("NetworkManager stopped.");
    }

    // INetEventListener implementations
    public void OnPeerConnected(NetPeer peer)
    {
        Logger.LogInfo($"Peer connected: {peer.Id}");
        peers[peer.Id] = peer;

        // Assign Peer ID
        SendMsg(peer.Id, new AssignPeerId
        {
            PeerId = peer.Id
        });

        // Replicate existing objects for the new peer
        // TODO replication utilities to sync state of those objects
        foreach (var netObj in Plugin.NetworkedObjects.Values)
        {
            // Send create game object message
            SendMsg(peer.Id, new CreateGameObject
            {
                GameObjectId = netObj.NetTypeID,
                NetId = netObj.netId,
                OwnerPeerId = netObj.peerId,
                PositionX = netObj.transform.position.x,
                PositionY = netObj.transform.position.y,
                PositionZ = netObj.transform.position.z
            });
        }

        // Send create player message
        // Clients will track it, including the host through its client connection
        PlayerController playerController = GameManager.Instance.Player;
        Transform playerTransform = playerController.transform;
        SendMsgToAll(new CreateGameObject
        {
            GameObjectId = "PeerPlayer",
            NetId = Plugin.NetworkedObjects.Count + 1,
            OwnerPeerId = peer.Id,
            PositionX = playerTransform.position.x,
            PositionY = playerTransform.position.y,
            PositionZ = playerTransform.position.z
        });
    }

    public void SendMsg(int peerId, Message msg)
    {
        if (peers.TryGetValue(peerId, out NetPeer peer))
        {
            writer.Reset();
            msg.Serialize(writer);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            Logger.LogInfo($"Sent message {msg.MessageType} to peer {peerId}");
        }
        else
        {
            Logger.LogWarning($"Peer {peerId} not found.");
        }
    }

    public void SendMsgToAll(Message msg)
    {
        writer.Reset();
        msg.Serialize(writer);
        netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        Logger.LogInfo($"Sent message {msg.MessageType} to all peers");
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
                Logger.LogInfo($"Forwarded message {msg.MessageType} to peer {peer.Id}");
            }
        }
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Logger.LogInfo($"Peer disconnected: {peer.Id}, reason: {disconnectInfo.Reason}");
        playerPositions.Remove(peer.Id);
        peers.Remove(peer.Id);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Logger.LogError($"Network error: {socketError}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        Logger.LogInfo($"Server received data from client {peer.Id} bytes {reader.AvailableBytes}");

        var msgType = reader.GetString(100);
        Type msgClass = Plugin.MessageTypes[msgType];

        Message msg = Activator.CreateInstance(msgClass, reader) as Message;
        
        // Handle the message (mostly forwarding)
        Logger.LogInfo($"Deserialized message of type: {msg.MessageType}");
        switch (msg)
        {
            case Messages.Shared.Position positionMsg:
                ForwardMsg(positionMsg);
                break;
            case Messages.Shared.Movement movementMsg:
                ForwardMsg(movementMsg);
                break;
            default:
                Logger.LogWarning($"Unknown message type received: {msgType}");
                break;
        }

        reader.Recycle();
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        Logger.LogInfo($"Received unconnected message from {remoteEndPoint}");
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (netManager.ConnectedPeersCount < 2) // TODO
        {
            request.AcceptIfKey(ConnectionKey);
            Logger.LogInfo($"Connection request accepted from {request.RemoteEndPoint}");
        }
        else
        {
            request.Reject();
        }
    }
}
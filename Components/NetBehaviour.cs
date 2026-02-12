
using GrimshireCoop.Messages;
using GrimshireCoop.Messages.Client;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrimshireCoop;

public abstract class NetBehaviour : MonoBehaviour
{
    public abstract string NetTypeID { get; }

    protected Client Client => Plugin.client;
    public bool IsLocalPlayer { get => peerId == Plugin.client.ClientPeerId; }
    public bool IsDirty { get; protected set; } = false;

    public PeerId peerId;
    public NetId netId;

    private bool Unregistered = false;

    public void Awake()
    {

    }

    public void SetPeerID(PeerId id)
    {
        peerId = id;
    }

    public void Update()
    {
        if (!IsLocalPlayer) return;

        NetworkUpdate();
    }

    /// <summary>
    /// Sends a message to the server.
    /// **The message will be freed to the pool afterwards**.
    /// </summary>
    public void SendMsg(Message msg)
    {
        // Not a very useful check, since we want to allow clients to interact with non-owned objects as well
        // if (!IsLocalPlayer) { Debug.LogWarning("Attempted to send message from non-local player"); return; }

        NetDataWriter writer = new NetDataWriter();
        msg.Serialize(writer);
        Client.ServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        NetMessagePool.Release(msg);
    }

    public virtual void NetworkUpdate() { }

    public virtual void OnAction(ObjectAction action)
    {
        
    }

    public void SendActionMsg(string actionID)
    {
        ObjectAction action = NetMessagePool.Get<ObjectAction>();
        action.OwnerPeerId = Plugin.client.ClientPeerId;
        action.NetId = netId;
        action.Action = actionID;
        SendMsg(action);
    }
    public void SendActionMsg(string actionID, Vector2 position)
    {
        ObjectPositionedAction action = NetMessagePool.Get<ObjectPositionedAction>();
        action.OwnerPeerId = Plugin.client.ClientPeerId;
        action.NetId = netId;
        action.Action = actionID;
        action.Position = position;
        SendMsg(action);
    }

    public void SendInteractionMsg()
    {
        SendActionMsg("Interact");
    }

    public virtual void Sync()
    {
        IsDirty = false;
    }

    public virtual byte[] GetReplicationData()
    {
        return [];
    }

    public virtual void ApplyReplicationData(byte[] data)
    {
        
    }

    /// <summary>
    /// Destroys the object locally and notifies peers to destroy it on their end as well.
    /// </summary>
    public void NetDestroy()
    {
        DestroyObject msg = NetMessagePool.Get<DestroyObject>();
        msg.OwnerPeerId = Client.ClientPeerId;
        msg.NetId = netId;
        SendMsg(msg);
        Object.Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!Unregistered)
        {
            Debug.Log($"NetworkedBehaviour OnDestroy called for netId {netId}, unregistering from scene {SceneManager.GetActiveScene().name} {gameObject.name}");
            Plugin.UnregisterNetObject(this, SceneManager.GetActiveScene().name);
            Unregistered = true;
        }
    }
}
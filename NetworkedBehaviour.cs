
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrimshireCoop;

public abstract class NetworkedBehaviour : MonoBehaviour
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

    public void SendMsg(Message msg)
    {
        if (!IsLocalPlayer) { Debug.LogWarning("Attempted to send message from non-local player"); return; }

        NetDataWriter writer = new NetDataWriter();
        msg.Serialize(writer);
        Client.ServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public virtual void NetworkUpdate() { }

    public virtual void Sync()
    {
        IsDirty = false;
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
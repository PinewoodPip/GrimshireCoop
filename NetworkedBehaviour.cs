
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrimshireCoop;

public abstract class NetworkedBehaviour : MonoBehaviour
{
    public abstract string NetTypeID { get; }

    protected NetManager client => Plugin.client;
    public int peerId;
    public int netId;
    public bool isLocalPlayer { get => peerId == Plugin.ClientPeerId; }
    public bool isDirty { get; protected set; } = false;

    private bool Unregistered = false;

    public void Awake()
    {

    }

    public void SetPeerID(int id)
    {
        peerId = id;
    }

    public void Update()
    {
        if (!isLocalPlayer) return;

        NetworkUpdate();
    }

    public void SendMsg(Message msg)
    {
        if (!isLocalPlayer) { Debug.LogWarning("Attempted to send message from non-local player"); return; }

        NetDataWriter writer = new NetDataWriter();
        msg.Serialize(writer);
        client.FirstPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public virtual void NetworkUpdate() { }

    public virtual void Sync()
    {
        isDirty = false;
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
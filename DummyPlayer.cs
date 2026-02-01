
using GrimshireCoop.Messages.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop;

public class DummyPlayer : NetworkedBehaviour
{
    public override string NetTypeID => "DummyPlayer";

    public void Awake()
    {
        Debug.Log("DummyPlayer awake");
    }

    public void Start()
    {
        Debug.Log("DummyPlayer started");

        var renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = GameManager.Instance.CharacterManager.GetCharacterByName("Tano").HeadSprite;
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 0;
    }

    public override void NetworkUpdate()
    {
        // transform.position = GameManager.Instance.Player.transform.position;
        // NetworkIdentity identity = GetComponent<NetworkIdentity>();
        // Debug.Log($"DummyPlayer.Update called for player {identity.netId} (local: {identity.isLocalPlayer})");
        // if (!identity.isLocalPlayer) return;

        // TODO player check

        bool moved = false;
        if (Input.GetKeyDown(KeyCode.W))
        {
            transform.position += new Vector3(0, 1, 0);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            transform.position += new Vector3(0, -1, 0);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            transform.position += new Vector3(-1, 0, 0);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            transform.position += new Vector3(1, 0, 0);
            moved = true;
        }

        isDirty = isDirty || moved;
    }

    // Send position to all connected peers
    public override void Sync()
    {
        // TODO position message
        // writer.Reset();
        // writer.Put(position.x);
        // writer.Put(position.y);
        // writer.Put(position.z);
        
        // server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        Debug.Log($"DummyPlayer.Sync called for netId {netId} at position {transform.position}");
        Position msg = new Position
        {
            OwnerPeerId = peerId,
            NetId = netId,
            PositionX = transform.position.x,
            PositionY = transform.position.y,
            PositionZ = transform.position.z
        };
        SendMsg(msg);

        base.Sync();
    }
}
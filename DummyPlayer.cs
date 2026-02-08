
using GrimshireCoop.Messages.Client;
using UnityEngine;

namespace GrimshireCoop;

public class DummyPlayer : NetworkedBehaviour
{
    public override string NetTypeID => "DummyPlayer";

    public new void Awake()
    {
        base.Awake();
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

        IsDirty = IsDirty || moved;
    }

    // Send position to all connected peers
    public override void Sync()
    {
        Debug.Log($"DummyPlayer.Sync called for netId {netId} at position {transform.position}");
        Position msg = new ()
        {
            OwnerPeerId = peerId,
            NetId = netId,
            Pos = transform.position
        };
        SendMsg(msg);

        base.Sync();
    }
}
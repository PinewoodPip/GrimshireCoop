
using GrimshireCoop.Messages.Client;
using UnityEngine;

namespace GrimshireCoop;

public class PeerPlayer : NetworkedBehaviour
{
    public override string NetTypeID => "PeerPlayer";

    public Animator animator;
    public SpriteRenderer heldItemSprite;

    private Vector3 OldPosition;

    public new void Awake()
    {
        base.Awake();
        Debug.Log("PeerPlayer awake");
        GameObject playerSpriteObj = transform.Find("PlayerSprite").gameObject;
        animator = playerSpriteObj.GetComponent<Animator>();
        heldItemSprite = playerSpriteObj.transform.Find("HeldItemSprite").GetComponent<SpriteRenderer>();
    }

    public void Start()
    {
        Debug.Log("PeerPlayer started");
        OldPosition = transform.position;
    }

    // Copied from PlayerController
    // TODO replace with UpdateAnim()
    public void FaceTowards(Vector2 dir)
    {
        Vector2 diff = dir - (Vector2)base.transform.position;
        animator.SetFloat("horizontal", 0f);
        animator.SetFloat("vertical", 0f);
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            animator.SetFloat("horizontal", (diff.x > 0f) ? 1 : (-1));
        }
        else
        {
            animator.SetFloat("vertical", (diff.y > 0f) ? 1 : (-1));
        }
    }

    public void AnimateWalkTowards(Vector2 dir)
    {
        FaceTowards(dir);
        animator.SetBool("isWalking", true);
    }

    public void OnStoppedMoving()
    {
        animator.SetBool("isWalking", false);
    }

    public void PlayToolUseAnimation(ToolUsed.ToolType toolId)
    {
        switch (toolId)
        {
            case ToolUsed.ToolType.Axe:
                animator.SetTrigger("Axe");
                break;
            case ToolUsed.ToolType.Scythe:
                animator.SetTrigger("Scythe");
                break;
            case ToolUsed.ToolType.Pickaxe:
                animator.SetTrigger("Pick");
                break;
            case ToolUsed.ToolType.FishingRod:
                animator.SetTrigger("FishCast"); // TODO sync FinishCast as well
                break;
            case ToolUsed.ToolType.Hoe:
                animator.SetTrigger("Hoe");
                break;
            case ToolUsed.ToolType.WaterCan:
                animator.SetTrigger("Water");
                break;
            default:
                Debug.LogWarning($"PlayToolUseAnimation: unknown tool {toolId}");
                break;
        }
    }

    public void SetHeldItem(int itemId)
    {
        InventoryItem item = itemId >= 0 ? ResourceManager.Instance.GetInventoryItemByID(itemId) : null;
        bool holdingItem = item != null;
        animator.SetBool("isHolding", holdingItem);
        this.heldItemSprite.enabled = holdingItem;
        if (holdingItem)
        {
            this.heldItemSprite.sprite = item.InventoryDisplayIcon;
        }
    }

    public override void NetworkUpdate()
    {
        OldPosition = transform.position;

        // Check for movement keys
        bool moved = false;
        Vector2 moveDirection = Vector2.zero;
        if (Input.GetKeyDown(KeyCode.W))
        {
            moveDirection += Vector2.up;
            transform.position += (Vector3)moveDirection;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            moveDirection += Vector2.down;
            transform.position += (Vector3)moveDirection;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            moveDirection += Vector2.left;
            transform.position += (Vector3)moveDirection;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            moveDirection += Vector2.right;
            transform.position += (Vector3)moveDirection;
            moved = true;
        }

        // Update animations
        if (moved)
        {
            FaceTowards((Vector2)transform.position + moveDirection);
        }

        IsDirty = IsDirty || moved;
    }

    // Send position to all connected peers
    public override void Sync()
    {
        Movement msg = NetMessagePool.Get<Movement>();
        msg.OwnerPeerId = peerId;
        msg.NetId = netId;
        msg.OldPosition = OldPosition;
        msg.NewPosition = transform.position;
        SendMsg(msg);

        base.Sync();
    }

    public static PeerPlayer Instantiate()
    {
        PlayerController clientPlayer = GameManager.Instance.Player;
        GameObject peerPlayerObj = new GameObject("Coop.NetPeerPlayer");

        // Copy the player animator setup
        GameObject playerSprite = clientPlayer.transform.Find("PlayerSprite").gameObject;
        GameObject peerPlayerSprite = GameObject.Instantiate(playerSprite, peerPlayerObj.transform);
        peerPlayerSprite.transform.parent = peerPlayerObj.transform;
        peerPlayerSprite.name = "PlayerSprite";

        // Copy placement blocker trigger (so the peer will block placing objects at its location)
        GameObject playerPlacementDetection = clientPlayer.transform.Find("PlayerPlacementDetection").gameObject;
        GameObject peerPlayerPlacementDetection = GameObject.Instantiate(playerPlacementDetection, peerPlayerObj.transform);
        peerPlayerPlacementDetection.transform.parent = peerPlayerObj.transform;
        peerPlayerPlacementDetection.name = "PlayerPlacementDetection";

        return peerPlayerObj.AddComponent<PeerPlayer>();
    }
}
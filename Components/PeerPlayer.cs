
using GrimshireCoop.Messages.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop;

public class PeerPlayer : NetworkedBehaviour
{
    public override string NetTypeID => "PeerPlayer";

    public Animator animator;

    private Vector3 OldPosition;

    public new void Awake()
    {
        base.Awake();
        Debug.Log("PeerPlayer awake");
        animator = transform.Find("PlayerSprite").GetComponent<Animator>();
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
        Debug.Log($"PeerPlayer.Sync called for netId {netId} at position {transform.position}");
        Movement msg = new()
        {
            OwnerPeerId = peerId,
            NetId = netId,
            OldPosition = OldPosition,
            NewPosition = transform.position
        };
        SendMsg(msg);

        base.Sync();
    }
}
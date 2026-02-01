
using GrimshireCoop.Messages.Shared;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using static GrimshireCoop.Utils;

namespace GrimshireCoop;

public class ClientPlayer : NetworkedBehaviour
{
    public override string NetTypeID => "ClientPlayer";

    private const float POSITION_CHANGE_THRESHOLD = 0.1f;

    public PlayerController player;
    public Vector3 OldPosition;
    public bool WasMoving = false;

    static bool MethodsWerePatched = false;

    public void Awake()
    {
        Debug.Log("ClientPlayer awake");
        player = GameManager.Instance.Player;

        if (!MethodsWerePatched)
        {
            Harmony.CreateAndPatchAll(typeof(ClientPlayer));
            MethodsWerePatched = true;
        }
    }

    // Sync movement
    [HarmonyPatch(typeof(PlayerMovement), "HandleMovement")]
    [HarmonyPrefix]
    static bool OnProcessInput(PlayerMovement __instance)
    {
        ClientPlayer clientPlayer = __instance.GetComponent<ClientPlayer>();
        clientPlayer.OldPosition = __instance.transform.position;
        return true;
    }
    [HarmonyPatch(typeof(PlayerMovement), "HandleMovement")]
    [HarmonyPostfix]
    static void AfterProcessInput(PlayerMovement __instance)
    {
        ClientPlayer clientPlayer = __instance.GetComponent<ClientPlayer>();
        Vector3 newPosition = __instance.transform.position;
        if (Vector3.Distance(newPosition, clientPlayer.OldPosition) > POSITION_CHANGE_THRESHOLD)
        {
            clientPlayer.isDirty = true;
            clientPlayer.WasMoving = true;
        }
        else if (clientPlayer.WasMoving)
        {
            // Report stopped moving
            StoppedMoving msg = new()
            {
                OwnerPeerId = clientPlayer.peerId,
                NetId = clientPlayer.netId
            };
            clientPlayer.SendMsg(msg);
            clientPlayer.WasMoving = false;
        }
    }

    // Sync facing dir
    [HarmonyPatch(typeof(PlayerController), "FaceTowards")]
    [HarmonyPostfix]
    static void OnFaceTowards(PlayerController __instance)
    {
        ClientPlayer clientPlayer = __instance.GetComponent<ClientPlayer>();
        if (clientPlayer != null)
        {
            Vector2 facingDir = __instance.GetInteractPosition();
            FaceDirection msg = new()
            {
                OwnerPeerId = clientPlayer.peerId,
                NetId = clientPlayer.netId,
                PosX = facingDir.x,
                PosY = facingDir.y
            };
            clientPlayer.SendMsg(msg);
        }
    }

    // Sync tool usage
    [HarmonyPatch(typeof(AxePlayerState), "Enter")]
    [HarmonyPostfix]
    static void AfterAxeUse(AxePlayerState __instance)
    {
        TrySendToolUse(__instance, ToolUsed.ToolType.Axe);
    }
    [HarmonyPatch(typeof(ScythePlayerState), "Enter")]
    [HarmonyPostfix]
    static void AfterScytheUse(ScythePlayerState __instance)
    {
        TrySendToolUse(__instance, ToolUsed.ToolType.Scythe);
    }
    [HarmonyPatch(typeof(HoePlayerState), "Enter")]
    [HarmonyPostfix]
    static void AfterHoeUse(HoePlayerState __instance)
    {
        TrySendToolUse(__instance, ToolUsed.ToolType.Hoe);
    }
    [HarmonyPatch(typeof(PickPlayerState), "Enter")]
    [HarmonyPostfix]
    static void AfterPickUse(PickPlayerState __instance)
    {
        TrySendToolUse(__instance, ToolUsed.ToolType.Pickaxe);
    }
    [HarmonyPatch(typeof(WateringCanPlayerState), "Enter")]
    [HarmonyPostfix]
    static void AfterWateringCanUse(WateringCanPlayerState __instance)
    {
        TrySendToolUse(__instance, ToolUsed.ToolType.WaterCan);
    }
    [HarmonyPatch(typeof(FishingPlayerState), "Enter")]
    [HarmonyPostfix]
    static void AfterFishingUse(FishingPlayerState __instance)
    {
        TrySendToolUse(__instance, ToolUsed.ToolType.FishingRod);
    }
    private static void TrySendToolUse(StateBehaviour state, ToolUsed.ToolType toolId)
    {
        PlayerController playerController = GetField<PlayerController>(state, "playerRef");
        ClientPlayer clientPlayer = playerController.GetComponent<ClientPlayer>();
        if (clientPlayer)
        {
            ToolUsed msg = new()
            {
                OwnerPeerId = clientPlayer.peerId,
                NetId = clientPlayer.netId,
                ToolId = toolId,
            };
            clientPlayer.SendMsg(msg);
        }
    }

    public override void NetworkUpdate()
    {
        
    }

    // Send position to all connected peers
    public override void Sync()
    {
        // TODO perhaps make a special message for this.
        // ATM the type of the game object is desynched between
        // peers, ie. for the local player it's a ClientPlayer
        // but for other peers it's a PeerPlayer.
        Debug.Log($"ClientPlayer.Sync called for netId {netId} at position {transform.position}");
        Movement msg = new()
        {
            OwnerPeerId = peerId,
            NetId = netId,
            OldPositionX = OldPosition.x,
            OldPositionY = OldPosition.y,
            OldPositionZ = OldPosition.z,
            NewPositionX = transform.position.x,
            NewPositionY = transform.position.y,
            NewPositionZ = transform.position.z
        };
        SendMsg(msg);

        base.Sync();
    }

    public void OnDestroy()
    {
        Debug.Log("ClientPlayer destroyed");
    }
}

using GrimshireCoop.Messages.Shared;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop;

public class ClientPlayer : NetworkedBehaviour
{
    public override string NetTypeID => "ClientPlayer";

    public PlayerController player;
    public Vector3 OldPosition;

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
        if (newPosition != clientPlayer.OldPosition)
        {
            clientPlayer.isDirty = true;
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
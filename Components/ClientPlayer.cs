
using GrimshireCoop.Messages.Client;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GrimshireCoop.Utils;

namespace GrimshireCoop;

public class ClientPlayer : NetBehaviour
{
    public override string NetTypeID => "ClientPlayer";

    private const float POSITION_CHANGE_THRESHOLD = 0.1f;

    public PlayerController player;
    public Vector3 OldPosition;
    public bool WasMoving = false;

    static bool MethodsWerePatched = false;

    static string CurrentSceneName = ""; // Necessary as the player is technically on the DontDestroyOnLoad scene


    public new void Awake()
    {
        base.Awake();
        Debug.Log("ClientPlayer awake");
        player = GameManager.Instance.Player;

        if (!MethodsWerePatched)
        {
            Harmony.CreateAndPatchAll(typeof(ClientPlayer));
            MethodsWerePatched = true;
        }

        CurrentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    public new void Update()
    {
        base.Update();
        if (!IsLocalPlayer)
        {
            Debug.LogWarning("ClientPlayer exists on non-local player??? This should never happen.");
            return;
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        string oldSceneName = CurrentSceneName;

        Plugin.PeerScenes[peerId] = newScene.name;

        // Send scene change message
        Plugin.ChangeNetObjectScene(this, oldSceneName, newScene.name); // oldScene.name is null, possibly due to it technically being the DontDestroyOnLoad scene?
        SceneChanged sceneChangedMsg = NetMessagePool.Get<SceneChanged>();
        sceneChangedMsg.OwnerPeerId = peerId;
        sceneChangedMsg.SceneId = newScene.name;
        sceneChangedMsg.ClientPlayerNetId = Client.ClientPlayerNetId;
        sceneChangedMsg.Position = player.transform.position;
        SendMsg(sceneChangedMsg);
        CurrentSceneName = newScene.name;
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
            clientPlayer.IsDirty = true;
            clientPlayer.WasMoving = true;
        }
        else if (clientPlayer.WasMoving)
        {
            // Report stopped moving
            StoppedMoving msg = NetMessagePool.Get<StoppedMoving>();
            msg.OwnerPeerId = clientPlayer.peerId;
            msg.NetId = clientPlayer.netId;
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
            FaceDirection msg = NetMessagePool.Get<FaceDirection>();
            msg.OwnerPeerId = clientPlayer.peerId;
            msg.NetId = clientPlayer.netId;
            msg.PosX = facingDir.x;
            msg.PosY = facingDir.y;
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
            ToolUsed msg = NetMessagePool.Get<ToolUsed>();
            msg.OwnerPeerId = clientPlayer.peerId;
            msg.NetId = clientPlayer.netId;
            msg.ToolId = toolId;
            clientPlayer.SendMsg(msg);
        }
    }

    // Sync held item.
    [HarmonyPatch(typeof(PlayerController), "SetHeldItem")]
    [HarmonyPostfix]
    static void AfterSetHeldItem(PlayerController __instance, bool holdingItem)
    {
        ClientPlayer clientPlayer = __instance.GetComponent<ClientPlayer>();
        if (clientPlayer)
        {
            int itemId = holdingItem ? __instance.GetHeldItemRef().ID : -1;
            SetHeldItem msg = NetMessagePool.Get<SetHeldItem>();
            msg.OwnerPeerId = clientPlayer.peerId;
            msg.NetId = clientPlayer.netId;
            msg.ItemId = itemId;
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
        Movement msg = NetMessagePool.Get<Movement>();
        msg.OwnerPeerId = peerId;
        msg.NetId = netId;
        msg.OldPosition = OldPosition;
        msg.NewPosition = transform.position;
        SendMsg(msg);

        base.Sync();
    }

    public static ClientPlayer Instantiate()
    {
        PlayerController clientPlayer = GameManager.Instance.Player;
        return clientPlayer.gameObject.AddComponent<ClientPlayer>();
    }

    public void OnDestroy()
    {
        Debug.Log("ClientPlayer destroyed");
    }
}
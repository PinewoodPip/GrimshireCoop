
using GrimshireCoop.Messages.Shared;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop;

public static class TreeManager
{
    public static bool ignoreHooks = false;

    [HarmonyPatch(typeof(PersistentTreeManager), "LoadTrees")]
    [HarmonyPrefix]
    static bool OnPersistentTreeManagerLoadTrees(PersistentTreeManager __instance)
    {
        return Plugin.IsHost; // TODO replace with ownership check for the scene
    }

    [HarmonyPatch(typeof(TreeObject), "Start")]
    [HarmonyPostfix]
    static void AfterTreeObjectStart(TreeObject __instance)
    {
        if (Plugin.IsHost)
        {
            // Create networked object
            Components.TreeObject netTree = WrapNetObject(__instance.gameObject.AddComponent<Components.TreeObject>(), Plugin.client.ClientPeerId, Plugin.NextFreeNetId) as Components.TreeObject; // TODO have this consume the next free netId from the plugin, not take it as param
            RequestCreateTree msg = new()
            {
                OwnerPeerId = netTree.peerId,
                NetId = netTree.netId,
                TreeData = __instance.PTreeDataContainer
            };
            SendMsg(msg);
        }
    }

    [HarmonyPatch(typeof(TreeObject), "UseAxe")]
    [HarmonyPostfix]
    static void TreeObjectUseAxe(TreeObject __instance, int axeTier, int value)
    {
        if (ignoreHooks) return;

        // Send action msg
        Components.TreeObject netObj = __instance.GetComponent<Components.TreeObject>();
        if (netObj)
        {
            ObjectAction action = new()
            {
                OwnerPeerId = Plugin.client.ClientPeerId,
                NetId = netObj.netId,
                Action = "UseAxe"
            };
            SendMsg(action);
        }
    }

    public static void SendMsg(Message msg)
    {
        NetDataWriter writer = new NetDataWriter();
        msg.Serialize(writer);
        Plugin.client.ServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    private static NetworkedBehaviour WrapNetObject(NetworkedBehaviour netObj, PeerId ownerPeerId, NetId netId) // TODO generic type
    {
        netObj.SetPeerID(ownerPeerId);
        netObj.netId = netId;

        // Track it
        Plugin.RegisterNetObject(netObj, Client.CurrentSceneID);

        return netObj;
    }
}
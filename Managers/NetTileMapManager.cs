
using GrimshireCoop.Components;
using GrimshireCoop.Messages.Shared;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop;

public static class NetTileMapManager
{
    public static bool ignoreHooks = false;

    // Sync tile actions.
    [HarmonyPatch(typeof(TileMapManager), "Dig")]
    [HarmonyPrefix]
    static void OnDig(TileMapManager __instance, Vector3 position)
    {
        if (ignoreHooks) return;
        Client.SendMsg(new TileMapAction
        {
            OwnerPeerId = Plugin.client.ClientPeerId,
            Position = position,
            Action = TileMapAction.ActionType.Dig
        });
    }
    [HarmonyPatch(typeof(TileMapManager), "Undig")]
    [HarmonyPrefix]
    static void OnUndig(TileMapManager __instance, Vector3 position)
    {
        if (ignoreHooks) return;
        Client.SendMsg(new TileMapAction
        {
            OwnerPeerId = Plugin.client.ClientPeerId,
            Position = position,
            Action = TileMapAction.ActionType.Undig
        });
    }
    [HarmonyPatch(typeof(TileMapManager), "WaterTile")]
    [HarmonyPrefix]
    static void OnWaterTile(TileMapManager __instance, Vector3 position)
    {
        if (ignoreHooks) return;
        Client.SendMsg(new TileMapAction
        {
            OwnerPeerId = Plugin.client.ClientPeerId,
            Position = position,
            Action = TileMapAction.ActionType.Water
        });
    }

    public static void HandleTileMapAction(TileMapAction msg)
    {
        TileMapManager manager = GameManager.Instance.TileMapManager;
        ignoreHooks = true;
        Debug.Log($"Handling TileMapAction: {msg.Action} at {msg.Position}");
        switch (msg.Action)
        {
            case TileMapAction.ActionType.Dig:
                manager.Dig(msg.Position);
                break;
            case TileMapAction.ActionType.Undig:
                manager.Undig(msg.Position);
                break;
            case TileMapAction.ActionType.Water:
                manager.WaterTile(msg.Position);
                break;
        }
        ignoreHooks = false;
    }
}

using GrimshireCoop.Messages.Client;
using HarmonyLib;

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
            Client.CreateNetObject<Components.TreeObject>(__instance.gameObject);
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
            netObj.SendMsg(action);
        }
    }
}
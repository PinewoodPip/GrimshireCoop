
using GrimshireCoop.Messages.Client;
using HarmonyLib;

namespace GrimshireCoop;

public static class NetTreeManager
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
            Client.CreateNetObject<Components.NetTreeObject>(__instance.gameObject);
        }
    }

    // Sync axe hits
    [HarmonyPatch(typeof(TreeObject), "UseAxe")]
    [HarmonyPostfix]
    static void TreeObjectUseAxe(TreeObject __instance, int axeTier, int value)
    {
        if (ignoreHooks) return;

        Components.NetTreeObject netObj = __instance.GetComponent<Components.NetTreeObject>();
        netObj?.SendActionMsg("UseAxe");
    }

    // Sync shaking
    [HarmonyPatch(typeof(TreeObject), "ShakeTree")]
    [HarmonyPostfix]
    static void AfterTreeObjectShakeTree(TreeObject __instance)
    {
        if (ignoreHooks) return;

        Components.NetTreeObject netObj = __instance.GetComponent<Components.NetTreeObject>();
        netObj?.SendActionMsg("Shake");
    }
}

using GrimshireCoop.Components;
using HarmonyLib;

namespace GrimshireCoop;

public static class NetCropManager
{
    public static bool ignoreHooks = false;

    [HarmonyPatch(typeof(CropManager), "LoadCrops")]
    [HarmonyPrefix]
    static bool OnCropManagerLoadCrops(CropManager __instance)
    {
        return Plugin.IsHost; // TODO replace with ownership check for the scene
    }

    [HarmonyPatch(typeof(CropObject), "Start")]
    [HarmonyPostfix]
    static void AfterCropObjectStart(CropObject __instance)
    {
        Client.TryCreateNetObject<NetCropObject>(__instance.gameObject);
    }

    [HarmonyPatch(typeof(CropObject), "Interact")]
    [HarmonyPostfix]
    static void AfterCropObjectInteract(CropObject __instance)
    {
        if (ignoreHooks) return;

        // Send action msg
        NetCropObject netObj = __instance.GetComponent<NetCropObject>();
        netObj?.SendInteractionMsg();
    }
}
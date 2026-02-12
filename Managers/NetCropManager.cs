
using GrimshireCoop.Components;
using HarmonyLib;

namespace GrimshireCoop;

public static class NetCropManager
{
    public static bool ignoreHooks = false;
    public static bool isSynchingInteraction = false;

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

    // Prevent synching harvest interactions with crops from causing duplicate item spawns. 
    [HarmonyPatch(typeof(CropObject), "SpawnDrops")]
    [HarmonyPrefix]
    static bool OnCropObjectSpawnDrops(CropObject __instance, CropData.ItemDrops[] itemDropsList)
    {
        return !ignoreHooks;
    }
}
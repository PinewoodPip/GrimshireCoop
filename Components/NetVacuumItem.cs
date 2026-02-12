
using HarmonyLib;
using LiteNetLib.Utils;
using UnityEngine;
using static GrimshireCoop.Utils;
using GrimshireCoop.Messages.Client;

namespace GrimshireCoop.Components;

public class NetVacuumItem : WrappedNetBehaviour<VacuumItem>
{
    public override string NetTypeID => "VacuumItem";

    public static bool ignoreHooks = false;

    public static NetVacuumItem Instantiate()
    {
        GameObject vacuumItem = Object.Instantiate<GameObject>(GameManager.Instance.GetDropItemBasePrefab());
        return vacuumItem.AddComponent<NetVacuumItem>();
    }

    // Create net object for dropped items created by this client
    // Any other ones will already receive this component from replication
    [HarmonyPatch(typeof(VacuumItem), "Start")]
    [HarmonyPostfix]
    static void AfterItemStart(VacuumItem __instance)
    {
        if (ignoreHooks) return;

        Debug.Log("Adding NetVacuumItem to " + __instance.name);
        Client.TryCreateNetObject<NetVacuumItem>(__instance.gameObject);
    }

    [HarmonyPatch(typeof(VacuumItem), "TryToPickupItem")]
    [HarmonyPrefix]
    static bool OnItemTryPickup(VacuumItem __instance, Collider2D collision)
    {
        if (ignoreHooks) return true;

        float pickupCooldown = GetField<float>(__instance, "pickupCoolDown");
        NetVacuumItem netObj = __instance.GetComponent<NetVacuumItem>();       
        if (netObj != null && pickupCooldown <= 0 && !netObj.IsLocalPlayer && collision.gameObject.GetComponent<PlayerController>())
        {
            Debug.Log("Requesting pickup of " + __instance.name + " by " + collision.gameObject.name);
            RequestItemPickup msg = NetMessagePool.Get<RequestItemPickup>();
            msg.OwnerPeerId = Client.Instance.ClientPeerId;
            msg.NetId = netObj.netId;
            Client.SendMsg(msg);
            NetMessagePool.Release(msg);
            SetField(__instance, "pickupCoolDown", 0.5f); // Set cooldown to prevent multiple pickup requests from the same item in a short time
            return false;
        }

        return true;
    }
    [HarmonyPatch(typeof(VacuumItem), "TryToPickupItem")]
    [HarmonyPostfix]
    static void AfterVacuumItemTryToPickupItem(VacuumItem __instance, Collider2D collision)
    {
        if (ignoreHooks) return;

        // Destroy the item when picked up by its owner.
        if (GetField<bool>(__instance, "thisHasBeenPickedUp"))
        {
            NetVacuumItem netObj = __instance.GetComponent<NetVacuumItem>();
            netObj.NetDestroy();
        }
    }

    public void HandlePickupRequest(PeerId requesterPeerId)
    {
        // Notify the requester that they can pickup this item, and destroy it on our (the owner's) end
        PickupItem msg = NetMessagePool.Get<PickupItem>();
        msg.OwnerPeerId = peerId;
        msg.NetId = netId;
        msg.PickerPeerId = requesterPeerId;
        Client.SendMsg(msg);
        NetMessagePool.Release(msg);
        
        ignoreHooks = true;
        NetDestroy(); // TODO the client won't necessarily have picked up the whole stack
        ignoreHooks = false;
    }

    /// <summary>
    /// Picks up the item on this client.
    /// </summary>
    public void Pickup()
    {
        VacuumItem item = WrappedComponent;
        if (item != null)
        {
            ignoreHooks = true;

            // Force-pickup item
            SetField(item, "pickupCoolDown", 0f);
            SetField(item, "thisHasBeenPickedUp", false);
            Collider2D playerCollider = GameManager.Instance.Player.GetComponent<Collider2D>();
            item.TryToPickupItem(playerCollider);

            ignoreHooks = false;
        }
    }

    public override byte[] GetReplicationData()
    {
        NetDataWriter writer = new NetDataWriter();
        VacuumItem item = WrappedComponent;
        writer.Put(GetField<int>(item, "stackAmount"));
        writer.Put(GetField<float>(item, "spoilageAmount"));
        writer.Put(item.inventoryItemRef.ID);
        return writer.Data;
    }

    public override void ApplyReplicationData(byte[] data)
    {
        NetDataReader reader = new NetDataReader(data);
        VacuumItem item = WrappedComponent;
        if (item != null)
        {
            int stackAmount = reader.GetInt();
            float spoilageAmount = reader.GetFloat();
            int itemId = reader.GetInt();

            SetField(item, "stackAmount", stackAmount);
            SetField(item, "spoilageAmount", spoilageAmount);
            InventoryItem invItem = ResourceManager.Instance.GetInventoryItemByID(itemId);
            item.inventoryItemRef = invItem;

            CallMethod(item, "Init", invItem, stackAmount, spoilageAmount);
        }
    }
}
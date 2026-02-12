
using GrimshireCoop.Messages.Client;
using UnityEngine;

namespace GrimshireCoop.MessageHandlers;

/// <summary>
/// Handlers for generic messages applicable to most networked game objects (ex. Transform changes, facing direction)
/// </summary>
public static class GameObjectHandlers
{
    public static void RegisterHandlers(Client client)
    {
        client.RegisterMessageHandler<Movement>(HandleMovementMsg);
        client.RegisterMessageHandler<StoppedMoving>(HandleStoppedMovingMsg);
        client.RegisterMessageHandler<Position>(HandlePositionMsg);
        client.RegisterMessageHandler<FaceDirection>(HandleFaceDirectionMsg);
        client.RegisterMessageHandler<ObjectAction>(HandleObjectActionMsg);
        client.RegisterMessageHandler<RequestItemPickup>(HandleRequestItemPickupMsg);
        client.RegisterMessageHandler<PickupItem>(HandlePickupItemMsg);
        client.RegisterMessageHandler<DestroyObject>(HandleDestroyObjectMsg);
    }

    private static void HandlePositionMsg(Client client, Position msg)
    {
        NetBehaviour netObj = client.GetByNetID(msg.NetId);
        netObj.transform.position = msg.Pos;
    }

    private static void HandleMovementMsg(Client client, Movement msg)
    {
        NetBehaviour netObj = client.GetByNetID(msg.NetId);

        // Animate - should be done before setting pos so the facing dir vector is correct
        if (netObj is PeerPlayer peerPlayer)
        {
            peerPlayer.AnimateWalkTowards(new Vector2(msg.NewPosition.x, msg.NewPosition.y));
        }

        netObj.transform.position = msg.NewPosition;
    }

    private static void HandleStoppedMovingMsg(Client client, StoppedMoving msg)
    {
        NetBehaviour netObj = client.GetByNetID(msg.NetId);
        if (netObj is PeerPlayer stoppedPeerPlayer)
        {
            stoppedPeerPlayer.OnStoppedMoving();
        }
    }

    private static void HandleFaceDirectionMsg(Client client, FaceDirection msg)
    {
        NetBehaviour netObj = client.GetByNetID(msg.NetId);
        if (netObj is PeerPlayer facingPeerPlayer)
        {
            facingPeerPlayer.FaceTowards(new Vector2(msg.PosX, msg.PosY));
        }
    }

    private static void HandleObjectActionMsg(Client client, ObjectAction msg)
    {
        NetBehaviour netObj = client.GetByNetID(msg.NetId);
        netObj.OnAction(msg);
    }

    private static void HandleRequestItemPickupMsg(Client client, RequestItemPickup msg)
    {
        // Approve requests to peers pickup items this client owns
        NetBehaviour netObj = client.GetByNetID(msg.NetId);
        if (netObj != null && netObj.peerId == client.ClientPeerId)
        {
            var netVacuumItem = netObj as Components.NetVacuumItem;
            netVacuumItem.HandlePickupRequest(msg.OwnerPeerId);
        }
    }

    private static void HandlePickupItemMsg(Client client, PickupItem msg)
    {
        // Pickup the item locally
        NetBehaviour netObj = client.GetByNetID(msg.NetId);
        if (netObj != null && netObj is Components.NetVacuumItem netVacuumItem && netObj.peerId == msg.OwnerPeerId)
        {
            netVacuumItem.Pickup();
        }
    }

    private static void HandleDestroyObjectMsg(Client client, DestroyObject msg)
    {
        NetBehaviour netObj = client.GetByNetID(msg.NetId);
        if (netObj != null)
        {
            Object.Destroy(netObj.gameObject);
        }
        else
        {
            Client.LogWarning($"Received DestroyObject for nonexistent object {msg.NetId}");
        }
    }
}
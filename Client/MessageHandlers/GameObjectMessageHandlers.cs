
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
}
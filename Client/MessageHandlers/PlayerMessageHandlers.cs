
using GrimshireCoop;
using GrimshireCoop.Messages.Client;

namespace GrimshireCoop.MessageHandlers;

/// <summary>
/// Handlers for net messages related to the player components (ClientPlayer, PeerPlayer)
/// </summary>
public static class PlayerHandlers
{
    public static void RegisterHandlers(Client client)
    {
        client.RegisterMessageHandler<ToolUsed>(HandleToolUsedMsg);
        client.RegisterMessageHandler<SetHeldItem>(HandleSetHeldItemMsg);
    }
    
    public static void HandleToolUsedMsg(Client client, ToolUsed msg)
    {
        NetBehaviour netObj = client.GetByNetID(msg.NetId);
        if (netObj is PeerPlayer toolUserPeerPlayer)
        {
            toolUserPeerPlayer.PlayToolUseAnimation(msg.ToolId);
        }
        else
        {
            Client.LogWarning($"Received ToolUsed message for net object {msg.NetId} that is not a PeerPlayer?");
        }
    }

    public static void HandleSetHeldItemMsg(Client client, SetHeldItem msg)
    {
        NetBehaviour netObj = client.GetByNetID(msg.NetId);
        if (netObj is PeerPlayer heldItemPeerPlayer)
        {
            heldItemPeerPlayer.SetHeldItem(msg.ItemId);
        }
        else
        {
            Client.LogWarning($"Received SetHeldItem message for net object {msg.NetId} that is not a PeerPlayer?");
        }
    }
}
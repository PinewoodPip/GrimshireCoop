
using GrimshireCoop.Messages.Client;
using UnityEngine;

namespace GrimshireCoop.MessageHandlers;

/// <summary>
/// Handlers for messages relating to game manager singletons.
/// </summary>
public static class GameManagerHandlers
{
    public static void RegisterHandlers(Client client)
    {
        client.RegisterMessageHandler<TileMapAction>(HandleTileMapActionMsg);
    }

    public static void HandleTileMapActionMsg(Client client, TileMapAction msg)
    {
        NetTileMapManager.HandleTileMapAction(msg);
    }
}
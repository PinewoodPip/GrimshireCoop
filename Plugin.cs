using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using GrimshireCoop.Components;

namespace GrimshireCoop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Server server;
    internal static Client client;
    internal static int PORT = 9050;
    internal static PeerId serverPeerId;
    private static int HomeSceneLoadCount = 0;

    public static bool IsHost => server.netManager.IsRunning;

    private static int usedNetIds = 0;
    public static NetId NextFreeNetId // TODO this should be kept in sync for everyone
    {
        get
        {
            int maxNetId = usedNetIds;
            usedNetIds++;
            return maxNetId + 1;
        }
    }

    // Maps scene ID to map of netId to NetworkedBehaviour
    public static Dictionary<string, Dictionary<NetId, NetBehaviour>> NetworkedObjects = [];
    // Maps peers to the scene they are in.
    public static Dictionary<PeerId, string> PeerScenes = [];

    public static Dictionary<string, Type> MessageTypes = new()
    {
        { "Client.CreateGameObject", typeof(Messages.Client.CreateGameObject) },
        { "Client.Position", typeof(Messages.Client.Position) },
        { "Client.Movement", typeof(Messages.Client.Movement) },
        { "Client.StoppedMoving", typeof(Messages.Client.StoppedMoving) },
        { "Client.ToolUsed", typeof(Messages.Client.ToolUsed) },
        { "Client.FaceDirection", typeof(Messages.Client.FaceDirection) },
        { "Client.SetHeldItem", typeof(Messages.Client.SetHeldItem) },
        { "Client.ObjectAction", typeof(Messages.Client.ObjectAction) },
        { "Client.ObjectPositionedAction", typeof(Messages.Client.ObjectPositionedAction) },
        { "Client.SceneChanged", typeof(Messages.Client.SceneChanged) },
        { "Client.ReplicateObject", typeof(Messages.Client.ReplicateObject) },
        { "Client.DestroyObject", typeof(Messages.Client.DestroyObject) },
        { "Host.SetRandomSeed", typeof(Messages.Host.SetRandomSeed) },
        { "Client.TileMapAction", typeof(Messages.Client.TileMapAction) },
        { "Server.AssignPeerId", typeof(Messages.Server.AssignPeerId) },
        { "Client.RequestItemPickup", typeof(Messages.Client.RequestItemPickup) },
        { "Client.PickupItem", typeof(Messages.Client.PickupItem) },
    };

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Apply patches
        Harmony.CreateAndPatchAll(typeof(Plugin));
        Harmony.CreateAndPatchAll(typeof(NetTreeManager));
        Harmony.CreateAndPatchAll(typeof(NetCropManager));
        Harmony.CreateAndPatchAll(typeof(NetTileMapManager));
        Harmony.CreateAndPatchAll(typeof(NetVacuumItem));

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            Logger.LogInfo($"Scene loaded: {scene.name}");
            if (scene.name == "Interior_Home_Scene")
            {
                HomeSceneLoadCount++;
                if (HomeSceneLoadCount == 2) // TODO reset on going back to main menu
                {
                    SetupServerAndClient();
                }
            }
        };
    }

    private void SetupServerAndClient()
    {
        Logger.LogInfo("Starting server...");

        var manager = new Server();
        server = manager;

        // Check if port is in use
        bool isHosting = !IsPortInUse(PORT);
        if (isHosting)
        {
            manager.netManager.Start(PORT);
        }

        client = new Client();
        client.StartClient();
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }

    public static void ChangeNetObjectScene(NetBehaviour netObj, string oldScene, string newScene)
    {
        Logger.LogInfo($"Changing net object netId {netObj.netId} {netObj.name} from scene {oldScene} to scene {newScene}");

        // Unregister from current scene
        UnregisterNetObject(netObj, oldScene);

        // Register in new scene
        RegisterNetObject(netObj, newScene);
    }

    public static void RegisterNetObject(NetBehaviour netObj, string scene)
    {
        if (!NetworkedObjects.ContainsKey(scene))
        {
            NetworkedObjects[scene] = new Dictionary<NetId, NetBehaviour>();
        }
        NetworkedObjects[scene][netObj.netId] = netObj;
    }

    public static void UnregisterNetObject(NetBehaviour netObj, string scene)
    {
        Logger.LogInfo($"Unregistering net object netId {netObj.netId} {netObj.name} from scene {scene}");
        var sceneObjects = NetworkedObjects[scene];
        if (sceneObjects.ContainsKey(netObj.netId))
        {
            sceneObjects.Remove(netObj.netId);
        }
        else
        {
            Logger.LogWarning($"Attempted to unregister net object {netObj.netId} that is not registered in scene {scene}");
        }
    }

    public static NetBehaviour GetByNetID(NetId netId)
    {
        foreach (var sceneObjects in NetworkedObjects.Values)
        {
            if (sceneObjects.TryGetValue(netId, out NetBehaviour netObj))
            {
                return netObj;
            }
        }
        return null;
    }

    public static Dictionary<NetId, NetBehaviour> GetCurrentSceneNetworkedObjects()
    {
        if (!NetworkedObjects.ContainsKey(Client.CurrentSceneID))
        {
            NetworkedObjects[Client.CurrentSceneID] = new Dictionary<NetId, NetBehaviour>();
        }
        return NetworkedObjects[Client.CurrentSceneID];
    }

    public static List<NetBehaviour> GetOwnedSceneObjects()
    {
        List<NetBehaviour> ownedObjects = new List<NetBehaviour>();
        var sceneObjects = GetCurrentSceneNetworkedObjects();
        foreach (var netObj in sceneObjects.Values)
        {
            if (netObj.peerId == client.ClientPeerId)
            {
                ownedObjects.Add(netObj);
            }
        }
        return ownedObjects;
    }

    // TODO move to some dedicated shared object manager class
    public static string GetPeerScene(PeerId peerId)
    {
        return PeerScenes[peerId];
    }

    private void LateUpdate()
    {
        // Poll network events
        server?.PollEvents();
        client?.PollEvents();

        // Update networked objects of the current scene
        // Only the owners of objects are expected to mark them as dirty,
        // thus this syncs the client's objects to server
        Dictionary<NetId, NetBehaviour> currentSceneObjects = GetCurrentSceneNetworkedObjects();
        foreach (var netObj in currentSceneObjects.Values)
        {
            if (netObj.IsDirty)
            {
                netObj.Sync();
            }
        }
    }

    private void OnDestroy()
    {
        server?.Stop();
        client?.Dispose();
    }

    // Sync RNG seed.
    [HarmonyPatch(typeof(GameManager), "SetRandomWithRandomSeed")]
    [HarmonyPostfix]
    static void AfterGameManagerSetRandomWithRandomSeed()
    {
        if (IsHost) // TODO should client be able to request this? The game has reseed calls in a lot of strange places
        {
            UnityEngine.Random.State state = UnityEngine.Random.state;
            GrimshireCoop.Messages.Host.SetRandomSeed msg = NetMessagePool.Get<Messages.Host.SetRandomSeed>();
            msg.OwnerPeerId = serverPeerId;
            msg.RandomState = state;
            server.SendMsgToAll(msg);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace GrimshireCoop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static NetworkManager server;
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
    public static Dictionary<string, Dictionary<NetId, NetworkedBehaviour>> NetworkedObjects = [];
    // Maps peers to the scene they are in.
    public static Dictionary<PeerId, string> PeerScenes = [];

    public static Dictionary<string, Type> MessageTypes = new Dictionary<string, Type>
    {
        { "Server.CreatePlayer", typeof(Messages.Server.CreatePlayer) },
        { "Server.CreateGameObject", typeof(Messages.Server.CreateGameObject) },
        { "Shared.Position", typeof(Messages.Shared.Position) },
        { "Shared.Movement", typeof(Messages.Shared.Movement) },
        { "Shared.StoppedMoving", typeof(Messages.Shared.StoppedMoving) },
        { "Shared.ToolUsed", typeof(Messages.Shared.ToolUsed) },
        { "Shared.FaceDirection", typeof(Messages.Shared.FaceDirection) },
        { "Shared.SetHeldItem", typeof(Messages.Shared.SetHeldItem) },
        { "Shared.ObjectAction", typeof(Messages.Shared.ObjectAction) },
        { "Shared.SceneChanged", typeof(Messages.Shared.SceneChanged) },
        { "Shared.ReplicateObject", typeof(Messages.Shared.ReplicateObject) },
        { "Server.AssignPeerId", typeof(Messages.Server.AssignPeerId) },
    };

    // Messages that should only be handled if they come from a client in the same scene.
    public static HashSet<string> LocalSceneMessages = new HashSet<string>
    {
        "Server.CreatePlayer",
        "Server.CreateGameObject",
        "Shared.Position",
        "Shared.Movement",
        "Shared.StoppedMoving",
        "Shared.ToolUsed",
        "Shared.FaceDirection",
        "Shared.SetHeldItem",
        "Shared.ReplicateObject",
        "Shared.ObjectAction",
    };

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Apply patches
        Harmony.CreateAndPatchAll(typeof(Plugin));
        Harmony.CreateAndPatchAll(typeof(TreeManager));
        Harmony.CreateAndPatchAll(typeof(NetCropManager));

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

        var manager = new NetworkManager();
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

    public static void ChangeNetObjectScene(NetworkedBehaviour netObj, string oldScene, string newScene)
    {
        Logger.LogInfo($"Changing net object netId {netObj.netId} {netObj.name} from scene {oldScene} to scene {newScene}");

        // Unregister from current scene
        UnregisterNetObject(netObj, oldScene);

        // Register in new scene
        RegisterNetObject(netObj, newScene);
    }

    public static void RegisterNetObject(NetworkedBehaviour netObj, string scene)
    {
        if (!NetworkedObjects.ContainsKey(scene))
        {
            NetworkedObjects[scene] = new Dictionary<NetId, NetworkedBehaviour>();
        }
        NetworkedObjects[scene][netObj.netId] = netObj;
    }

    public static void UnregisterNetObject(NetworkedBehaviour netObj, string scene)
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

    public static NetworkedBehaviour GetByNetID(NetId netId)
    {
        foreach (var sceneObjects in NetworkedObjects.Values)
        {
            if (sceneObjects.TryGetValue(netId, out NetworkedBehaviour netObj))
            {
                return netObj;
            }
        }
        return null;
    }

    public static Dictionary<NetId, NetworkedBehaviour> GetCurrentSceneNetworkedObjects()
    {
        if (!NetworkedObjects.ContainsKey(Client.CurrentSceneID))
        {
            NetworkedObjects[Client.CurrentSceneID] = new Dictionary<NetId, NetworkedBehaviour>();
        }
        return NetworkedObjects[Client.CurrentSceneID];
    }

    public static List<NetworkedBehaviour> GetOwnedSceneObjects()
    {
        List<NetworkedBehaviour> ownedObjects = new List<NetworkedBehaviour>();
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

    private void LateUpdate()
    {
        // Poll network events
        server?.PollEvents();
        client?.PollEvents();

        // Update networked objects of the current scene
        // Only the owners of objects are expected to mark them as dirty,
        // thus this syncs the client's objects to server
        Dictionary<NetId, NetworkedBehaviour> currentSceneObjects = GetCurrentSceneNetworkedObjects();
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
}

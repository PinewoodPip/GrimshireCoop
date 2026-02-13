using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace GrimshireCoop;

/// <summary>
/// Configuration for which net messages should be logged to console.
/// Reads from a JSON file in the persistent data dir.
/// </summary>
public class LoggingConfig
{
    private static readonly ManualLogSource Logger = Plugin.Logger;
    private static readonly string CONFIG_FILENAME = "GrimshireCoop_LoggingConfig.json";
    
    private HashSet<string> loggedMessageTypes = [];
    
    // For serialization only.
    [Serializable]
    private class ConfigData
    {
        public List<string> MessageTypes = [];
    }

    public static LoggingConfig Instance { get; private set; }

    public static void Initialize()
    {
        Instance = new LoggingConfig();
        Instance.LoadConfig();
    }

    /// <summary>
    /// Returns whether logging is enabled for a net message type.
    /// </summary>
    public bool ShouldLogNetMsg(string messageType)
    {
        return loggedMessageTypes.Contains(messageType);
    }

    private void LoadConfig()
    {
        string configPath = Path.Combine(Application.persistentDataPath, CONFIG_FILENAME);
        if (!File.Exists(configPath)) return;
        try
        {
            string json = File.ReadAllText(configPath);
            ConfigData data = JsonConvert.DeserializeObject<ConfigData>(json);
            if (data?.MessageTypes != null)
            {
                loggedMessageTypes = [.. data.MessageTypes];
            }
            else
            {
                Logger.LogError("Logging config file is missing 'messageTypes' field");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load logging config: {ex.Message}");
        }
    }
}

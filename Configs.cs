using UnityEngine;
using BepInEx.Configuration;

namespace MyMycoModRoot;

internal static class Configs
{
    public static ConfigEntry<bool> Enabled { get; private set; }
    public static ConfigEntry<bool> IsOpenByDefault { get; private set; }

    internal static void Initialize(ConfigFile configFile)
    {
        Enabled = configFile.Bind(
            "Features",
            "Enabled",
            true,
            "If true, makes the mod do stuff.");
        
        IsOpenByDefault = configFile.Bind(
            "Preferences",
            "Is Open By Default",
            false,
            "If true, the mission list window will be open when you interact with the terminal.");
    }
}
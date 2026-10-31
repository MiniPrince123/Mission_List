using UnityEngine;
using BepInEx.Configuration;

namespace MyMycoModRoot;

internal static class Configs
{
    public static ConfigEntry<bool> Enabled { get; private set; }

    internal static void Initialize(ConfigFile configFile)
    {
        Enabled = configFile.Bind(
            "Features",
            "Enabled",
            true,
            "If true, makes the mod do stuff.");
    }
}
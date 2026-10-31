using UnityEngine;
using BepInEx.Configuration;

namespace MyMycoModRoot;

internal static class Configs
{
    public static ConfigEntry<bool> SideObjectivesRevealed { get; private set; }
    public static ConfigEntry<bool> NorthUp { get; private set; }
    public static ConfigEntry<short> ZoomAmount { get; private set; }
    // public static ConfigEntry<float> MapScale { get; private set; }

    internal static void Initialize(ConfigFile configFile)
    {
        SideObjectivesRevealed = configFile.Bind(
            "Features",
            "Reveal Side Objectives",
            true,
            "If true, reveals what each side objectives are without having to go near them.");

        NorthUp = configFile.Bind(
            "Features",
            "North Up",
            true,
            "If true, north will always point upwards.");
        
        ZoomAmount = configFile.Bind(
            "Features",
            "Zoom Amount",
            (short)180,
            new ConfigDescription(
                "Changes how zoomed in the minimap is. (lower = more zoomed in)",
                new AcceptableValueRange<short>(1, 512)
            ));
        ZoomAmount.SettingChanged += (_, _) => { ZoomAmount.Value = (short)Mathf.Clamp(ZoomAmount.Value, 1, 512); };
        
        // MapScale = configFile.Bind(
        //     "General",
        //     "Map Scale",
        //     1.0f,
        //     new ConfigDescription(
        //         "Changes the minimap size.",
        //         new AcceptableValueRange<float>(0.0f, 5.0f)
        //     ));
        // MapScale.SettingChanged += (_, _) => { MapScale.Value = Mathf.Clamp(MapScale.Value, 0.0f, 5.0f); };
    }
}
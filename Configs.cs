using BepInEx.Configuration;

namespace MyMycoModRoot;

internal static class Configs
{
    internal enum SortMethod { Biome, ModifierCount, MissionType }
    internal enum WeekliesEnum { Vanilla, HideAll, HideNone }

    public static ConfigEntry<bool> Enabled { get; private set; }
    public static ConfigEntry<SortMethod> SortBy { get; private set; }
    public static ConfigEntry<bool> IsOpenByDefault { get; private set; }
    public static ConfigEntry<bool> BiomeIconsColor { get; private set; }
    
    public static ConfigEntry<bool> Oxythane { get; private set; }
    public static ConfigEntry<bool> Freeroam { get; private set; }
    public static ConfigEntry<bool> Incursion { get; private set; }
    public static ConfigEntry<bool> Ouroboros { get; private set; }
    public static ConfigEntry<bool> Ouroblitz { get; private set; }
    public static ConfigEntry<WeekliesEnum> WeekliesVisibility { get; private set; }
    public static ConfigEntry<bool> LegacyMissions { get; private set; }
    public static ConfigEntry<bool> ReplayMissions { get; private set; }

    internal static void Initialize(ConfigFile configFile)
    {
        SortBy = configFile.Bind<SortMethod>(
            "Preferences",
            "Sorting Method",
            SortMethod.MissionType,
            "Determines how missions are sorted in the Mission List.");
        
        IsOpenByDefault = configFile.Bind<bool>(
            "Preferences",
            "Is Open By Default",
            false,
            "Automatically opens the Mission List when interacting with the terminal.");
        
        BiomeIconsColor = configFile.Bind<bool>(
            "Preferences",
            "Biome-Based Outline Color",
            false,
            "Colors mission's outlines based on their biome's color.");
        
        
        
        
        Freeroam = configFile.Bind<bool>(
            "Missions Visibility",
            "PTO",
            true,
            "Shows PTO in the Mission List.");
        
        Incursion = configFile.Bind<bool>(
            "Missions Visibility",
            "Incursion",
            true,
            "Shows Incursion in the Mission List.");
        
        Ouroboros = configFile.Bind<bool>(
            "Missions Visibility",
            "Ouroboros",
            false,
            "Shows Ouroboros in the Mission List.");
        
        Ouroblitz = configFile.Bind<bool>(
            "Missions Visibility",
            "Ouroblitz",
            false,
            "Shows Ouroboros Blitz in the Mission List.");
        
        Oxythane = configFile.Bind<bool>(
            "Missions Visibility",
            "Oxythane Breach",
            false,
            "Shows Oxythane Breach Escalation in the Mission List.");
        
        WeekliesVisibility = configFile.Bind<WeekliesEnum>(
            "Missions Visibility",
            "Completed Weeklies",
            WeekliesEnum.Vanilla,
            "Completed weeklies' visibility.");
        
        LegacyMissions = configFile.Bind<bool>(
            "Missions Visibility",
            "Include Extra Permanent Locations",
            false,
            "Adds the \"Permanent Locations\" tab's missions to the Mission List.");
        
        ReplayMissions = configFile.Bind<bool>(
            "Missions Visibility",
            "Include Replay Missions",
            false,
            "Adds the missions from the \"Replay Mission\" tab's to the Mission List.");
    }
}
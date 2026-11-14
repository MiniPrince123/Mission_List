using BepInEx.Configuration;

namespace MyMycoModRoot;

internal static class Configs
{
    internal enum SortMethod { Biome, ModifierCount, MissionType }
    internal enum WeekliesEnum { Vanilla, HideAll, HideNone }

    public static ConfigEntry<SortMethod> SortBy { get; private set; }
    public static ConfigEntry<bool> IsOpenByDefault { get; private set; }
    public static ConfigEntry<bool> BiomeIconsColor { get; private set; }
    
    public static ConfigEntry<bool> OxybreachE { get; private set; }
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
            "Paid Time Off",
            true,
            "Includes Paid Time Off in the Mission List.");
        
        Incursion = configFile.Bind<bool>(
            "Missions Visibility",
            "Incursion",
            true,
            "Includes Incursion in the Mission List.");
        
        Ouroboros = configFile.Bind<bool>(
            "Missions Visibility",
            "Ouroboros",
            false,
            "Includes Ouroboros in the Mission List.");
        
        Ouroblitz = configFile.Bind<bool>(
            "Missions Visibility",
            "Ouroboros Blitz",
            false,
            "Includes Ouroboros Blitz in the Mission List.");
        
        OxybreachE = configFile.Bind<bool>(
            "Missions Visibility",
            "Oxythane Breach: Escalation",
            false,
            "Includes Oxythane Breach: Escalation in the Mission List.");
        
        WeekliesVisibility = configFile.Bind<WeekliesEnum>(
            "Missions Visibility",
            "Completed Weeklies",
            WeekliesEnum.Vanilla,
            "How the completed weekly missions are shown.");
        
        LegacyMissions = configFile.Bind<bool>(
            "Missions Visibility",
            "Include Extra Permanent Locations",
            false,
            "Includes the \"Permanent Locations\" missions to the Mission List.");
        
        ReplayMissions = configFile.Bind<bool>(
            "Missions Visibility",
            "Include Replay Missions",
            false,
            "Includes the \"Replay Missions\" missions to the Mission List.");
    }
}
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

    internal static void Initialize(ConfigFile configFile)
    {
        Enabled = configFile.Bind<bool>(
            "Features",
            "Enabled",
            true,
            "Enables or disables the mod.");

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
            "Mission Type Visibility",
            "PTO",
            true,
            "Shows PTO in the Mission List.");
        
        Incursion = configFile.Bind<bool>(
            "Mission Type Visibility",
            "Incursion",
            true,
            "Shows Incursion in the Mission List.");
        
        Ouroboros = configFile.Bind<bool>(
            "Mission Type Visibility",
            "Ouroboros",
            false,
            "Shows Ouroboros in the Mission List.");
        
        Ouroblitz = configFile.Bind<bool>(
            "Mission Type Visibility",
            "Ouroblitz",
            false,
            "Shows Ouroboros Blitz in the Mission List.");
        
        Oxythane = configFile.Bind<bool>(
            "Mission Type Visibility",
            "Oxythane Breach",
            false,
            "Shows Oxythane Breach Escalation in the Mission List.");
        
        WeekliesVisibility = configFile.Bind<WeekliesEnum>(
            "Mission Type Visibility",
            "Completed Weeklies",
            WeekliesEnum.Vanilla,
            "Completed weeklies' visibility.");
    }
}
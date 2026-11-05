using BepInEx.Configuration;

namespace MyMycoModRoot;

internal static class Configs
{
    internal enum SortMethod { Biome, ModifierCount, MissionType }

    public static ConfigEntry<bool> Enabled { get; private set; }
    public static ConfigEntry<SortMethod> SortBy { get; private set; }
    public static ConfigEntry<bool> IsOpenByDefault { get; private set; }
    public static ConfigEntry<bool> BiomeIconsColor { get; private set; }
    
    public static ConfigEntry<bool> Oxythane { get; private set; }
    public static ConfigEntry<bool> Freeroam { get; private set; }
    public static ConfigEntry<bool> Incursion { get; private set; }
    public static ConfigEntry<bool> Ouroboros { get; private set; }
    public static ConfigEntry<bool> Ouroblitz { get; private set; }

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
            "Biome-Based Icons Color",
            false,
            "Colors mission icons based on their biome's color.");
        
        
        
        Oxythane = configFile.Bind<bool>(
            "Mission Type Visibility",
            "Oxythane Breach",
            true,
            "Shows Oxythane Breach Escalation in the Mission List.");
        
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
            true,
            "Shows Ouroboros in the Mission List.");
        
        Ouroblitz = configFile.Bind<bool>(
            "Mission Type Visibility",
            "Ouroblitz",
            true,
            "Shows Ouroboros Blitz in the Mission List.");
    }
}
using BepInEx.Configuration;

namespace MyMycoModRoot;

internal static class Configs
{
    internal enum SortMethod { Biome, ModifierCount, MissionType }

    public static ConfigEntry<bool> Enabled { get; private set; }
    public static ConfigEntry<SortMethod> SortBy { get; private set; }
    public static ConfigEntry<bool> IsOpenByDefault { get; private set; }
    public static ConfigEntry<bool> BiomeIconsColor { get; private set; }

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
    }
}
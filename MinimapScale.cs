using HarmonyLib;

namespace MyMycoModRoot;

[HarmonyPatch]
internal static class MinimapScale
{
    [HarmonyPatch(
        typeof(Minimap),
        nameof(Minimap.UpdateMinimap)
    )]
    [HarmonyPostfix]
    private static void Postfix(Minimap __instance)
    {
        // Changes map zoom amount
        __instance.minimapWorldRadius = Configs.ZoomAmount.Value;
    }
}
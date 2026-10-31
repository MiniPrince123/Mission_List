// using HarmonyLib;
// using UnityEngine;
// using Pigeon.Movement;
//
// namespace MyMycoModRoot;
//
// [HarmonyPatch]
// internal static class MinimapRotation
// {
//     [HarmonyPatch(
//         typeof(Minimap),
//         nameof(Minimap.UpdatePing)
//     )]
//     [HarmonyPrefix]
//     private static void Prefix(ref Quaternion rotation)
//     {
//         if (!Configs.NorthUp.Value)
//             return;
//
//         // Stops waypoints/pings from rotating
//         rotation = Quaternion.identity;
//     }
//     
//     [HarmonyPatch(
//         typeof(Minimap),
//         nameof(Minimap.UpdateMinimap)
//     )]
//     [HarmonyPostfix]
//     private static void Postfix(Minimap __instance)
//     {
//         if (!Configs.NorthUp.Value)
//             return;
//
//         // Stops the map image from rotating
//         __instance.mapRenderer.transform.localRotation = Quaternion.identity;
//         // Enables player icon to rotate
//         __instance.playerPointer.rectTransform.localRotation =
//             Quaternion.Euler(0.0f, 0.0f, -PlayerLook.Rotation.eulerAngles.y);
//         
//         
//         
//         // Changes map scale
//         // __instance.transform.localScale = new Vector3(Configs.MapScale.Value, Configs.MapScale.Value, Configs.MapScale.Value);
//         // __instance.transform.localPosition = new Vector3(Configs.MapScale.Value, Configs.MapScale.Value, Configs.MapScale.Value);
//         // Plugin.Logger.LogInfo("GetTopLeftPos : " + __instance.GetTopLeftPos());
//         // Plugin.Logger.LogInfo("transform.position : " + __instance.transform.position);
//         // Plugin.Logger.LogInfo("transform.localPosition : " + __instance.transform.localPosition);
//
//     }
// }
//
//

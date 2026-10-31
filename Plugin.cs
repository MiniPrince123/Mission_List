using HarmonyLib;
using BepInEx;
using BepInEx.Logging;

namespace MyMycoModRoot;

[MycoMod(null, ModFlags.IsClientSide)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    internal new static ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} ({MyPluginInfo.PLUGIN_GUID} {MyPluginInfo.PLUGIN_VERSION}) is loaded!");

        Configs.Initialize(Config);
        Logger.LogInfo("Config initialized: " +
                       $"SideObjectivesRevealed={Configs.SideObjectivesRevealed.Value}, "+
                       $"NorthUp={Configs.NorthUp.Value}, "+
                       $"ZoomAmount={Configs.ZoomAmount.Value}" 
        );

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        Logger.LogInfo("Harmony patches applied");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
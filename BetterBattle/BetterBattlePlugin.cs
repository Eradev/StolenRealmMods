using BepInEx;
using BepInEx.Logging;
using eradev.stolenrealm.BetterBattle.Features;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterBattle
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class BetterBattlePlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        [UsedImplicitly]
        private void Awake()
        {
            Log = Logger;

            AutoCastAuras.Register(Config);
            AutoCastSkills.Register(Config);
            ConvertExpToGold.Register(Config);
            RemoveBarrels.Register(Config);
            RemoveDropsLimit.Register(Config);

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}

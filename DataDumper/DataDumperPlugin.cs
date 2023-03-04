using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Burst2Flame;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DataDumper
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<string> _difficulties;
        private static ConfigEntry<string> _gamblingChance;
        private static ConfigEntry<string> _gamblingRolls;
        private static ConfigEntry<string> _itemMods;
        private static ConfigEntry<string> _fortunes;

        private void Awake()
        {
            _difficulties = Config.Bind("General", "Difficulties", string.Empty);
            _gamblingChance = Config.Bind("General", "Gambling chance", string.Empty);
            _gamblingRolls = Config.Bind("General", "Gambling rolls", string.Empty);
            _itemMods = Config.Bind("General", "Item mods", string.Empty);
            _fortunes = Config.Bind("General", "Fortunes", string.Empty);

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(OptionsManager), "Start")]
        public class OptionsManagerStartPatch
        {
            [UsedImplicitly]
            private static void Postfix()
            {
                var difficulties = GlobalSettingsManager.instance.difficultySettings.Difficulties
                    .ToDictionary(
                        difficulty => difficulty.DifficultyName,
                        difficulty => new DifficultyModifiers(difficulty));

                _difficulties.Value = JsonConvert.SerializeObject(difficulties);

                var gamblingChance = GamblingManager.Instance.RarityChances;

                _gamblingChance.Value = JsonConvert.SerializeObject(gamblingChance);

                var gamblingRolls = GamblingManager.Instance.GamblingRolls;

                _gamblingRolls.Value = JsonConvert.SerializeObject(gamblingRolls);

                var itemMods = Game.Instance.ItemMods;

                _itemMods.Value = JsonConvert.SerializeObject(itemMods);

                var fortunes = Game.Instance.Fortunes
                    .ToDictionary(
                        fortune => fortune.Name,
                        fortune => fortune.Guid);

                _fortunes.Value = JsonConvert.SerializeObject(fortunes);
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterExploration.Features
{
    [UsedImplicitly]
    public class BetterTreasures
    {
        private const bool IsBetterTreasuresEnabledDefault = false;
        private const string CmdToggleBetterTreasuresDefault = "t_bettertreasures";

        private static ConfigEntry<string> _cmdToggleBetterTreasures;
        private static ConfigEntry<bool> _isBetterTreasuresEnabled;

        public static void Register(ConfigFile config)
        {
            _isBetterTreasuresEnabled = config.Bind("General", "bettertreasures_enabled", IsBetterTreasuresEnabledDefault, "Enable better treasures");

            _cmdToggleBetterTreasures = config.Bind("Commands", "bettertreasures_toggle", CmdToggleBetterTreasuresDefault, "Toggle better treasures");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleBetterTreasures);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleBetterTreasures.Value))
                {
                    _isBetterTreasuresEnabled.Value = !_isBetterTreasuresEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isBetterTreasuresEnabled.Value ? "enabled" : "disabled")} better treasures",
                        PluginInfo.PLUGIN_NAME);
                }
            };
        }

        [HarmonyPatch(typeof(EventOption), "InitActions")]
        public class EvenOptionInitActionsPatch
        {
            [UsedImplicitly]
            private static void Prefix(EventOption __instance)
            {
                if (!_isBetterTreasuresEnabled.Value)
                {
                    return;
                }

                foreach (var eventAction in __instance.eventActions)
                {
                    foreach (var eventActionEffect in eventAction.EventActionEffects.Where(x => x.giveItems && x.itemPoolType != UniversalLootTableType.None))
                    {
                        eventActionEffect.lootRestrictions.limitItemRarities = true;
                        eventActionEffect.lootRestrictions.possibleRarities = new List<ItemQuality>
                        {
                            ItemQuality.Legendary,
                            ItemQuality.Mythic
                        };
                    }
                }
            }
        }
    }
}

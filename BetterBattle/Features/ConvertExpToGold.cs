using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterBattle.Features
{
    [UsedImplicitly]
    public class ConvertExpToGold
    {
        private const bool IsConvertExpGoldEnabledDefault = true;
        private const string CmdToggleConvertExpGoldDefault = "t_expgold";

        private static ConfigEntry<string> _cmdToggleConvertExpGold;
        private static ConfigEntry<bool> _isConvertExpGoldEnabled;

        public static void Register(ConfigFile config)
        {
            _isConvertExpGoldEnabled = config.Bind("General", "convertexpgold_enabled", IsConvertExpGoldEnabledDefault,
                "Enable convert EXP to gold when your character reached max level");

            _cmdToggleConvertExpGold = config.Bind("Commands", "convertexpgold_toggle", CmdToggleConvertExpGoldDefault,
                "Toggle convert EXP to gold when your character reached max level");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleConvertExpGold);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleConvertExpGold.Value))
                {
                    _isConvertExpGoldEnabled.Value = !_isConvertExpGoldEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isConvertExpGoldEnabled.Value ? "enabled" : "disabled")} convert EXP to gold",
                        PluginInfo.PLUGIN_NAME);
                }
            };

        }

        [HarmonyPatch(typeof(Character), "GiveExperience")]
        public class CharacterGiveExperiencePatch
        {
            [UsedImplicitly]
            private static bool Prefix(Character __instance, float expValue, bool showMessage)
            {
                if (!_isConvertExpGoldEnabled.Value || __instance.Level < __instance.MaxLevel)
                {
                    return true;
                }

                __instance.GiveGold(expValue, 1f, showMessage);

                return false;
            }
        }
    }
}

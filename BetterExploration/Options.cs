using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;

namespace eradev.stolenrealm.BetterExploration
{
    public static class Options
    {
        private const bool IsBetterTreasuresEnabledDefault = false;
        private const string CmdToggleBetterTreasuresDefault = "t_bettertreasures";

        private const bool IsCustomizedMovementSpeedEnabledDefault = true;
        private const string CmdSetCustomizedMovementSpeedDefault = "set_ms";
        private const string CmdToggleCustomizedMovementSpeedDefault = "t_ms";
        private const float SpeedMultiplierDefault = 1.5f;

        private const bool IsMiniGamesAutoCompletionEnabledDefault = true;
        private const string CmdToggleMiniGamesAutoCompletionDefault = "t_gathering";

        private const bool IsUnlockFortunePartyEnabledDefault = true;
        private const string CmdToggleUnlockFortunePartyDefault = "t_fortune4party";

        private static ConfigEntry<string> CmdToggleBetterTreasures;
        public static ConfigEntry<bool> IsBetterTreasuresEnabled;

        private static ConfigEntry<string> _cmdSetCustomizedMovementSpeed;
        private static ConfigEntry<string> _cmdToggleCustomizedMovementSpeed;
        public static ConfigEntry<bool> IsCustomizedMovementSpeedEnabled;
        public static ConfigEntry<float> SpeedMultiplier;

        private static ConfigEntry<string> _cmdToggleMiniGamesAutoCompletion;
        public static ConfigEntry<bool> IsMiniGamesAutoCompletionEnabled;

        private static ConfigEntry<string> _cmdToggleUnlockFortuneParty;
        public static ConfigEntry<bool> IsUnlockFortunePartyEnabled;

        public static void Register(ConfigFile config)
        {
            IsBetterTreasuresEnabled = config.Bind("General", "bettertreasures_enabled", IsBetterTreasuresEnabledDefault, "Enable better treasures");
            IsCustomizedMovementSpeedEnabled = config.Bind("General", "cuztomizedmovementspeed_enabled", IsCustomizedMovementSpeedEnabledDefault, "Enable customized movement speed");
            SpeedMultiplier = config.Bind("General", "customizedmovementspeed_mult", SpeedMultiplierDefault, "Speed multiplier (Min 1.0)");
            if (SpeedMultiplier.Value < 1.0f)
            {
                SpeedMultiplier.Value = 1.0f;
            }
            IsMiniGamesAutoCompletionEnabled = config.Bind("General", "minigamesautocompletion_enabled", IsMiniGamesAutoCompletionEnabledDefault, "Enable mini-games auto-completion");
            IsUnlockFortunePartyEnabled = config.Bind("General", "unlockfortuneparty_enabled", IsUnlockFortunePartyEnabledDefault,
                "Enable unlock fortune for your party");

            CmdToggleBetterTreasures = config.Bind("Commands", "bettertreasures_toggle", CmdToggleBetterTreasuresDefault, "Toggle better treasures");
            _cmdSetCustomizedMovementSpeed = config.Bind("Commands", "customizedmovementspeed_set", CmdSetCustomizedMovementSpeedDefault, "[Args (float, min 1.0)] Set the movement speed multiplier");
            _cmdToggleCustomizedMovementSpeed = config.Bind("Commands", "customizedmovementspeed_toggle", CmdToggleCustomizedMovementSpeedDefault, "Toggle customized movement speed");
            _cmdToggleMiniGamesAutoCompletion = config.Bind("Commands", "minigamesautocompletion_toggle", CmdToggleMiniGamesAutoCompletionDefault, "Toggle mini-games auto-completion");
            _cmdToggleUnlockFortuneParty = config.Bind("Commands", "unlockfortuneparty_toggle", CmdToggleUnlockFortunePartyDefault, "Toggle unlock fortune for your party");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref CmdToggleBetterTreasures);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdSetCustomizedMovementSpeed);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleCustomizedMovementSpeed);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleMiniGamesAutoCompletion);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleUnlockFortuneParty);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(CmdToggleBetterTreasures.Value))
                {
                    IsBetterTreasuresEnabled.Value = !IsBetterTreasuresEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(IsBetterTreasuresEnabled.Value ? "enabled" : "disabled")} better treasures",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleCustomizedMovementSpeed.Value))
                {
                    IsCustomizedMovementSpeedEnabled.Value = !IsCustomizedMovementSpeedEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(IsCustomizedMovementSpeedEnabled.Value ? "enabled" : "disabled")} customized movement speed",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdSetCustomizedMovementSpeed.Value))
                {
                    if (command.Args.Count < 1 || !float.TryParse(command.Args[0], out var newValue) || newValue < 1.0f)
                    {
                        CommandHandler.DisplayError("You must specify a value greater or equal to 1.0");

                        return;
                    }

                    SpeedMultiplier.Value = newValue;

                    CommandHandler.DisplayMessage($"Successfully set the speed multiplier to {newValue}", PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleMiniGamesAutoCompletion.Value))
                {
                    IsMiniGamesAutoCompletionEnabled.Value = !IsMiniGamesAutoCompletionEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(IsMiniGamesAutoCompletionEnabled.Value ? "enabled" : "disabled")} mini-games auto-completion",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleUnlockFortuneParty.Value))
                {
                    IsUnlockFortunePartyEnabled.Value = !IsUnlockFortunePartyEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(IsUnlockFortunePartyEnabled.Value ? "enabled" : "disabled")} unlock fortune for party",
                        PluginInfo.PLUGIN_NAME);
                }
            };
        }
    }
}

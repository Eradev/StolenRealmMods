using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterExploration.Features
{
    [UsedImplicitly]
    public class CustomizedMovementSpeed
    {
        private const bool IsCustomizedMovementSpeedEnabledDefault = true;
        private const string CmdSetCustomizedMovementSpeedDefault = "set_ms";
        private const string CmdToggleCustomizedMovementSpeedDefault = "t_ms";
        private const float SpeedMultiplierDefault = 1.5f;

        private static ConfigEntry<string> _cmdSetCustomizedMovementSpeed;
        private static ConfigEntry<string> _cmdToggleCustomizedMovementSpeed;
        private static ConfigEntry<bool> _isCustomizedMovementSpeedEnabled;
        private static ConfigEntry<float> _speedMultiplier;

        public static void Register(ConfigFile config)
        {
            _isCustomizedMovementSpeedEnabled = config.Bind("General", "cuztomizedmovementspeed_enabled", IsCustomizedMovementSpeedEnabledDefault, "Enable customized movement speed");
            _speedMultiplier = config.Bind("General", "customizedmovementspeed_mult", SpeedMultiplierDefault, "Speed multiplier (Min 1.0)");
            if (_speedMultiplier.Value < 1.0f)
            {
                _speedMultiplier.Value = 1.0f;
            }

            _cmdSetCustomizedMovementSpeed = config.Bind("Commands", "customizedmovementspeed_set", CmdSetCustomizedMovementSpeedDefault, "[Args (float, min 1.0)] Set the movement speed multiplier");
            _cmdToggleCustomizedMovementSpeed = config.Bind("Commands", "customizedmovementspeed_toggle", CmdToggleCustomizedMovementSpeedDefault, "Toggle customized movement speed");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdSetCustomizedMovementSpeed);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleCustomizedMovementSpeed);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleCustomizedMovementSpeed.Value))
                {
                    _isCustomizedMovementSpeedEnabled.Value = !_isCustomizedMovementSpeedEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isCustomizedMovementSpeedEnabled.Value ? "enabled" : "disabled")} customized movement speed",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdSetCustomizedMovementSpeed.Value))
                {
                    if (command.Args.Count < 1 || !float.TryParse(command.Args[0], out var newValue) || newValue < 1.0f)
                    {
                        CommandHandler.DisplayError("You must specify a value greater or equal to 1.0");

                        return;
                    }

                    _speedMultiplier.Value = newValue;

                    CommandHandler.DisplayMessage($"Successfully set the speed multiplier to {newValue}", PluginInfo.PLUGIN_NAME);
                }
            };
        }

        [HarmonyPatch(typeof(GlobalSettings), "GetMovementSpeedMod")]
        public class GlobalSettingsGetMovementSpeedModPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref float __result)
            {
                if (!_isCustomizedMovementSpeedEnabled.Value)
                {
                    return;
                }

                __result *= _speedMultiplier.Value;
            }
        }
    }
}

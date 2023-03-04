using BepInEx;
using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.FasterMovementSpeed
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class FasterMovementSpeedPlugin : BaseUnityPlugin
    {
        private const bool IsDisabledDefault = false;
        private const string CmdSetDefault = "set_ms";
        private const string CmdToggleDefault = "t_ms";
        private const float SpeedMultiplierDefault = 1.5f;

        private static ConfigEntry<string> _cmdSet;
        private static ConfigEntry<string> _cmdToggle;
        private static ConfigEntry<bool> _isDisabled;
        private static ConfigEntry<float> _speedMultiplier;

        [UsedImplicitly]
        private void Awake()
        {
            _isDisabled = Config.Bind("General", "disabled", IsDisabledDefault, "Mod is disabled");
            _speedMultiplier = Config.Bind("General", "speedMult", SpeedMultiplierDefault, "Speed multiplier (Min 1.0)");

            if (_speedMultiplier.Value < 1.0f)
            {
                _speedMultiplier.Value = 1.0f;
            }

            _cmdSet = Config.Bind("Commands", "set", CmdSetDefault, "[Args (float, min 1.0)] Set the movement speed multiplier");
            _cmdToggle = Config.Bind("Commands", "toggle", CmdToggleDefault, $"Toggle {PluginInfo.PLUGIN_NAME} on/off");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdSet);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggle);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggle.Value))
                {
                    _isDisabled.Value = !_isDisabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isDisabled.Value ? "disabled" : "enabled")}",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdSet.Value))
                {
                    if (command.Args.Count < 1 || !float.TryParse(command.Args[0], out var newValue) || newValue < 1.0f)
                    {
                        CommandHandler.DisplayError("You must specify a value greater or equal to 1.0");

                        return;
                    }

                    _speedMultiplier.Value = newValue;

                    CommandHandler.DisplayMessage($"Successfully set the speed multiplier to {newValue}",
                        PluginInfo.PLUGIN_NAME);
                }
            };

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(GlobalSettings), "GetMovementSpeedMod")]
        public class GlobalSettingsGetMovementSpeedModPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref float __result)
            {
                if (_isDisabled.Value)
                {
                    return;
                }

                __result *= _speedMultiplier.Value;
            }
        }
    }
}

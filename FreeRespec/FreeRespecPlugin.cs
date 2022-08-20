using BepInEx;
using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.FreeRespec
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class FreeRespecPlugin : BaseUnityPlugin
    {
        private const bool IsDisabledDefault = false;
        private const string CmdToggleDefault = "t_respec";

        private static ConfigEntry<string> _cmdToggle;
        private static ConfigEntry<bool> _isDisabled;

        [UsedImplicitly]
        private void Awake()
        {
            _isDisabled = Config.Bind("General", "disabled", IsDisabledDefault, "Mod is disabled");

            _cmdToggle = Config.Bind("Commands", "toggle", CmdToggleDefault, $"Toggle {PluginInfo.PLUGIN_NAME} on/off");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggle);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggle.Value))
                {
                    _isDisabled.Value = !_isDisabled.Value;

                    CommandHandler.DisplayMessage($"{PluginInfo.PLUGIN_NAME}: Successfully {(_isDisabled.Value ? "disabled" : "enabled")}");
                }
            };

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(SkillRefundManager), "RespecCost", MethodType.Getter)]
        public class SkillRefundManagerRespecCostPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref float __result)
            {
                if (_isDisabled.Value)
                {
                    return;
                }

                __result = 0.0f;
            }
        }
    }
}

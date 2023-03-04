using BepInEx;
using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.StatPoints
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class StatPointsPlugin : BaseUnityPlugin
    {
        private const string CmdSetDefault = "set_stpl";
        private const int StatPointsPerLevelDefault = 5;

        private static ConfigEntry<string> _cmdSet;
        private static ConfigEntry<int> _statPointsPerLevel;

        [UsedImplicitly]
        private void Awake()
        {
            _statPointsPerLevel = Config.Bind("General", "Stat points per level", StatPointsPerLevelDefault, "Stat points per level");

            if (_statPointsPerLevel.Value < 0)
            {
                _statPointsPerLevel.Value = 5;
            }

            _cmdSet = Config.Bind("Commands", "set", CmdSetDefault, "[Args (float, >= 0.0)] Set the number of stat points per level");

            CommandHandler.RegisterCommandsEvt += (_, _) => { CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdSet); };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdSet.Value))
                {
                    if (command.Args.Count < 1 || !int.TryParse(command.Args[0], out var newValue) || newValue < 0)
                    {
                        CommandHandler.DisplayError("You must specify a value greater or equal to 0");

                        return;
                    }

                    _statPointsPerLevel.Value = newValue;

                    GlobalSettingsManager.instance.globalSettings.NumStatsPerNewLevel = newValue;

                    GUIManager.instance.UpdateUnspentPointsUI();

                    CommandHandler.DisplayMessage($"Successfully set the stat points per level to {newValue}", PluginInfo.PLUGIN_NAME);
                }
            };

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(Character), "UnspentStatPoints", MethodType.Getter)]
        public class CharacterUnspentSkillPointsPatch
        {
            [UsedImplicitly]
            private static void Prefix()
            {
                GlobalSettingsManager.instance.globalSettings.NumStatsPerNewLevel = _statPointsPerLevel.Value;
            }
        }
    }
}

using System.Linq;
using BepInEx.Configuration;
using Burst2Flame;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterBattle.Features
{
    [UsedImplicitly]
    public class RemoveBarrels
    {
        private const bool IsRemoveBarrelsEnabledDefault = false;
        private const string CmdToggleRemoveBarrelsDefault = "t_removebarrels";

        private static ConfigEntry<string> _cmdToggleRemoveBarrels;
        private static ConfigEntry<bool> _isRemoveBarrelsEnabled;

        private static DestructibleSpawnInfo[] _defaultDestructibleSpawnInfos;

        public static void Register(ConfigFile config)
        {
            _isRemoveBarrelsEnabled = config.Bind("General", "removebarrels_enabled", IsRemoveBarrelsEnabledDefault,
                "Enable the removal of barrels");

            _cmdToggleRemoveBarrels = config.Bind("Commands", "removebarrels_toggle", CmdToggleRemoveBarrelsDefault,
                "Toggle the removal of barrels");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleRemoveBarrels);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleRemoveBarrels.Value))
                {
                    _isRemoveBarrelsEnabled.Value = !_isRemoveBarrelsEnabled.Value;

                    ApplyRemoveBarrelChange();

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isRemoveBarrelsEnabled.Value ? "enabled" : "disabled")} the removal of barrels",
                        PluginInfo.PLUGIN_NAME);
                }
            };
        }

        [HarmonyPatch(typeof(Game), "Awake")]
        public class GameAwakePatch
        {
            [UsedImplicitly]
            private static void Postfix()
            {
                ApplyRemoveBarrelChange();
            }
        }

        private static void ApplyRemoveBarrelChange()
        {
            var globalSettings = GlobalSettingsManager.instance.globalSettings;

            if (_defaultDestructibleSpawnInfos == null)
            {
                _defaultDestructibleSpawnInfos = globalSettings.DestructibleSpawnSettings;
            }
            else
            {
                globalSettings.DestructibleSpawnSettings = _defaultDestructibleSpawnInfos;
            }

            if (_isRemoveBarrelsEnabled.Value)
            {
                globalSettings.DestructibleSpawnSettings =
                    globalSettings.DestructibleSpawnSettings.Where(x => x.DestructibleType != DestructibleType.Barrel)
                        .ToArray();
            }
        }
    }
}

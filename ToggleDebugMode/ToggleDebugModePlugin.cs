using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace eradev.stolenrealm.ToggleDebugMode
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ToggleDebugModePlugin : BaseUnityPlugin
    {
        private const bool IsEnabledDefault = false;
        private const string CmdDisplayDefault = "debug";
        private const string CmdToggleDefault = "t_debug";

        private static ConfigEntry<bool> _isEnabled;
        private static ConfigEntry<string> _cmdDisplay;
        private static ConfigEntry<string> _cmdToggle;

        private static readonly List<string> DebugKeys = new()
        {
            "DebugModeEnabled",
            "DebugMode:ToggleUnlockAllQuests",
            "DebugMode:ToggleUnlockMapNodes",
            "DebugMode:ToggleUnlockAllSkills",
            "DebugMode:ToggleFastMovement",
            "DebugMode:ToggleFreeCasting",
            "DebugMode:ToggleFreeMovement",
            "DebugMode:ToggleInvincibility",
            "DebugMode:ToggleFreeCrafting",
            "DebugMode:ToggleDamage_X10",
            "DebugMode:ToggleInfMoveObjectRange",
            "DebugMode:ToggleNeverExpendEvents",
            "DebugMode:ToggleHideTextEvents",
            "DebugMode:ToggleForceEvent",
            "DebugMode:InputEventRollResults",
            "DebugMode:EventRollValue",
            "DebugMode:ToggleForceProfession",
            "DebugMode:ToggleForceDestructible",
            "DebugMode:ToggleForceEnemyMod",
            "DebugMode:ProfessionToForce",
            "DebugMode:EnemyModToForce",
            "DebugMode:EventToForce",
            "DebugMode:DestructibleToForce",
            "DebugMode:StatusToGive"
        };

        private static ManualLogSource _log;

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

            _isEnabled = Config.Bind("General", "enabled", IsEnabledDefault, "Enable the debug mode");
            _cmdDisplay = Config.Bind("Commands", "display", CmdDisplayDefault, "Activate the debug mode, and display the debug window");
            _cmdToggle = Config.Bind("Commands", "toggle", CmdToggleDefault, "Toggle the debug mode");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdDisplay);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggle);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdDisplay.Value))
                {
                    if (!NetworkingManager.Instance.IsServer)
                    {
                        CommandHandler.DisplayError("Only the host can activate the debug mode.");

                        return;
                    }

                    EnableDebugMode();

                    DebugWindow.Instance.ShowDebugWindow();

                    CommandHandler.BroadcastMessage("The host opened the debug menu.", PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggle.Value))
                {
                    if (!NetworkingManager.Instance.IsServer)
                    {
                        CommandHandler.DisplayError("Only the host can activate the debug mode.");

                        return;
                    }

                    if (_isEnabled.Value)
                    {
                        DisableDebugMode();
                    }
                    else
                    {
                        EnableDebugMode();

                        DebugWindow.Instance.ShowDebugWindow();

                        CommandHandler.BroadcastMessage("The host opened the debug menu.", PluginInfo.PLUGIN_NAME);
                    }
                }
            };

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void EnableDebugMode()
        {
            _isEnabled.Value = true;

            if (DebugWindow.Instance == null)
            {
                DebugWindow.LoadInstanceReference();
            }

            AccessTools.FieldRefAccess<bool>(typeof(DebugWindow), "debugActivated")
                .Invoke(DebugWindow.Instance) = true;

            PlayerPrefs.SetString("DebugModeEnabled", "TRUE");
        }

        private static void DisableDebugMode()
        {
            DebugWindow.Instance.HideDebugWindow();

            _isEnabled.Value = false;

            AccessTools.FieldRefAccess<bool>(typeof(DebugWindow), "debugActivated").Invoke(DebugWindow.Instance) = false;

            foreach (var key in DebugKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            DebugWindow.Instance.UnlockAllQuestsToggle.isOn = false;
            DebugWindow.Instance.UnlockMapNodesToggle.isOn = false;
            DebugWindow.Instance.UnlockAllSkillsToggle.isOn = false;
            DebugWindow.Instance.FastMovementToggle.isOn = false;
            DebugWindow.Instance.FreeCastingToggle.isOn = false;
            DebugWindow.Instance.FreeMovementToggle.isOn = false;
            DebugWindow.Instance.InvincibilityToggle.isOn = false;
            DebugWindow.Instance.FreeCraftingToggle.isOn = false;
            DebugWindow.Instance.Damage_X10Toggle.isOn = false;
            DebugWindow.Instance.InfMoveObjectRangeToggle.isOn = false;
            DebugWindow.Instance.InputEventRollResultToggle.isOn = false;
            DebugWindow.Instance.ForceEventToggle.isOn = false;
            DebugWindow.Instance.ForceProfessionToggle.isOn = false;
            DebugWindow.Instance.ForceDestructibleToggle.isOn = false;
            DebugWindow.Instance.ForceEnemyModToggle.isOn = false;
            DebugWindow.Instance.NeverExpendEventsToggle.isOn = false;
            DebugWindow.Instance.HideTextEventsToggle.isOn = false;
            DebugWindow.Instance.HideHealthbarsToggle.isOn = false;
            DebugWindow.Instance.HideHexBordersToggle.isOn = false;
            DebugWindow.Instance.HideCharacterHoverInfoToggle.isOn = false;
            DebugWindow.Instance.HideBasicUIToggle.isOn = false;
            DebugWindow.Instance.UnlockRoguelikeDifficultiesToggle.isOn = false;
            DebugWindow.Instance.UnlockRoguelikePresetsToggle.isOn = false;

            foreach (var instanceTierToggle in DebugWindow.Instance.TierToggles)
            {
                instanceTierToggle.isOn = false;
            }

            CommandHandler.BroadcastMessage("The host disabled the debug mode.", PluginInfo.PLUGIN_NAME);
        }
    }
}

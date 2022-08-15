using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
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
            "DebugMode:ToggleNewStoryQuests",
            "DebugMode:ToggleForceEvent",
            "DebugMode:ToggleForceProfession",
            "DebugMode:ToggleForceDestructible",
            "DebugMode:ToggleForceEnemyMod",
            "DebugMode:ToggleUnlockAllQuests",
            "DebugMode:ToggleUnlockMapNodes"
        };

        [UsedImplicitly]
        private void Awake()
        {
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
                    if (!_isEnabled.Value)
                    {
                        EnableDebugMode();
                    }

                    DebugWindow.instance.ShowDebugWindow();
                }
                else if (command.Name.Equals(_cmdToggle.Value))
                {
                    if (_isEnabled.Value)
                    {
                        DisableDebugMode();
                    }
                    else
                    {
                        EnableDebugMode();

                        DebugWindow.instance.ShowDebugWindow();
                    }
                }
            };

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void EnableDebugMode()
        {
            _isEnabled.Value = true;

            AccessTools.FieldRefAccess<bool>(typeof(DebugWindow), "debugActivated").Invoke(DebugWindow.instance) = true;

            PlayerPrefs.SetString("DebugModeEnabled", "TRUE");

            CommandHandler.DisplayMessage("Successfully enabled the debug mode.");
        }

        private static void DisableDebugMode()
        {
            DebugWindow.instance.HideDebugWindow();

            _isEnabled.Value = false;

            AccessTools.FieldRefAccess<bool>(typeof(DebugWindow), "debugActivated").Invoke(DebugWindow.instance) = false;

            foreach (var key in DebugKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            DebugWindow.instance.UnlockAllSkillsToggle.isOn = false;
            DebugWindow.instance.FastMovementToggle.isOn = false;
            DebugWindow.instance.FreeCastingToggle.isOn = false;
            DebugWindow.instance.FreeMovementToggle.isOn = false;
            DebugWindow.instance.InvincibilityToggle.isOn = false;
            DebugWindow.instance.FreeCraftingToggle.isOn = false;
            DebugWindow.instance.Damage_X10Toggle.isOn = false;
            DebugWindow.instance.InfMoveObjectRangeToggle.isOn = false;
            DebugWindow.instance.NeverExpendEventsToggle.isOn = false;
            DebugWindow.instance.HideTextEventsToggle.isOn = false;
            DebugWindow.instance.NewStoryQuestsToggle.isOn = false;
            DebugWindow.instance.ForceEventToggle.isOn = false;
            DebugWindow.instance.ForceProfessionToggle.isOn = false;
            DebugWindow.instance.ForceDestructibleToggle.isOn = false;
            DebugWindow.instance.ForceEnemyModToggle.isOn = false;
            DebugWindow.instance.UnlockAllQuestsToggle.isOn = false;
            DebugWindow.instance.UnlockMapNodesToggle.isOn = false;

            CommandHandler.DisplayMessage("Successfully disabled the debug mode.");
        }
    }
}

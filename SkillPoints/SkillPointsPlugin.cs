using System;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.SkillPoints
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class SkillPointsPlugin : BaseUnityPlugin
    {
        private const string CmdSetDefault = "set_spl";
        private const float SkillPointsPerLevelDefault = 1.0f;

        private static ConfigEntry<string> _cmdSet;
        private static ConfigEntry<float> _skillPointsPerLevel;

        [UsedImplicitly]
        private void Awake()
        {
            _skillPointsPerLevel = Config.Bind("General", "Skills points per level", SkillPointsPerLevelDefault, "Skill points per level");

            if (_skillPointsPerLevel.Value < 0.0f)
            {
                _skillPointsPerLevel.Value = 1.0f;
            }

            _cmdSet = Config.Bind("Commands", "set", CmdSetDefault, "[Args (float, >= 0.0)] Set the number of skill points per level");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdSet);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdSet.Value))
                {
                    if (command.Args.Count < 1 || !float.TryParse(command.Args[0], out var newValue) || newValue < 0.0f)
                    {
                        CommandHandler.DisplayError("You must specify a value greater or equal to 0.0");

                        return;
                    }

                    _skillPointsPerLevel.Value = newValue;

                    var refreshUnspentSkillPoints = AccessTools.FieldRefAccess<bool>(typeof(Character), "refreshUnspentSkillPoints");

                    foreach (var instanceControlledCharacter in GameLogic.instance.ControlledCharacters)
                    {
                        refreshUnspentSkillPoints.Invoke(instanceControlledCharacter) = true;
                    }
                    GUIManager.instance.UpdateUnspentPointsUI();

                    CommandHandler.DisplayMessage($"Successfully set the skill points per level to {newValue}", PluginInfo.PLUGIN_NAME);
                }
            };

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(Character), "UnspentSkillPoints", MethodType.Getter)]
        public class CharacterUnspentSkillPointsPatch
        {
            [UsedImplicitly]
            private static void Prefix(
                // ReSharper disable once RedundantAssignment
                ref bool __state,
                bool ___refreshUnspentSkillPoints)
            {
                __state = ___refreshUnspentSkillPoints;
            }

            [UsedImplicitly]
            private static void Postfix(
                ref Character __instance,
                ref int __result,
                bool __state,
                ref int ___unspentSkillPoints)
            {
                if (!__state)
                {
                    return;
                }

                var skillPointsFromLevels = (int)Math.Floor((__instance.Level - 1) * _skillPointsPerLevel.Value);
                var spentSkillPoints = __instance.SkillsFromPoints.Sum(x => GlobalSettingsManager.instance.globalSettings.skillsTierCosts[x.Tier - 1]);

                ___unspentSkillPoints = skillPointsFromLevels + 3 - spentSkillPoints;

                __result = ___unspentSkillPoints;
            }
        }

        [HarmonyPatch(typeof(SkillRefundManager), "SelectedRefundCharacter", MethodType.Setter)]
        public class SkillRefundManagerSelectedRefundCharacterPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref SkillRefundManager __instance)
            {
                var character = __instance.SelectedRefundCharacter;
                var characterDetails = __instance.CharacterDetail;
                var skillPointsFromLevels = (int)Math.Floor((character.Level - 1) * _skillPointsPerLevel.Value);

                var skillPointsRegex = new Regex("Skill Points: \\d+");

                characterDetails.text = skillPointsRegex.Replace(characterDetails.text, $"Skill Points: {skillPointsFromLevels + 3}");
            }
        }
    }
}

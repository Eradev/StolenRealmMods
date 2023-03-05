using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Burst2Flame;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.CheatDetector
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class CheatDetectorPlugin : BaseUnityPlugin
    {
        private const string CmdCheckCheatersDefault = "cheaters";

        private static ConfigEntry<string> _cmdCheckCheaters;

        // ReSharper disable once NotAccessedField.Local
        private static ManualLogSource _log;

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

            _cmdCheckCheaters = Config.Bind("Commands", "check_cheaters", CmdCheckCheatersDefault,
                "Check if any connected characters are cheating. (Host only)");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdCheckCheaters);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdCheckCheaters.Value))
                {
                    if (!NetworkingManager.Instance.IsServer)
                    {
                        CommandHandler.DisplayError("Only the host can execute this command.");

                        return;
                    }

                    CheckForCheaters();
                }
            };


            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void CheckForCheaters()
        {
            var connectedCharacters = NetworkingManager.Instance.AllPlayerCharacters;

            var cheaters = new Dictionary<string, List<string>>();

            foreach (var character in connectedCharacters)
            {
                var detectedCheats = CheckForCheats(character);

                if (detectedCheats.Any())
                {
                    cheaters.Add(character.CharacterName, detectedCheats);
                }
            }

            if (cheaters.Count > 0)
            {
                CommandHandler.DisplayMessage(
                    $"Cheater{(cheaters.Count > 1 ? "s" : "")} detected!", PluginInfo.PLUGIN_NAME);

                foreach (var cheaterKvp in cheaters)
                {
                    CommandHandler.DisplayMessage(
                        $"   <color=red>{cheaterKvp.Key}</color> : {string.Join(", ", cheaterKvp.Value)}");
                }
            }

            CommandHandler.DisplayMessage("No cheaters detected!", PluginInfo.PLUGIN_NAME);
        }

        private static List<string> CheckForCheats(Character character)
        {
            var detectedCheats = new List<string>();

            var normalSkillPoints = character.Level - 1 + 3;
            var usedSkillPoints =
                character.SkillsFromPoints.Sum(x => GlobalSettingsManager.instance.globalSettings.skillsTierCosts[x.Tier - 1]);

            if (usedSkillPoints > normalSkillPoints)
            {
                detectedCheats.Add("Skill points");
            }

            var normalStatPoints = GlobalSettingsManager.instance.globalSettings.creationStatTotal + (character.Level - 1) * 5;
            var usedStatPoints =
                Game.Instance.LevelableCharacterAttributes.Sum(characterAttribute => (int)character.SavedMap[characterAttribute.Guid]);

            if (usedStatPoints > normalStatPoints)
            {
                detectedCheats.Add("Stat points");
            }

            return detectedCheats;
        }
    }
}

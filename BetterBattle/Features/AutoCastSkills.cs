using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Burst2Flame;
using Burst2Flame.Observable;
using eradev.stolenrealm.BetterBattle.Shared;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterBattle.Features
{
    [UsedImplicitly]
    public class AutoCastSkills
    {
        private const bool IsAutoCastSkillsEnabledDefault = true;
        private const string CmdToggleAutoCastSkillsDefault = "t_autocastskills";
        private const string CmdAddAutoCastSkillsDefault = "add_acs";
        private const string CmdRemoveAutoCastSkillsDefault = "remove_acs";
        private const string CmdClearAutoCastSkillsDefault = "clear_acs";

        private static ConfigEntry<string> _cmdToggleAutoCastSkills;
        private static ConfigEntry<string> _cmdAddAutoCastSkills;
        private static ConfigEntry<string> _cmdRemoveAutoCastSkills;
        private static ConfigEntry<string> _cmdClearAutoCastSkills;
        private static ConfigEntry<bool> _isAutoCastSkillsEnabled;
        private static ConfigEntry<string> _autoCastSkills;

        private static List<string> _autoCastListCache;
        private static bool _autoCastSkillsRanOnce;

        public static void Register(ConfigFile config)
        {
            _isAutoCastSkillsEnabled = config.Bind("General", "autocastskills_enabled", IsAutoCastSkillsEnabledDefault,
                "Enable auto-cast skills whenever available");
            _autoCastSkills = config.Bind("General", "autocastskills", string.Empty,
                "List of skills to auto-cast (GUID)");

            _cmdToggleAutoCastSkills =
                config.Bind("Commands", "autocastskills_toggle", CmdToggleAutoCastSkillsDefault, "Toggle auto-cast skills");
            _cmdAddAutoCastSkills =
                config.Bind("Commands", "autocastskills_add", CmdAddAutoCastSkillsDefault, "[Args (string, name or GUID] Add skill to auto-cast list");
            _cmdRemoveAutoCastSkills =
                config.Bind("Commands", "autocastskills_remove", CmdRemoveAutoCastSkillsDefault, "[Args (string, name or GUID)] Remove skill from auto-cast list");
            _cmdClearAutoCastSkills =
                config.Bind("Commands", "autocastskills_clear", CmdClearAutoCastSkillsDefault, "Remove all skills from auto-cast list");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleAutoCastSkills);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdAddAutoCastSkills);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdRemoveAutoCastSkills);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdClearAutoCastSkills); ;
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleAutoCastSkills.Value))
                {
                    _isAutoCastSkillsEnabled.Value = !_isAutoCastSkillsEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isAutoCastSkillsEnabled.Value ? "enabled" : "disabled")} auto-cast skills",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdAddAutoCastSkills.Value) || command.Name.Equals(_cmdRemoveAutoCastSkills.Value))
                {
                    if (command.Args.Count < 1 || string.IsNullOrWhiteSpace(string.Join(" ", command.Args)))
                    {
                        CommandHandler.DisplayError("You must specify a valid value");

                        return;
                    }

                    var searchParam = string.Join(" ", command.Args);
                    var foundSkills = Game.Instance.Skills
                        .Distinct()
                        .Where(x =>
                            x.Guid.ToString() == searchParam ||
                            x.SkillName.ToLowerInvariant().Contains(searchParam.ToLowerInvariant()) ||
                            OptionsManager.Localize(x.SkillName).ToLowerInvariant().Contains(searchParam.ToLowerInvariant()))
                        .ToList();

                    if (!foundSkills.Any())
                    {
                        CommandHandler.DisplayError("Skill not found");

                        return;
                    }

                    if (foundSkills.Count > 1)
                    {
                        CommandHandler.DisplayError($"Multiple skills found for term '{searchParam}'. Please use a more precise search term, or use a GUID.");

                        return;
                    }

                    var currentAutoCastSkills = _autoCastSkills.Value.Split(',').ToList();
                    var skillGuid = foundSkills[0].Guid.ToString();

                    if (command.Name.Equals(_cmdAddAutoCastSkills.Value))
                    {
                        if (currentAutoCastSkills.Contains(skillGuid))
                        {
                            CommandHandler.DisplayError("This skill already exist in the auto-cast list");

                            return;
                        }

                        currentAutoCastSkills.Add(skillGuid);
                    }
                    else
                    {
                        if (!currentAutoCastSkills.Contains(skillGuid))
                        {
                            CommandHandler.DisplayError("This skill is not in the auto-cast list");

                            return;
                        }

                        currentAutoCastSkills.Remove(skillGuid);
                    }

                    _autoCastSkills.Value = string.Join(",", currentAutoCastSkills);

                    _autoCastListCache = currentAutoCastSkills;

                    CommandHandler.DisplayMessage(
                        command.Name.Equals(_cmdAddAutoCastSkills.Value)
                            ? $"Successfully added skill {OptionsManager.Localize(foundSkills[0].SkillName)} to the auto-cast list"
                            : $"Successfully removed skill {OptionsManager.Localize(foundSkills[0].SkillName)} from the auto-cast list",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdClearAutoCastSkills.Value))
                {
                    _autoCastSkills.Value = string.Empty;

                    _autoCastListCache = new List<string>();

                    CommandHandler.DisplayMessage("Successfully cleared the auto-cast list", PluginInfo.PLUGIN_NAME);
                }
            };
        }

        [HarmonyPatch(typeof(GameLogic), "Update")]
        public class GameLogicUpdateSkillsPatch
        {
            [UsedImplicitly]
            private static void Postfix(GameLogic __instance)
            {
                if (!_isAutoCastSkillsEnabled.Value ||
                    GUIManager.instance.CurrentGuiState != GUIState.InBattle ||
                    !NetworkingManager.Instance.NetworkManager.Root.IsPlayerTurnAndReady ||
                    _autoCastSkillsRanOnce)
                {
                    return;
                }

                _autoCastListCache ??= _autoCastSkills.Value.Split(',').ToList();

                foreach (var character in NetworkingManager.Instance.MyPartyCharacters.Where(x => x.IsMyTurn && !x.IsDead))
                {
                    var skillsToCast = character.Actions
                        .Where(x => _autoCastListCache.Contains(x.SkillInfo.Guid.ToString()))
                        .Select(x => x.ActionInfo)
                        .ToList();

                    foreach (var actionInfo in skillsToCast)
                    {
                        var canCastInfo = character.PlayerMovement.CanCast(new StructList<HexCell>
                        {
                            null
                        }, actionInfo);

                        if (!canCastInfo.CanCast)
                        {
                            continue;
                        }

                        __instance.StartCoroutine(Coroutines.QueueCast(character, character, actionInfo));
                    }
                }

                _autoCastSkillsRanOnce = true;
            }
        }

        [HarmonyPatch(typeof(GameLogic), "StartNewTurnSequence")]
        public class GameLogicStartNewTurnSequencePatch
        {
            [UsedImplicitly]
            private static void Postfix()
            {
                _autoCastSkillsRanOnce = false;
            }
        }
    }
}

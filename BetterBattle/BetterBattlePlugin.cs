using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Burst2Flame;
using Burst2Flame.Observable;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace eradev.stolenrealm.BetterBattle
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class BetterBattlePlugin : BaseUnityPlugin
    {
        private const bool IsAutoCastAurasEnabledDefault = true;
        private const string CmdToggleAutoCastAurasDefault = "t_auras";

        private const bool IsAutoCastSkillsEnabledDefault = true;
        private const string CmdToggleAutoCastSkillsDefault = "t_autocastskills";
        private const string CmdAddAutoCastSkillsDefault = "add_acs";
        private const string CmdRemoveAutoCastSkillsDefault = "remove_acs";

        private const bool IsConvertExpGoldEnabledDefault = true;
        private const string CmdToggleConvertExpGoldDefault = "t_expgold";

        private const bool IsRemoveBarrelsEnabledDefault = false;
        private const string CmdToggleRemoveBarrelsDefault = "t_removebarrels";

        private const bool IsDisplayLootInConsoleDisabledDefault = true;
        private const bool IsRemoveDropsLimitEnabledDefault = true;
        private const string CmdToggleRemoveDropsLimitDefault = "t_dropslimit";

        private static ConfigEntry<string> _cmdToggleAutoCastAuras;
        private static ConfigEntry<bool> _isAutoCastAurasEnabled;

        private static ConfigEntry<string> _cmdToggleAutoCastSkills;
        private static ConfigEntry<string> _cmdAddAutoCastSkills;
        private static ConfigEntry<string> _cmdRemoveAutoCastSkills;
        private static ConfigEntry<bool> _isAutoCastSkillsEnabled;
        private static ConfigEntry<string> _autoCastSkills;

        private static ConfigEntry<bool> _isDisplayLootInConsoleEnabled;

        private static ConfigEntry<string> _cmdToggleConvertExpGold;
        private static ConfigEntry<bool> _isConvertExpGoldEnabled;

        private static ConfigEntry<string> _cmdToggleRemoveBarrels;
        private static ConfigEntry<bool> _isRemoveBarrelsEnabled;

        private static ConfigEntry<string> _cmdToggleRemoveDropsLimit;
        private static ConfigEntry<bool> _isRemoveDropsLimitEnabled;

        // ReSharper disable once NotAccessedField.Local
        private static ManualLogSource _log;

        private static DestructibleSpawnInfo[] _defaultDestructibleSpawnInfos;
        private static List<string> _autoCastListCache;

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

            _isAutoCastAurasEnabled = Config.Bind("General", "autocastauras_enabled", IsAutoCastAurasEnabledDefault,
                "Enable auto-cast auras at the start of battles");
            _isAutoCastSkillsEnabled = Config.Bind("General", "autocastskills_enabled", IsAutoCastSkillsEnabledDefault,
                "Enable auto-cast skills whenever available");
            _autoCastSkills = Config.Bind("General", "autocastskills", string.Empty,
                "List of skills to auto-cast (GUID)");
            _isConvertExpGoldEnabled = Config.Bind("General", "convertexpgold_enabled", IsConvertExpGoldEnabledDefault,
                "Enable convert EXP to gold when your character reached max level");
            _isDisplayLootInConsoleEnabled = Config.Bind("General", "displaylootinconsole_enabled", IsDisplayLootInConsoleDisabledDefault,
                "Enable display loot in console");
            _isRemoveBarrelsEnabled = Config.Bind("General", "removebarrels_enabled", IsRemoveBarrelsEnabledDefault,
                "Enable the removal of barrels");
            _isRemoveDropsLimitEnabled = Config.Bind("General", "removedropslimit_enabled", IsRemoveDropsLimitEnabledDefault,
                "Enable the removal of the drops limit");

            _cmdToggleAutoCastAuras =
                Config.Bind("Commands", "autocastauras_toggle", CmdToggleAutoCastAurasDefault, "Toggle auto-cast auras");
            _cmdToggleAutoCastSkills =
                Config.Bind("Commands", "autocastskills_toggle", CmdToggleAutoCastSkillsDefault, "Toggle auto-cast skills");
            _cmdAddAutoCastSkills =
                Config.Bind("Commands", "autocastskills_add", CmdAddAutoCastSkillsDefault, "[Args (string, name or GUID] Add skill to auto-cast list");
            _cmdRemoveAutoCastSkills =
                Config.Bind("Commands", "autocastskills_remove", CmdRemoveAutoCastSkillsDefault, "[Args (string, name or GUID)] Remove skill from auto-cast list");
            _cmdToggleConvertExpGold = Config.Bind("Commands", "convertexpgold_toggle", CmdToggleConvertExpGoldDefault,
                "Toggle convert EXP to gold when your character reached max level");
            _cmdToggleRemoveBarrels = Config.Bind("Commands", "removebarrels_toggle", CmdToggleRemoveBarrelsDefault,
                "Toggle the removal of barrels");
            _cmdToggleRemoveDropsLimit = Config.Bind("Commands", "removedropslimit_toggle", CmdToggleRemoveDropsLimitDefault,
                "Toggle the removal of drops limit");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleAutoCastAuras);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleAutoCastSkills);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdAddAutoCastSkills);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdRemoveAutoCastSkills);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleConvertExpGold);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleRemoveBarrels);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleRemoveDropsLimit);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleAutoCastAuras.Value))
                {
                    _isAutoCastAurasEnabled.Value = !_isAutoCastAurasEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isAutoCastAurasEnabled.Value ? "enabled" : "disabled")} auto-cast auras",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleAutoCastSkills.Value))
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
                else if (command.Name.Equals(_cmdToggleConvertExpGold.Value))
                {
                    _isConvertExpGoldEnabled.Value = !_isConvertExpGoldEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isConvertExpGoldEnabled.Value ? "enabled" : "disabled")} convert EXP to gold",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleRemoveDropsLimit.Value))
                {
                    _isRemoveDropsLimitEnabled.Value = !_isRemoveDropsLimitEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isRemoveDropsLimitEnabled.Value ? "enabled" : "disabled")} the removal of the drops limit",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleRemoveBarrels.Value))
                {
                    _isRemoveBarrelsEnabled.Value = !_isRemoveBarrelsEnabled.Value;

                    ApplyRemoveBarrelChange();

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isRemoveBarrelsEnabled.Value ? "enabled" : "disabled")} the removal of barrels",
                        PluginInfo.PLUGIN_NAME);
                }
            };

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        #region Right-click cast
        [HarmonyPatch(typeof(ActionSlot), "Start")]
        public class ActionSlotStartPatch
        {
            [UsedImplicitly]
            private static void Postfix(ActionSlot __instance)
            {
                __instance.GetComponent<Button>()
                    .AddComponentIfNone<ClickAction>()
                    .OnRightClick = () => TrySelfCast(__instance);
            }

            private static void TrySelfCast(ActionSlot actionSlot)
            {
                var skillBar = Skillbar.Instance;

                if (!skillBar.AllowCasting || actionSlot.disabled || !actionSlot.skillIcon.enabled || actionSlot.cooldownOverlay.enabled)
                {
                    return;
                }

                var selectedCharacter = HexCellManager.instance.MyPlayer;
                var actionInfo = actionSlot.ActionAndSkill.ActionInfo;

                var canCastInfo = selectedCharacter.CanCast(new StructList<HexCell>
                {
                    null
                }, actionInfo);

                if (!canCastInfo.CanCast)
                {
                    return;
                }

                var target = (TargetInfo)actionInfo.Targets[0];
                var rangeText = !string.IsNullOrEmpty(actionInfo.TargetTextOverride)
                    ? OptionsManager.Localize(actionInfo.TargetTextOverride)
                    : target.UseSimpleTargetingRange
                        ? actionInfo.GetSimpleRange(selectedCharacter.Character, target).ToString()
                        : "Special";

                if (rangeText != "0")
                {
                    return;
                }

                selectedCharacter.ExecuteAction(selectedCharacter.Character.Cell, actionInfo);
            }
        }
        #endregion

        #region Auto-cast auras/skills
        [HarmonyPatch(typeof(GameLogic), "StartNewTurnSequence")]
        public class GameLogicStartNewTurnSequenceAurasPatch
        {
            [UsedImplicitly]
            private static void Postfix(GameLogic __instance)
            {
                if (!_isAutoCastAurasEnabled.Value || __instance.currentTeamTurnIndex != 0 || __instance.numPlayerTurnsStarted != 0)
                {
                    return;
                }

                foreach (var character in NetworkingManager.Instance.MyPartyCharacters)
                {
                    var auras = character.Actions
                        .Select(x => x.ActionInfo)
                        .Where(x => x.StatusEffects.Any(y => y.IsAura))
                        .ToList();

                    foreach (var actionInfo in auras)
                    {
                        var canCastInfo = character.PlayerMovement.CanCast(new StructList<HexCell>
                        {
                            null
                        }, actionInfo);

                        if (!canCastInfo.CanCast ||
                            !canCastInfo.NotYetActivated)
                        {
                            continue;
                        }

                        __instance.StartCoroutine(QueueCast(character, actionInfo));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameLogic), "StartNewTurnSequence")]
        public class GameLogicStartNewTurnSequenceSkillsPatch
        {
            [UsedImplicitly]
            private static void Postfix(GameLogic __instance)
            {
                if (!_isAutoCastSkillsEnabled.Value || __instance.currentTeamTurnIndex != 1)
                {
                    return;
                }

                _autoCastListCache ??= _autoCastSkills.Value.Split(',').ToList();

                foreach (var character in NetworkingManager.Instance.MyPartyCharacters)
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

                        __instance.StartCoroutine(QueueCast(character, actionInfo));
                    }
                }
            }
        }

        private static IEnumerator QueueCast(Character character, ActionInfo actionInfo)
        {
            while (character.Acting)
            {
                yield return new WaitForEndOfFrame();
            }

            character.PlayerMovement.ExecuteAction(character.Cell, actionInfo);
        }
        #endregion

        #region Convert EXP to gold
        [HarmonyPatch(typeof(Character), "GiveExperience")]
        public class CharacterGiveExperiencePatch
        {
            [UsedImplicitly]
            private static bool Prefix(Character __instance, float expValue, bool showMessage)
            {
                if (!_isConvertExpGoldEnabled.Value || __instance.Level < __instance.MaxLevel)
                {
                    return true;
                }

                __instance.GiveGold(expValue, 1f, showMessage);

                return false;
            }
        }
        #endregion

        #region Remove barrels
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
        #endregion

        #region Remove drops limit
        [HarmonyPatch(typeof(GameLogic), "GenerateLoot")]
        public class GameLogicGenerateLootPatch
        {
            [HarmonyTranspiler]
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> RemoveDropsLimit(IEnumerable<CodeInstruction> instructions)
            {
                var codeInstructionList = new List<CodeInstruction>(instructions);

                if (_isRemoveDropsLimitEnabled.Value)
                {
                    var index = codeInstructionList.FindIndex(codeInstruction => codeInstruction.opcode == OpCodes.Ldc_I4_7);
                    codeInstructionList[index] = new CodeInstruction(OpCodes.Ldc_I4, int.MaxValue);
                }

                foreach (var codeInstruction in codeInstructionList)
                {
                    yield return codeInstruction;
                }
            }

            [UsedImplicitly]
            private static void Postfix(List<Item> __result)
            {
                if (_isDisplayLootInConsoleEnabled.Value)
                {
                    return;
                }

                var itemCount = __result.Sum(x => x.numStacks);

                _log.LogInfo($"Looted {itemCount} item{(itemCount > 0 ? "s" : "")}:");

                foreach (var item in __result)
                {
                    _log.LogInfo($"  {Regex.Replace(OptionsManager.Localize(item.ItemName), "<.*?>", string.Empty)}{(item.numStacks > 1 ? $" ({item.numStacks})" : "")}");
                }
            }
        }
        #endregion
    }
}

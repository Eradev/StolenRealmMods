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
        private const bool IsAutoCastAurasDisabledDefault = false;
        private const string CmdToggleAutoCastAurasDefault = "t_auras";

        private const bool IsConvertExpGoldDisabledDefault = false;
        private const string CmdToggleConvertExpGoldDefault = "t_expgold";

        private const bool IsDisplayLootInConsoleDisabledDefault = true;

        private const bool IsRemoveDropsLimitDisabledDefault = false;
        private const string CmdToggleRemoveDropsLimitDefault = "t_dropslimit";

        private static ConfigEntry<string> _cmdToggleAutoCastAuras;
        private static ConfigEntry<bool> _isAutoCastAurasDisabled;

        private static ConfigEntry<bool> _isDisplayLootInConsoleDisabled;

        private static ConfigEntry<string> _cmdToggleConvertExpGold;
        private static ConfigEntry<bool> _isConvertExpGoldDisabled;

        private static ConfigEntry<string> _cmdToggleRemoveDropsLimit;
        private static ConfigEntry<bool> _isRemoveDropsLimitDisabled;

        // ReSharper disable once NotAccessedField.Local
        private static ManualLogSource _log;

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

            _isAutoCastAurasDisabled = Config.Bind("General", "autocastauras_disabled", IsAutoCastAurasDisabledDefault,
                "Disable auto-cast auras at the start of battles");
            _isConvertExpGoldDisabled = Config.Bind("General", "convertexpgold_disabled", IsConvertExpGoldDisabledDefault,
                "Disable convert EXP to gold when your character reached max level");
            _isDisplayLootInConsoleDisabled = Config.Bind("General", "displaylootinconsole_disabled", IsDisplayLootInConsoleDisabledDefault,
                "Disable display loot in console");
            _isRemoveDropsLimitDisabled = Config.Bind("General", "removedropslimit_disabled", IsRemoveDropsLimitDisabledDefault,
                "Disable remove drops limit");

            _cmdToggleAutoCastAuras =
                Config.Bind("Commands", "autocastauras_toggle", CmdToggleAutoCastAurasDefault, "Toggle auto-cast auras");
            _cmdToggleConvertExpGold = Config.Bind("Commands", "convertexpgold_toggle", CmdToggleConvertExpGoldDefault,
                "Toggle convert EXP to gold when your character reached max level");
            _cmdToggleRemoveDropsLimit = Config.Bind("Commands", "removedropslimit_toggle", CmdToggleRemoveDropsLimitDefault,
                "Toggle remove drops limit");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleAutoCastAuras);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleConvertExpGold);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleRemoveDropsLimit);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleAutoCastAuras.Value))
                {
                    _isAutoCastAurasDisabled.Value = !_isAutoCastAurasDisabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isAutoCastAurasDisabled.Value ? "disabled" : "enabled")} auto-cast auras",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleConvertExpGold.Value))
                {
                    _isConvertExpGoldDisabled.Value = !_isConvertExpGoldDisabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isConvertExpGoldDisabled.Value ? "disabled" : "enabled")} convert EXP to gold",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleRemoveDropsLimit.Value))
                {
                    _isRemoveDropsLimitDisabled.Value = !_isRemoveDropsLimitDisabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isRemoveDropsLimitDisabled.Value ? "disabled" : "enabled")} remove drops limit",
                        PluginInfo.PLUGIN_NAME);
                }
            };

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

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

        [HarmonyPatch(typeof(GameLogic), "StartNewTurnSequence")]
        public class GameLogicStartNewTurnSequencePatch
        {
            [UsedImplicitly]
            private static void Postfix(GameLogic __instance)
            {
                if (_isAutoCastAurasDisabled.Value || __instance.currentTeamTurnIndex != 0 || __instance.numPlayerTurnsStarted != 0)
                {
                    return;
                }

                foreach (var character in NetworkingManager.Instance.MyPartyCharacters)
                {
                    foreach (var actionInfo in character.Actions.Select(x => x.ActionInfo).ToList())
                    {
                        var canCastInfo = character.PlayerMovement.CanCast(new StructList<HexCell>
                        {
                            null
                        }, actionInfo);

                        var statusEffect = actionInfo.StatusEffects.FirstOrDefault();

                        var target = (TargetInfo)actionInfo.Targets[0];
                        var rangeText = !string.IsNullOrEmpty(actionInfo.TargetTextOverride)
                            ? OptionsManager.Localize(actionInfo.TargetTextOverride)
                            : target.UseSimpleTargetingRange
                                ? actionInfo.GetSimpleRange(character, target).ToString()
                                : "Special";

                        if (!canCastInfo.CanCast ||
                            !canCastInfo.NotYetActivated ||
                            statusEffect == null ||
                            !statusEffect.Infinite ||
                            rangeText != "0")
                        {
                            continue;
                        }

                        __instance.StartCoroutine(QueueCast(character, actionInfo));
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
        }

        [HarmonyPatch(typeof(Character), "GiveExperience")]
        public class CharacterGiveExperiencePatch
        {
            [UsedImplicitly]
            private static bool Prefix(Character __instance, float expValue, bool showMessage)
            {
                if (_isConvertExpGoldDisabled.Value || __instance.Level < __instance.MaxLevel)
                {
                    return true;
                }

                __instance.GiveGold(expValue, 1f, showMessage);

                return false;
            }
        }

        [HarmonyPatch(typeof(GameLogic), "GenerateLoot")]
        public class GameLogicGenerateLootPatch
        {
            [HarmonyTranspiler]
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> RemoveDropsLimit(IEnumerable<CodeInstruction> instructions)
            {
                var codeInstructionList = new List<CodeInstruction>(instructions);

                if (!_isRemoveDropsLimitDisabled.Value)
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
                if (_isDisplayLootInConsoleDisabled.Value)
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
    }
}

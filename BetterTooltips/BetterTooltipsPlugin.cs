using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Burst2Flame;
using Burst2Flame.Observable;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace eradev.stolenrealm.BetterTooltips
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class BetterTooltipsPlugin : BaseUnityPlugin
    {
        // ReSharper disable once NotAccessedField.Local
        private static ManualLogSource _log;

        private static readonly Dictionary<ActionStatusInfo, bool> IsGroundEffectCache = new ();

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(HexCellManager), "CurrentState", MethodType.Setter)]
        public class HexCellManagerCurrentStatePatch
        {
            [UsedImplicitly]
            private static void Postfix(HexCellManager __instance)
            {
                if (GUIManager.instance.CurrentGuiState != GUIState.InBattle ||
                    __instance.CurrentState != PlayerState.Movement)
                {
                    return;
                }

                RefreshActionSlotsInSkillBar();
            }
        }

        [HarmonyPatch(typeof(ActionSlot), "SelectActionSlot")]
        public class ActionSlotSelectActionSlotPatch
        {
            [UsedImplicitly]
            private static void Prefix(ActionSlot __instance)
            {
                // Refresh the status of the ActionSlot so it can be selected if disabled previously by this mod
                __instance.RefreshActionSlot(__instance.ActionAndSkill);
            }

            [UsedImplicitly]
            private static void Postfix()
            {
                var hexCellManager = HexCellManager.instance;

                if (hexCellManager == null ||
                    GUIManager.instance.CurrentGuiState != GUIState.InBattle ||
                    hexCellManager.CurrentState != PlayerState.Action ||
                    hexCellManager.MyPlayer.CurrentAction == null)
                {
                    return;
                }

                RefreshActionSlotsInSkillBar();

                var actionSlots = Skillbar.Instance.transform.GetComponentsInChildren<ActionSlot>();

                if (actionSlots.All(x => x.ActionAndSkill.ActionInfo != hexCellManager.MyPlayer.CurrentAction))
                {
                    return;
                }

                var otherSlots = actionSlots.Where(x => x.ActionAndSkill.ActionInfo != hexCellManager.MyPlayer.CurrentAction).ToList();

                foreach (var actionSlot in otherSlots)
                {
                    actionSlot.cooldownOverlay.enabled = true;
                }
            }
        }

        [HarmonyPatch(typeof(HexCellManager), "CurrentlyHoveringHexCell", MethodType.Setter)]
        public class HexCellManagerCurrentlyHoveringHexCellPatch
        {
            [UsedImplicitly]
            private static void Postfix(HexCellManager __instance)
            {
                if (GUIManager.instance.CurrentGuiState != GUIState.InBattle ||
                    __instance.CurrentlyHoveringHexCell == null ||
                    CursorManager.instance.HoveringUseableObject)
                {
                    return;
                }

                var selectedCharacter = GameLogic.instance.CurrentlySelectedCharacter;

                if (__instance.CurrentlyHoveringHexCell.HasEnemy(selectedCharacter))
                {
                    // This is necessary or the tooltip will flicker. Cannot be put earlier or it will hide other tooltips.
                    GUIManager.instance.tooltip.HideTooltip();

                    switch (__instance.CurrentState)
                    {
                        case PlayerState.Movement:
                            if (AccessTools.FieldRefAccess<CursorType>(typeof(CursorManager), "currentCursorType")
                                    .Invoke(CursorManager.instance) == CursorType.Attack)
                            {
                                ShowSelectedActionTooltip(selectedCharacter.BasicAttacks[0]);
                            }

                            break;

                        case PlayerState.Action:
                            if (IsCurrentHoveringCellValidCellForAction())
                            {
                                ShowSelectedActionTooltip(__instance.MyPlayer.CurrentAction);
                            }

                            break;
                    }
                }
                else if (__instance.CurrentState == PlayerState.Action)
                {
                    // This is necessary or the tooltip will flicker. Cannot be put earlier or it will hide other tooltips.
                    GUIManager.instance.tooltip.HideTooltip();

                    if (IsCurrentHoveringCellValidCellForAction())
                    {
                        ShowSelectedActionTooltip(__instance.MyPlayer.CurrentAction);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Tooltip), "ShowActionStatusTooltip",
            typeof(ActionStatusInfo),
            typeof(Character),
            typeof(Character),
            typeof(Item),
            typeof(PersistentDurationType),
            typeof(string),
            typeof(float),
            typeof(bool),
            typeof(string))]
        public class TooltipShowActionStatusTooltipPatch
        {
            [UsedImplicitly]
            private static void Prefix(
                ref ActionStatusInfo actionStatusInfo,
                PersistentDurationType persistentDurationType,
                Character source,
                Character target,
                float level)
            {
                var actionStatusInfoClone = actionStatusInfo.Clone();

                bool isGroundEffect;
                if (IsGroundEffectCache.ContainsKey(actionStatusInfo))
                {
                    isGroundEffect = IsGroundEffectCache[actionStatusInfo];
                }
                else
                {
                    var info = actionStatusInfo;

                    isGroundEffect = Game.Instance.GroundEffects.Any(x => x.ActionStatuses.Contains(info)) ||
                                     GameLogic.instance.GroundEffects.Any(x => x.ActionStatuses.Contains(info));

                    IsGroundEffectCache.Add(actionStatusInfo, isGroundEffect);
                }

                if (actionStatusInfo.CannotBeDispelled ||
                    isGroundEffect ||
                    actionStatusInfo.IsAura ||
                    persistentDurationType is PersistentDurationType.Quest)
                {
                    actionStatusInfoClone.Description += "<br><br><color=yellow>Cannot be dispelled.</color>";
                }

                var extraLineBreakAdded = false;

                if (actionStatusInfo.CanStack)
                {
                    int stackCount;

                    if (actionStatusInfo.StackIgnoreSource)
                    {
                        var actionStatusGuid = actionStatusInfo.Guid;

                        stackCount = target == null
                            ? 0
                            : target.ActionStatuses
                                .Where(x => x.ActionStatusInfo.Guid == actionStatusGuid)
                                .Select(x => x.TotalStacks)
                                .Sum();
                    }
                    else
                    {
                        stackCount = actionStatusInfo.GetNumStacksFromSource(source, target);
                    }


                    if (stackCount > 1)
                    {
                        extraLineBreakAdded = true;

                        actionStatusInfoClone.Description += $"<br><br>{OptionsManager.Localize("Stack")}: {stackCount}";
                    }
                }

                if (!actionStatusInfoClone.Infinite && source != null)
                {
                    var duration = Game.Eval<float>(actionStatusInfo.Duration, new GameFunctionParameters
                    {
                        Source = source
                    });

                    if (duration > 0f)
                    {
                        if (!extraLineBreakAdded)
                        {
                            actionStatusInfoClone.Description += "<br>";
                        }

                        actionStatusInfoClone.Description += $"<br>{Tooltip.GetLocalizedDurationText(false, duration)}";
                    }
                }

                // Display for Fortunes
                if (persistentDurationType == PersistentDurationType.Permanent)
                {
                    var fortuneGuid = actionStatusInfo.Guid.ToString();

                    actionStatusInfoClone.Description += "<br>";

                    foreach (var character in NetworkingManager.Instance.MyPartyCharacters)
                    {
                        var characterFortune = character.FortuneData.SingleOrDefault(x => x.Guid == fortuneGuid);

                        if (characterFortune == null)
                        {
                            actionStatusInfoClone.Description += $"<br><color=red>{character.CharacterName}</color>";
                        }
                        else
                        {
                            var colorString = characterFortune.Level < level ? "yellow" : "green";
                            var levelString = OptionsManager.Localize("Level [level value]")
                                .Replace("[level value]", characterFortune.Level.ToString(CultureInfo.InvariantCulture));

                            actionStatusInfoClone.Description += $"<br><color={colorString}>{character.CharacterName} ({levelString})</color>";
                        }
                    }
                }

                actionStatusInfo = actionStatusInfoClone;
            }
        }

        [HarmonyPatch(typeof(Tooltip), "ShowGroundEffectTooltip")]
        public class TooltipShowGroundEffectTooltipPatch
        {
            [UsedImplicitly]
            private static bool Prefix(
                ref Tooltip __instance,
                // ReSharper disable once InconsistentNaming
                ref bool ___hideOnNotHoveringGO,
                List<GroundEffectNetworkInfo> groundEffectNetworkInfos)
            {
                if (OptionsManager.instance.GroundEffectTooltipsDisabled)
                {
                    return true;
                }

                __instance.ShowingGroundEffect = true;

                var dictionary = new Dictionary<string, int[]>();

                foreach (var groundEffectNetworkInfo in groundEffectNetworkInfos)
                {
                    var key = "";
                    var turnsToExpire = 0;

                    if (groundEffectNetworkInfo.GroundEffectInfoIndex > -1)
                    {
                        var groundEffectInfo = groundEffectNetworkInfo.GroundEffectInfo;

                        var title = groundEffectNetworkInfo.ActionInfo == null || string.IsNullOrEmpty(groundEffectNetworkInfo.ActionInfo.GroundEffectTitleOverride)
                            ? groundEffectInfo.Name
                            : groundEffectNetworkInfo.ActionInfo.GroundEffectTitleOverride;
                        var descriptionExpression = groundEffectInfo.ActionStatuses == null || groundEffectInfo.ActionStatuses.Length == 0
                            ? __instance.ApplyDescriptionExpressions(OptionsManager.Localize(groundEffectInfo.Description),
                                groundEffectInfo.DescriptionExpressions, NetworkingManager.Instance.NetworkManager.Root.WorldCharacter,
                                null, __instance.Description.fontSize)
                            : __instance.ApplyDescriptionExpressions(OptionsManager.Localize(groundEffectInfo.ActionStatuses[0].Description),
                                groundEffectInfo.ActionStatuses[0].DescriptionExpressions,
                                NetworkingManager.Instance.NetworkManager.Root.WorldCharacter, null, __instance.Description.fontSize);

                        turnsToExpire = groundEffectNetworkInfo.GroundEffectInfo.CanExpire
                            ? Game.Instance.GroundEffects[groundEffectNetworkInfo.GroundEffectInfoIndex].TurnsToExpire - GameLogic.instance.turnNumber
                            : 0;

                        key =  $"<color=#CBB396><size=17>{OptionsManager.Localize(title)}[Stacks]</size></color>\n{descriptionExpression}";
                    }

                    if (groundEffectNetworkInfo.ActionInfoIndex > -1)
                    {
                        var actionInfo = groundEffectNetworkInfo.ActionInfo!;
                        var title = string.IsNullOrWhiteSpace(actionInfo.GroundEffectTitleOverride)
                            ? actionInfo.ActionName
                            : actionInfo.GroundEffectTitleOverride;

                        // Only the host has access to this list.
                        var foundGroundEffect = GameLogic.instance.GroundEffects.FirstOrDefault(x => x.EffectID == groundEffectNetworkInfo.ID);

                        turnsToExpire = actionInfo.GroundIsInfinite || foundGroundEffect == null
                            ? 0
                            : (int)Math.Ceiling((decimal)(foundGroundEffect.ExpireTurnNumber - GameLogic.instance.turnNumber) / GameLogic.instance.NumberOfTeams);

                        key += $"<color=#CBB396><size=17>{OptionsManager.Localize(title)}[Stacks]</size></color>";

                        if (!string.IsNullOrEmpty(actionInfo.GroundEffectDescription))
                        {
                            var groundEffectDescription = "";

                            if (actionInfo.GroundEffectDescriptionActionRef == null)
                            {
                                if (actionInfo.GroundEffectDescriptionActionStatusRef == null)
                                {
                                    groundEffectDescription = OptionsManager.Localize(actionInfo.GroundEffectDescription);
                                }
                                else
                                {
                                    groundEffectDescription = __instance.GetDamageString(
                                        OptionsManager.Localize(actionInfo.GroundEffectDescription),
                                        actionInfo.GroundEffectDescriptionActionStatusRef,
                                        groundEffectNetworkInfo.Character, 
                                        null);
                                }
                            }
                            else
                            {
                                groundEffectDescription = __instance.GetDamageString(
                                    OptionsManager.Localize(actionInfo.GroundEffectDescription),
                                    actionInfo.GroundEffectDescriptionActionRef,
                                    groundEffectNetworkInfo.Character,
                                    null);
                            }

                            if (!string.IsNullOrEmpty(groundEffectDescription))
                            {
                                key += $"\n{groundEffectDescription}";
                            }
                        }
                    }

                    if (groundEffectNetworkInfo.Character != null)
                    {
                        key += (groundEffectNetworkInfo.OverrideTeamIndex
                                ? groundEffectNetworkInfo.OverrideTeamIndexValue
                                : groundEffectNetworkInfo.Character.TeamIndex) switch
                                {
                                    0 => $"\n<color=#{ColorUtility.ToHtmlStringRGB(GUIManager.instance.positiveColor)}><size=12>{OptionsManager.Localize("Friendly")}</size></color>",
                                    1 => $"\n<color=#{ColorUtility.ToHtmlStringRGB(GUIManager.instance.negativeColor)}><size=12>{OptionsManager.Localize("Enemy")}</size></color>",
                                    _ => $"\n<color=#{ColorUtility.ToHtmlStringRGB(GUIManager.instance.neutralColor)}><size=12>{OptionsManager.Localize("Neutral")}</size></color>"
                                };
                    }

                    if (dictionary.ContainsKey(key))
                    {
                        var currentMaxDuration = dictionary[key][1];
                        if (currentMaxDuration < turnsToExpire)
                        {
                            currentMaxDuration = turnsToExpire;
                        }

                        dictionary[key][0] += 1;
                        dictionary[key][1] = currentMaxDuration;
                    }
                    else
                    {
                        dictionary.Add(key, new []{ 1, turnsToExpire });
                    }
                }

                var description = "";
                foreach (var keyValuePair in dictionary)
                {
                    if (!string.IsNullOrEmpty(description))
                    {
                        description += "\n\n";
                    }

                    var stacks = keyValuePair.Value[0];
                    var duration = keyValuePair.Value[1];

                    description += keyValuePair.Key.Replace("[Stacks]", stacks > 1 ? $" x{stacks}" : "");

                    if (duration > 0)
                    {
                        description += $"\n\n{Tooltip.GetLocalizedDurationText(false, duration)}";
                    }
                }

                __instance.ShowTooltip("", "", null, description, null, __instance.defaultTitleColor, false, alpha: 0.9f);

                ___hideOnNotHoveringGO = false;

                return false;
            }
        }

        [HarmonyPatch(typeof(Tooltip), "ShowTooltip")]
        public class TooltipShowTooltipPatch
        {
            [UsedImplicitly]
            private static void Prefix(Tooltip __instance)
            {
                // Resets the Image component that's disabled in ShowSelectedActionTooltip
                __instance.GetComponent<Image>().enabled = true;
            }
        }

        private static void RefreshActionSlotsInSkillBar()
        {
            var actionSlots = Skillbar.Instance.transform.GetComponentsInChildren<ActionSlot>();

            foreach (var actionSlot in actionSlots)
            {
                actionSlot.RefreshActionSlot(actionSlot.ActionAndSkill);
            }
        }

        private static bool IsCurrentHoveringCellValidCellForAction()
        {
            var selectedCharacter = GameLogic.instance.CurrentlySelectedCharacter;
            var hexCellManager = HexCellManager.instance;

            var playerMovement = hexCellManager.MyPlayer;

            var canCast = playerMovement.CanCast(new StructList<HexCell> { hexCellManager.CurrentlyHoveringHexCell }, playerMovement.CurrentAction).CanCast;
            var hasLoS = !hexCellManager.CurrentlyHoveringHexCell.GetLineOfSightHitPoint(selectedCharacter.Cell).HasValue;

            return canCast && hasLoS;
        }

        private static void ShowSelectedActionTooltip(ActionInfo actionInfo)
        {
            var tooltip = GUIManager.instance.tooltip;
            var skill = Game.Instance.GetSkillFromActionInfo(actionInfo);

            tooltip.ShowTooltip(OptionsManager.Localize(skill.SkillName), "", skill.Icon, "", null, Color.white, alpha: 0.7f);
            tooltip.GetComponent<Image>().enabled = false;
            tooltip.TitleSep.SetActive(false);

            AccessTools.FieldRefAccess<bool>(typeof(Tooltip), "hideOnNotHoveringGO").Invoke(tooltip) = true;
        }
    }
}

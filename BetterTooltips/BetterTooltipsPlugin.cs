using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Burst2Flame;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

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
                string header,
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

                /*_log.LogDebug($"Name: {OptionsManager.Localize(actionStatusInfo.Name)}");
                _log.LogDebug($"Description: {OptionsManager.Localize(actionStatusInfo.Description)}");
                _log.LogDebug($"Cannot be dispelled: {actionStatusInfo.CannotBeDispelled}");
                _log.LogDebug($"Persistent duration type: {persistentDurationType}");
                _log.LogDebug($"Status type: {actionStatusInfo.StatusType}");
                _log.LogDebug($"IsGroundEffect: {isGroundEffect}");
                _log.LogDebug($"IsAura: {actionStatusInfo.IsAura}");
                _log.LogDebug($"header: {header}");*/

                if (actionStatusInfo.CannotBeDispelled ||
                    isGroundEffect ||
                    actionStatusInfo.IsAura ||
                    persistentDurationType is PersistentDurationType.Quest)
                {
                    actionStatusInfoClone.Description += "<br><br><color=yellow>Cannot be dispelled.</color>";
                }

                var extraLineBreakAdded = false;

                //_log.LogDebug($"Can stack: {actionStatusInfo.CanStack}");
                if (actionStatusInfo.CanStack)
                {

                    /*_log.LogDebug($"Source null?: {source == null}");
                    _log.LogDebug($"Target null?: {target == null}");
                    _log.LogDebug($"Stack count: {stackCount}");
                    _log.LogDebug($"Stack ignoreSource: {actionStatusInfo.StackIgnoreSource}");*/

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

                if (persistentDurationType == PersistentDurationType.Permanent)
                {
                    var fortuneGuid = actionStatusInfo.Guid.ToString();

                    actionStatusInfoClone.Description += "<br>";

                    //_log.LogDebug("Trying to get my party characters...");
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
                        var title = string.IsNullOrWhiteSpace(groundEffectNetworkInfo.Title)
                            ? OptionsManager.Localize(groundEffectInfo.Name)
                            : OptionsManager.Localize(groundEffectNetworkInfo.Title);
                        var descriptionExpression = groundEffectInfo.ActionStatuses == null || groundEffectInfo.ActionStatuses.Length == 0
                            ? GUIManager.instance.tooltip.ApplyDescriptionExpressions(OptionsManager.Localize(groundEffectInfo.Description),
                                groundEffectInfo.DescriptionExpressions, NetworkingManager.Instance.NetworkManager.Root.WorldCharacter,
                                null, __instance.Description.fontSize)
                            : GUIManager.instance.tooltip.ApplyDescriptionExpressions(OptionsManager.Localize(groundEffectInfo.ActionStatuses[0].Description),
                                groundEffectInfo.ActionStatuses[0].DescriptionExpressions,
                                NetworkingManager.Instance.NetworkManager.Root.WorldCharacter, null, __instance.Description.fontSize);

                        turnsToExpire = groundEffectNetworkInfo.GroundEffectInfo.CanExpire
                            ? Game.Instance.GroundEffects[groundEffectNetworkInfo.GroundEffectInfoIndex].TurnsToExpire - GameLogic.instance.turnNumber
                            : 0;

                        key =  $"<color=#CBB396><size=17>{title}[Stacks]</size></color>\n{descriptionExpression}";
                    }

                    if (groundEffectNetworkInfo.ActionInfoIndex > -1)
                    {
                        var actionInfo = groundEffectNetworkInfo.ActionInfo;
                        var title = string.IsNullOrWhiteSpace(groundEffectNetworkInfo.Title)
                            ? OptionsManager.Localize(actionInfo.ActionName)
                            : OptionsManager.Localize(groundEffectNetworkInfo.Title);

                        // Only the host has access to this list.
                        var foundGroundEffect = GameLogic.instance.GroundEffects.FirstOrDefault(x => x.EffectID == groundEffectNetworkInfo.ID);

                        turnsToExpire = actionInfo.GroundIsInfinite || foundGroundEffect == null
                            ? 0
                            : (int)Math.Ceiling((decimal)(foundGroundEffect.ExpireTurnNumber - GameLogic.instance.turnNumber) / GameLogic.instance.NumberOfTeams);

                        key += $"<color=#CBB396><size=17>{title}[Stacks]</size></color>\n{OptionsManager.Localize(groundEffectNetworkInfo.Description)}";
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
    }
}

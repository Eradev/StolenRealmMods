using System.Collections;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Burst2Flame;
using Burst2Flame.Observable;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace eradev.stolenrealm.BetterBattle
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class BetterBattlePlugin : BaseUnityPlugin
    {
        // ReSharper disable once NotAccessedField.Local
        private static ManualLogSource _log;

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

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
                if (__instance.currentTeamTurnIndex != 0 || __instance.numPlayerTurnsStarted != 0)
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

                        if (!canCastInfo.CanCast || !canCastInfo.NotYetActivated || statusEffect == null || !statusEffect.Infinite || rangeText != "0")
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
    }
}

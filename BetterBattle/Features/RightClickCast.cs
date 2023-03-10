using Burst2Flame;
using Burst2Flame.Observable;
using eradev.stolenrealm.BetterBattle.Shared;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.UI;

namespace eradev.stolenrealm.BetterBattle.Features
{
    [UsedImplicitly]
    public class RightClickCast
    {
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
    }
}

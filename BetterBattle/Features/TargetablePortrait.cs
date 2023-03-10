using Burst2Flame;
using Burst2Flame.Observable;
using eradev.stolenrealm.BetterBattle.Shared;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace eradev.stolenrealm.BetterBattle.Features
{
    [UsedImplicitly]
    public class TargetablePortrait
    {
        [HarmonyPatch(typeof(Portrait), "SelectCharacter")]
        public class PortraitSelectCharacterPatch
        {
            [UsedImplicitly]
            private static bool Prefix(Portrait __instance)
            {
                var hexCellManager = HexCellManager.instance;

                if (!IsCurrentActionValidHoverPortrait())
                {
                    return true;
                }

                GameLogic.instance.StartCoroutine(Coroutines.QueueCast(GameLogic.instance.CurrentlySelectedCharacter, __instance.Character, hexCellManager.MyPlayer.CurrentAction));

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

        [HarmonyPatch(typeof(Portrait), "Update")]
        public class PortraitUpdatePatch
        {
            [UsedImplicitly]
            private static void Postfix()
            {
                if (!IsCurrentActionValidHoverPortrait() ||
                    GUIManager.instance.tooltip.gameObject.activeInHierarchy)
                {
                    return;
                }

                CursorManager.instance.SetCursor(CursorType.Target);
                GUIManager.instance.tooltip.HideTooltip();

                ShowSelectedActionTooltip(HexCellManager.instance.MyPlayer.CurrentAction);
            }
        }

        [HarmonyPatch(typeof(Portrait), "OnExitHover")]
        public class PortraitOnExitHoverPatch
        {
            [UsedImplicitly]
            private static void Postfix()
            {
                GUIManager.instance.tooltip.HideTooltip();
            }
        }

        private static bool IsCharacterCellValidCellForAction(Character character)
        {
            var selectedCharacter = GameLogic.instance.CurrentlySelectedCharacter;

            var canCast = selectedCharacter.PlayerMovement.CanCast(new StructList<HexCell> { character.Cell }, selectedCharacter.PlayerMovement.CurrentAction).CanCast;
            var hasLoS = !character.Cell.GetLineOfSightHitPoint(selectedCharacter.Cell).HasValue;

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

        private static bool IsCurrentActionValidHoverPortrait()
        {
            var hexCellManager = HexCellManager.instance;
            var character = PortraitManager.Instance.HoverPortraitCharacter;

            if (character == null ||
                GUIManager.instance.CurrentGuiState != GUIState.InBattle ||
                hexCellManager.CurrentState != PlayerState.Action ||
                hexCellManager.MyPlayer.CurrentAction == null ||
                !IsCharacterCellValidCellForAction(character))
            {
                return false;
            }

            return true;
        }
    }
}

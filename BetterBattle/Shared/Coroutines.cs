using System.Collections;
using Burst2Flame;
using UnityEngine;

namespace eradev.stolenrealm.BetterBattle.Shared
{
    public static class Coroutines
    {
        public static IEnumerator QueueCast(Character source, Character target, ActionInfo actionInfo)
        {
            while (source.Acting)
            {
                yield return new WaitForEndOfFrame();
            }

            source.PlayerMovement.ExecuteAction(target.Cell, actionInfo);

            GUIManager.instance.tooltip.HideTooltip();
        }
    }
}

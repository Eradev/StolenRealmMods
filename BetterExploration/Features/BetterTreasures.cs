using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterExploration.Features
{
    [UsedImplicitly]
    public class BetterTreasures
    {
        [HarmonyPatch(typeof(EventOption), "InitActions")]
        public class EvenOptionInitActionsPatch
        {
            [UsedImplicitly]
            private static void Prefix(EventOption __instance)
            {
                if (!Options.IsBetterTreasuresEnabled.Value)
                {
                    return;
                }

                foreach (var eventAction in __instance.eventActions)
                {
                    foreach (var eventActionEffect in eventAction.EventActionEffects.Where(x => x.giveItems && x.itemPoolType != UniversalLootTableType.None))
                    {
                        eventActionEffect.lootRestrictions.limitItemRarities = true;
                        eventActionEffect.lootRestrictions.possibleRarities = new List<ItemQuality>
                        {
                            ItemQuality.Legendary,
                            ItemQuality.Mythic
                        };
                    }
                }
            }
        }
    }
}

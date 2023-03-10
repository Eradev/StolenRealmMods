using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterExploration.Features
{
    [UsedImplicitly]
    public class UnlockFortunesForParty
    {
        [HarmonyPatch(typeof(Character), "AddFortune")]
        public class CharacterAddFortunePatch
        {
            [UsedImplicitly]
            private static void Postfix(string guid, float level)
            {
                if (!Options.IsUnlockFortunePartyEnabled.Value)
                {
                    return;
                }

                foreach (var character in NetworkingManager.Instance.MyPartyCharacters)
                {
                    var hasChanges = false;

                    var characterFortune = character.FortuneData.SingleOrDefault(x => x.Guid == guid);

                    if (characterFortune == null)
                    {
                        character.FortuneData.Add(new FortuneSaveData
                        {
                            Guid = guid,
                            Level = level,
                            EquippedSlotIndex = -1,
                            IsNew = true
                        });

                        hasChanges = true;
                    }
                    else if (characterFortune.Level < level)
                    {
                        characterFortune.Level = level;

                        hasChanges = true;
                    }

                    if (!hasChanges)
                    {
                        continue;
                    }

                    /*character.FortuneData = character.FortuneData
                        .OrderBy(x => x.GetLocalizedName())
                        .ToList();*/

                    character.Save(true);
                }
            }
        }
    }
}

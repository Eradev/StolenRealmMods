using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Burst2Flame;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.UnlockFortunes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class UnlockFortunesPlugin : BaseUnityPlugin
    {
        private static ManualLogSource _log;

        private void Awake()
        {
            _log = Logger;

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(GameLogic), "LoadCharacters")]
        public class GameLogicLoadCharactersPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref List<Character> __result)
            {
                foreach (var character in __result)
                {
                    UnlockFortunes(character);
                }
            }
        }

        [HarmonyPatch(typeof(Root), "CreateNewHumanCharacter")]
        public class RootCreateNewHumanCharacterPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref Character __result)
            {
                UnlockFortunes(__result);
            }
        }

        private static void UnlockFortunes(Character character)
        {
            var characterName = string.IsNullOrEmpty(character.CharacterName)
                ? PresetManager.Instance.nameField.text // New character
                : character.CharacterName;

            var hasChanges = false;

            foreach (var fortune in Game.Instance.Fortunes.Where(x => !x.Name.Equals("Test Fortune", StringComparison.InvariantCultureIgnoreCase)))
            {
                var characterFortune = character.FortuneData.SingleOrDefault(x => x.Guid == fortune.Guid.ToString());

                if (characterFortune == null)
                {
                    character.FortuneData.Add(new FortuneSaveData
                    {
                        Guid = fortune.Guid.ToString(),
                        Level = 30f,
                        EquippedSlotIndex = -1,
                        IsNew = true
                    });

                    hasChanges = true;

                    _log.LogDebug($"[{characterName}] Unlocked {fortune.Name}");
                }
                else if (characterFortune.Level < 30f)
                {
                    characterFortune.Level = 30f;

                    hasChanges = true;

                    _log.LogDebug($"[{characterName}] Upgraded {fortune.Name} to level 30");
                }
            }

            if (hasChanges)
            {
                character.Save(true);
            }
        }
    }
}

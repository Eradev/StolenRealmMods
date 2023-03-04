using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Burst2Flame;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.UnlockFortunes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class UnlockFortunesPlugin : BaseUnityPlugin
    {
        private const bool IsUnlockAllFortunesMaxedEnabledDefault = false;
        private const bool IsUnlockFortuneAllPartyEnabledDefault = true;

        private static ConfigEntry<bool> _isUnlockAllFortunesMaxedEnabled;
        private static ConfigEntry<bool> _isUnlockFortuneAllPartyEnabled;

        private static ManualLogSource _log;

        private void Awake()
        {
            _log = Logger;

            _isUnlockAllFortunesMaxedEnabled = Config.Bind("General", "unlockallfortunesmaxed_enabled", IsUnlockAllFortunesMaxedEnabledDefault,
                "Enable always unlock all fortunes at maxed level (On game load, and on character creation)");
            _isUnlockFortuneAllPartyEnabled = Config.Bind("General", "unlockfortuneallparty_enabled", IsUnlockFortuneAllPartyEnabledDefault,
                "Enable unlock fortune on all your party when you unlock on one");

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(GameLogic), "LoadCharacters")]
        public class GameLogicLoadCharactersPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref List<Character> __result)
            {
                if (!_isUnlockAllFortunesMaxedEnabled.Value)
                {
                    return;
                }

                foreach (var character in __result)
                {
                    UnlockFortunes(character);
                }
            }
        }

        [HarmonyPatch(typeof(Character), "AddFortune")]
        public class CharacterAddFortunePatch
        {
            [UsedImplicitly]
            private static void Postfix(string guid, float level)
            {
                if (!_isUnlockFortuneAllPartyEnabled.Value)
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

                    if (hasChanges)
                    {
                        character.FortuneData = character.FortuneData
                            .OrderBy(x => x.GetLocalizedName())
                            .ToList();

                        character.Save(true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Root), "CreateNewHumanCharacter")]
        public class RootCreateNewHumanCharacterPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref Character __result)
            {
                if (!_isUnlockAllFortunesMaxedEnabled.Value)
                {
                    return;
                }

                UnlockFortunes(__result);
            }
        }

        private static void UnlockFortunes(Character character, float level = 30f)
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
                        Level = level,
                        EquippedSlotIndex = -1,
                        IsNew = true
                    });

                    hasChanges = true;

                    _log.LogDebug($"[{characterName}] Unlocked {fortune.Name}");
                }
                else if (characterFortune.Level < level)
                {
                    characterFortune.Level = level;

                    hasChanges = true;

                    _log.LogDebug($"[{characterName}] Upgraded {fortune.Name} to level {level}");
                }
            }

            if (hasChanges)
            {
                character.FortuneData = character.FortuneData
                    .OrderBy(x => x.GetLocalizedName())
                    .ToList();

                character.Save(true);
            }
        }
    }
}

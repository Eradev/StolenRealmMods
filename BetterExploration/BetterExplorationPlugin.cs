using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterExploration
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class BetterExplorationPlugin : BaseUnityPlugin
    {
        private const bool IsBetterTreasuresEnabledDefault = false;
        private const string CmdToggleBetterTreasuresDefault = "t_bettertreasures";

        private const bool IsCustomizedMovementSpeedEnabledDefault = true;
        private const string CmdSetCustomizedMovementSpeedDefault = "set_ms";
        private const string CmdToggleCustomizedMovementSpeedDefault = "t_ms";
        private const float SpeedMultiplierDefault = 1.5f;

        private const bool IsMiniGamesAutoCompletionEnabledDefault = true;
        private const string CmdToggleMiniGamesAutoCompletionDefault = "t_gathering";

        private const bool IsUnlockFortunePartyEnabledDefault = true;
        private const string CmdToggleUnlockFortunePartyDefault = "t_fortune4party";

        private static ConfigEntry<string> _cmdToggleBetterTreasures;
        private static ConfigEntry<bool> _isBetterTreasuresEnabled;

        private static ConfigEntry<string> _cmdSetCustomizedMovementSpeed;
        private static ConfigEntry<string> _cmdToggleCustomizedMovementSpeed;
        private static ConfigEntry<bool> _isCustomizedMovementSpeedEnabled;
        private static ConfigEntry<float> _speedMultiplier;

        private static ConfigEntry<string> _cmdToggleMiniGamesAutoCompletion;
        private static ConfigEntry<bool> _isMiniGamesAutoCompletionEnabled;

        private static ConfigEntry<string> _cmdToggleUnlockFortuneParty;
        private static ConfigEntry<bool> _isUnlockFortunePartyEnabled;

        // ReSharper disable once NotAccessedField.Local
        private static ManualLogSource _log;

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

            _isBetterTreasuresEnabled = Config.Bind("General", "bettertreasures_enabled", IsBetterTreasuresEnabledDefault, "Enable better treasures");
            _isCustomizedMovementSpeedEnabled = Config.Bind("General", "cuztomizedmovementspeed_enabled", IsCustomizedMovementSpeedEnabledDefault, "Enable customized movement speed");
            _speedMultiplier = Config.Bind("General", "customizedmovementspeed_mult", SpeedMultiplierDefault, "Speed multiplier (Min 1.0)");
            if (_speedMultiplier.Value < 1.0f)
            {
                _speedMultiplier.Value = 1.0f;
            }
            _isMiniGamesAutoCompletionEnabled = Config.Bind("General", "minigamesautocompletion_enabled", IsMiniGamesAutoCompletionEnabledDefault, "Enable mini-games auto-completion");
            _isUnlockFortunePartyEnabled = Config.Bind("General", "unlockfortuneparty_enabled", IsUnlockFortunePartyEnabledDefault,
                "Enable unlock fortune for your party");

            _cmdToggleBetterTreasures = Config.Bind("Commands", "bettertreasures_toggle", CmdToggleBetterTreasuresDefault, "Toggle better treasures");
            _cmdSetCustomizedMovementSpeed = Config.Bind("Commands", "customizedmovementspeed_set", CmdSetCustomizedMovementSpeedDefault, "[Args (float, min 1.0)] Set the movement speed multiplier");
            _cmdToggleCustomizedMovementSpeed = Config.Bind("Commands", "customizedmovementspeed_toggle", CmdToggleCustomizedMovementSpeedDefault, "Toggle customized movement speed");
            _cmdToggleMiniGamesAutoCompletion = Config.Bind("Commands", "minigamesautocompletion_toggle", CmdToggleMiniGamesAutoCompletionDefault, "Toggle mini-games auto-completion");
            _cmdToggleUnlockFortuneParty = Config.Bind("Commands", "unlockfortuneparty_toggle", CmdToggleUnlockFortunePartyDefault, "Toggle unlock fortune for your party");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleBetterTreasures);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdSetCustomizedMovementSpeed);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleCustomizedMovementSpeed);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleMiniGamesAutoCompletion);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleUnlockFortuneParty);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleBetterTreasures.Value))
                {
                    _isBetterTreasuresEnabled.Value = !_isBetterTreasuresEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isBetterTreasuresEnabled.Value ? "enabled" : "disabled")} better treasures",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleCustomizedMovementSpeed.Value))
                {
                    _isCustomizedMovementSpeedEnabled.Value = !_isCustomizedMovementSpeedEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isCustomizedMovementSpeedEnabled.Value ? "enabled" : "disabled")} customized movement speed",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdSetCustomizedMovementSpeed.Value))
                {
                    if (command.Args.Count < 1 || !float.TryParse(command.Args[0], out var newValue) || newValue < 1.0f)
                    {
                        CommandHandler.DisplayError("You must specify a value greater or equal to 1.0");

                        return;
                    }

                    _speedMultiplier.Value = newValue;

                    CommandHandler.DisplayMessage($"Successfully set the speed multiplier to {newValue}", PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleMiniGamesAutoCompletion.Value))
                {
                    _isMiniGamesAutoCompletionEnabled.Value = !_isMiniGamesAutoCompletionEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isMiniGamesAutoCompletionEnabled.Value ? "enabled" : "disabled")} mini-games auto-completion",
                        PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdToggleUnlockFortuneParty.Value))
                {
                    _isUnlockFortunePartyEnabled.Value = !_isUnlockFortunePartyEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isUnlockFortunePartyEnabled.Value ? "enabled" : "disabled")} unlock fortune for party",
                        PluginInfo.PLUGIN_NAME);
                }
            };

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        #region Better treasures
        [HarmonyPatch(typeof(EventOption), "InitActions")]
        public class EvenOptionInitActionsPatch
        {
            [UsedImplicitly]
            private static void Prefix(EventOption __instance)
            {
                if (!_isBetterTreasuresEnabled.Value)
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
        #endregion

        #region Customized movement speed
        [HarmonyPatch(typeof(GlobalSettings), "GetMovementSpeedMod")]
        public class GlobalSettingsGetMovementSpeedModPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref float __result)
            {
                if (!_isCustomizedMovementSpeedEnabled.Value)
                {
                    return;
                }

                __result *= _speedMultiplier.Value;
            }
        }
        #endregion

        #region Mini-games auto-completion
        [HarmonyPatch(typeof(ProfessionManager), "GatheringUpdateNew")]
        public class ProfessionManagerGatheringUpdateNewPatch
        {
            [UsedImplicitly]
            private static bool Prefix(
                ref ProfessionManager __instance,
                ref bool ___currentlyGathering,
                PlayerMovement ___professionPlayer,
                HexCell ___currentProfessionHex)
            {
                if (!_isMiniGamesAutoCompletionEnabled.Value)
                {
                    return true;
                }

                ___currentlyGathering = false;

                var herb = PortalManager.Instance.CurrentIsland.gatheringDict[___currentProfessionHex];
                var items = herb.GetItems();

                if (__instance.gatheringLoopSound)
                {
                    SoundManager.instance.StopSound(__instance.gatheringLoopSound);
                }

                var routine = AccessTools.Method(typeof(ProfessionManager), "ShowProfessionSuccess").Invoke(__instance,
                    new object[]
                    {
                        OptionsManager.Localize("Perfect!"),
                        items,
                        3,
                        ShakeIntensity.Light,
                        ProfessionType.Gathering,
                        ___currentProfessionHex,
                        null,
                        herb.NumCharges,
                        __instance.gatheringAnimDelay,
                        __instance.gatherPerfectSound,
                        __instance.gatherPerfectSoundVol
                    }) as IEnumerator;

                __instance.StartCoroutine(routine);
                ___professionPlayer.AnimManager.SetTrigger("GatheringSuccess");

                return false;
            }
        }

        [HarmonyPatch(typeof(ProfessionManager), "ExecuteStartFishing")]
        public class ProfessionManagerExecuteStartFishingPatch
        {
            [UsedImplicitly]
            private static void Postfix(
                ref ProfessionManager __instance,
                ref bool ___success,
                ref bool ___perfectCatch)
            {
                if (!_isMiniGamesAutoCompletionEnabled.Value)
                {
                    return;
                }

                ___success = true;
                ___perfectCatch = true;
                __instance.ProgressSlider.value = 1.0f;
            }
        }

        [HarmonyPatch(typeof(ProfessionManager), "MiningUpdate")]
        public class ProfessionManagerMiningUpdatePatch
        {
            [UsedImplicitly]
            private static bool Prefix(
                ref ProfessionManager __instance,
                ref bool ___currentlyMining,
                PlayerMovement ___professionPlayer,
                HexCell ___currentProfessionHex)
            {
                if (!_isMiniGamesAutoCompletionEnabled.Value)
                {
                    return true;
                }

                ___currentlyMining = false;

                var oreVein = PortalManager.Instance.CurrentIsland.miningDict[___currentProfessionHex];
                var items = oreVein.GetItems();

                var routine = AccessTools.Method(typeof(ProfessionManager), "ShowProfessionSuccess").Invoke(__instance,
                    new object[]
                    {
                        OptionsManager.Localize("Perfect!"),
                        items,
                        oreVein.OkayAmount * 4,
                        ShakeIntensity.Medium,
                        ProfessionType.Mining,
                        ___currentProfessionHex,
                        oreVein.successEffect,
                        oreVein.NumCharges,
                        0.85f,
                        ___professionPlayer.GetRandomAttackSound(),
                        0.25f
                    }) as IEnumerator;

                __instance.StartCoroutine(routine);

                ___professionPlayer.AnimManager.SetTrigger("MiningSuccess");

                return false;
            }
        }
        #endregion

        #region Unlock fortune for party
        [HarmonyPatch(typeof(Character), "AddFortune")]
        public class CharacterAddFortunePatch
        {
            [UsedImplicitly]
            private static void Postfix(string guid, float level)
            {
                if (!_isUnlockFortunePartyEnabled.Value)
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
        #endregion
    }
}

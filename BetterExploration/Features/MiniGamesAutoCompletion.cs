using System.Collections;
using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterExploration.Features
{
    [UsedImplicitly]
    public class MiniGamesAutoCompletion
    {
        private const bool IsMiniGamesAutoCompletionEnabledDefault = true;
        private const string CmdToggleMiniGamesAutoCompletionDefault = "t_gathering";

        private static ConfigEntry<string> _cmdToggleMiniGamesAutoCompletion;
        private static ConfigEntry<bool> _isMiniGamesAutoCompletionEnabled;

        public static void Register(ConfigFile config)
        {
            _isMiniGamesAutoCompletionEnabled = config.Bind("General", "minigamesautocompletion_enabled", IsMiniGamesAutoCompletionEnabledDefault, "Enable mini-games auto-completion");

            _cmdToggleMiniGamesAutoCompletion = config.Bind("Commands", "minigamesautocompletion_toggle", CmdToggleMiniGamesAutoCompletionDefault, "Toggle mini-games auto-completion");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleMiniGamesAutoCompletion);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleMiniGamesAutoCompletion.Value))
                {
                    _isMiniGamesAutoCompletionEnabled.Value = !_isMiniGamesAutoCompletionEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isMiniGamesAutoCompletionEnabled.Value ? "enabled" : "disabled")} mini-games auto-completion",
                        PluginInfo.PLUGIN_NAME);
                }
            };
        }

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
    }
}

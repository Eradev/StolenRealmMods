using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.EasyGathering
{
    [BepInDependency("eradev.stolenrealm.CommandHandler")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class EasyGatheringPlugin : BaseUnityPlugin
    {
        private const bool IsDisabledDefault = false;
        private const string CmdToggleDefault = "t_gathering";

        private static ConfigEntry<string> _cmdToggle;
        private static ConfigEntry<bool> _isDisabled;

        [UsedImplicitly]
        private void Awake()
        {
            _isDisabled = Config.Bind("General", "disabled", IsDisabledDefault, "Mod is disabled");

            _cmdToggle = Config.Bind("Commands", "toggle", CmdToggleDefault, $"Toggle {PluginInfo.PLUGIN_NAME} on/off");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggle);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggle.Value))
                {
                    _isDisabled.Value = !_isDisabled.Value;

                    CommandHandler.DisplayMessage($"{PluginInfo.PLUGIN_NAME}: Successfully {(_isDisabled.Value ? "disabled" : "enabled")}");
                }
            };

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(ProfessionManager), "GatheringUpdateNew")]
        public class ProfessionManagerGatheringUpdateNewPatch
        {
            [UsedImplicitly]
            private static bool Prefix(
                ref ProfessionManager __instance,
                // ReSharper disable once RedundantAssignment
                ref bool ___currentlyGathering,
                ref PlayerMovement ___professionPlayer)
            {
                if (_isDisabled.Value)
                {
                    return true;
                }

                ___currentlyGathering = false;

                var herb = PortalManager.Instance.CurrentIsland.gatheringDict[PortalManager.Instance.currentEventHex];
                var items = herb.GetItems();

                if (__instance.gatheringLoopSound)
                {
                    SoundManager.instance.StopSound(__instance.gatheringLoopSound);
                }

                var routine = AccessTools.Method(typeof(ProfessionManager), "ShowProfessionSuccess").Invoke(__instance,
                    new object[]
                    {
                        "Perfect!",
                        items,
                        3,
                        ShakeIntensity.Light,
                        ProfessionType.Gathering,
                        PortalManager.Instance.currentEventHex,
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
                // ReSharper disable once RedundantAssignment
                ref bool ___success,
                // ReSharper disable once RedundantAssignment
                ref bool ___perfectCatch)
            {
                if (_isDisabled.Value)
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
                // ReSharper disable once RedundantAssignment
                ref bool ___currentlyMining,
                ref PlayerMovement ___professionPlayer)
            {
                if (_isDisabled.Value)
                {
                    return true;
                }

                ___currentlyMining = false;

                var portalManager = PortalManager.Instance;

                var oreVein = portalManager.CurrentIsland.miningDict[portalManager.currentEventHex];
                var items = oreVein.GetItems();

                var routine = AccessTools.Method(typeof(ProfessionManager), "ShowProfessionSuccess").Invoke(__instance,
                    new object[]
                    {
                        "Perfect!",
                        items,
                        oreVein.OkayAmount * 4,
                        ShakeIntensity.Medium,
                        ProfessionType.Mining,
                        PortalManager.Instance.currentEventHex,
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

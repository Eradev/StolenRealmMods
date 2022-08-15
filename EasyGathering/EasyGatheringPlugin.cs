using System.Collections;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.EasyGathering
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class EasyGatheringPlugin : BaseUnityPlugin
    {
        [UsedImplicitly]
        private void Awake()
        {
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
                ___success = true;
                ___perfectCatch = true;
                __instance.ProgressSlider.value = 1.0f;
            }
        }
    }
}

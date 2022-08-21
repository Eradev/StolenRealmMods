﻿using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.RemoveDropsLimit
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RemoveDropsLimitPlugin : BaseUnityPlugin
    {
        private static ManualLogSource _log;

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        /* For DEBUG purposes  */
        /*[HarmonyPatch(typeof(LootTable), "GetLoot")]
        public class LootTableGetLootPatch
        {
            [UsedImplicitly]
            // ReSharper disable once RedundantAssignment
            private static void Prefix(ref float chanceModifier)
            {
                chanceModifier = 100f;
            }
        }*/

        [HarmonyPatch(typeof(GameLogic), "GenerateLoot")]
        public class GameLogicGenerateLootPatch
        {
            [HarmonyTranspiler]
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> RemoveDropsLimit(IEnumerable<CodeInstruction> instructions)
            {
                var codeInstructionList = new List<CodeInstruction>(instructions);
                var index = codeInstructionList.FindIndex(codeInstruction => codeInstruction.opcode == OpCodes.Ldc_I4_7);
                codeInstructionList[index] = new CodeInstruction(OpCodes.Ldc_I4, int.MaxValue);

                foreach (var codeInstruction in codeInstructionList)
                {
                    yield return codeInstruction;
                }
            }

            [UsedImplicitly]
            private static void Postfix(List<Item> __result)
            {
                _log.LogDebug($"Looted {__result.Count} item{(__result.Count > 0 ? "s" : "")}!");
            }
        }
    }
}

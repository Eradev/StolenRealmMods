using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterBattle.Features
{
    [UsedImplicitly]
    public class RemoveDropsLimit
    {
        private const bool IsDisplayLootInConsoleDisabledDefault = true;
        private const bool IsRemoveDropsLimitEnabledDefault = true;
        private const string CmdToggleRemoveDropsLimitDefault = "t_dropslimit";

        private static ConfigEntry<bool> _isDisplayLootInConsoleEnabled;

        private static ConfigEntry<string> _cmdToggleRemoveDropsLimit;
        private static ConfigEntry<bool> _isRemoveDropsLimitEnabled;

        public static void Register(ConfigFile config)
        {
            _isDisplayLootInConsoleEnabled = config.Bind("General", "displaylootinconsole_enabled", IsDisplayLootInConsoleDisabledDefault,
                "Enable display loot in console");
            _isRemoveDropsLimitEnabled = config.Bind("General", "removedropslimit_enabled", IsRemoveDropsLimitEnabledDefault,
                "Enable the removal of the drops limit");

            _cmdToggleRemoveDropsLimit = config.Bind("Commands", "removedropslimit_toggle", CmdToggleRemoveDropsLimitDefault,
                "Toggle the removal of drops limit");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleRemoveDropsLimit);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleRemoveDropsLimit.Value))
                {
                    _isRemoveDropsLimitEnabled.Value = !_isRemoveDropsLimitEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isRemoveDropsLimitEnabled.Value ? "enabled" : "disabled")} the removal of the drops limit",
                        PluginInfo.PLUGIN_NAME);
                }
            };
        }

        [HarmonyPatch(typeof(GameLogic), "GenerateLoot")]
        public class GameLogicGenerateLootPatch
        {
            [HarmonyTranspiler]
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> RemoveDropsLimit(IEnumerable<CodeInstruction> instructions)
            {
                var codeInstructionList = new List<CodeInstruction>(instructions);

                if (_isRemoveDropsLimitEnabled.Value)
                {
                    var index = codeInstructionList.FindIndex(codeInstruction => codeInstruction.opcode == OpCodes.Ldc_I4_7);
                    codeInstructionList[index] = new CodeInstruction(OpCodes.Ldc_I4, int.MaxValue);
                }

                foreach (var codeInstruction in codeInstructionList)
                {
                    yield return codeInstruction;
                }
            }

            [UsedImplicitly]
            private static void Postfix(List<Item> __result)
            {
                if (_isDisplayLootInConsoleEnabled.Value)
                {
                    return;
                }

                var itemCount = __result.Sum(x => x.numStacks);

                BetterBattlePlugin.Log.LogInfo($"Looted {itemCount} item{(itemCount > 0 ? "s" : "")}:");

                foreach (var item in __result)
                {
                    BetterBattlePlugin.Log.LogInfo($"  {Regex.Replace(OptionsManager.Localize(item.ItemName), "<.*?>", string.Empty)}{(item.numStacks > 1 ? $" ({item.numStacks})" : "")}");
                }
            }
        }
    }
}

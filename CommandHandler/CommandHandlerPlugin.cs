using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using Burst2Flame;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.CommandHandlerNS
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class CommandHandlerPlugin : BaseUnityPlugin
    {
        private const string CmdListDefault = "ls";
        private const string CmdClearDefault = "cls";
        private const string CmdSetCommandKeyDefault = "set_cmd_key";
        private const string CmdFindSkillDefault = "find_skill";

        private static ConfigEntry<string> _cmdKey;
        private static ConfigEntry<string> _cmdList;
        private static ConfigEntry<string> _cmdClear;
        private static ConfigEntry<string> _cmdSetCommandKey;
        private static ConfigEntry<string> _cmdFindSkill;

        [UsedImplicitly]
        private void Awake()
        {
            _cmdKey = Config.Bind("General", "commandKey", CommandHandler.CommandKeyDefault.ToString(), "(char) Command key. Each command sent to the chat must start with this key. Cannot be alphanumeric or a whitespace");

            _cmdList = Config.Bind("Commands", "list", CmdListDefault, "List all available commands");
            _cmdClear = Config.Bind("Commands", "clear", CmdClearDefault, "Clear the chat window");
            _cmdFindSkill = Config.Bind("Commands", "findskill", CmdFindSkillDefault, "[Args (string)] Search for a skill GUID by name");
            _cmdSetCommandKey = Config.Bind("Commands", "setCommandKey", CmdSetCommandKeyDefault, "[Args (char, non-alphanumeric)] Set a new command key");

            CommandHandler.SetLogger(Logger);
            CommandHandler.TrySetCommandKey(ref _cmdKey);

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdList);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdClear);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdFindSkill);
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdSetCommandKey);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdList.Value))
                {
                    var sb = new StringBuilder();

                    CommandHandler.LogInfo("---Available commands---");

                    sb.AppendLine("---Available commands---");
                    foreach (var availableCommand in CommandHandler.AvailableCommands)
                    {
                        var input = availableCommand.Value;
                        var extractedArgs = Regex.Match(input, "\\[Args ([^\\]]*)\\] ");
                        var capture = string.Empty;

                        if (extractedArgs.Success)
                        {
                            capture = extractedArgs.Groups[1].Captures[0].Value;

                            input = input.Replace(extractedArgs.Value, string.Empty);
                        }

                        // Cyan = #00FFFF
                        sb.AppendLine(
                            $"  <b><color=orange>{CommandHandler.CommandKey}{availableCommand.Key}</color>{(capture.IsNullOrWhiteSpace() ? "" : $" <color=#00FFFF>{capture}</color>")}</b> : {input}");
                        CommandHandler.LogInfo($"{CommandHandler.CommandKey}{availableCommand.Key}{(capture.IsNullOrWhiteSpace() ? "" : $" {capture}")} : {Regex.Replace(input, "<.*?>", string.Empty)}");
                    }

                    CommandHandler.DisplayMessage(sb.ToString(), PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdClear.Value))
                {
                    MessageWindowManager.instance.ClearAllMessages();
                }
                else if (command.Name.Equals(_cmdFindSkill.Value))
                {
                    if (command.Args.Count < 1 || string.IsNullOrWhiteSpace(string.Join(" ", command.Args)))
                    {
                        CommandHandler.DisplayError("You must specify a valid value");

                        return;
                    }

                    var searchTerm = string.Join(" ", command.Args);
                    var foundSkills = Game.Instance.Skills
                        .Distinct()
                        .Where(x =>
                            x.SkillName.ToLowerInvariant().Contains(searchTerm.ToLowerInvariant()) ||
                            OptionsManager.Localize(x.SkillName).ToLowerInvariant().Contains(searchTerm.ToLowerInvariant()))
                        .ToList();

                    if (!foundSkills.Any())
                    {
                        CommandHandler.DisplayError(
                            $"Cannot find a skill with the name '{searchTerm}'. Try with the english name if possible.");

                        return;
                    }

                    var sb = new StringBuilder();

                    sb.AppendLine($"Found {foundSkills.Count} {(foundSkills.Count > 1 ? "entries" : "entry")} with '{searchTerm}':");

                    foreach (var skill in foundSkills)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"  {OptionsManager.Localize(skill.SkillName)}");
                        sb.AppendLine($"  (English: {skill.SkillName})");
                        sb.AppendLine($"  {skill.Guid}");
                    }

                    CommandHandler.DisplayMessage(sb.ToString(), PluginInfo.PLUGIN_NAME);
                }
                else if (command.Name.Equals(_cmdSetCommandKey.Value))
                {
                    if (command.Args.Count < 1 || !char.TryParse(command.Args[0], out var newKey) || char.IsLetterOrDigit(newKey) || char.IsWhiteSpace(newKey))
                    {
                        CommandHandler.DisplayError("You must specify a valid value");

                        return;
                    }

                    _cmdKey.Value = newKey.ToString();

                    CommandHandler.TrySetCommandKey(ref _cmdKey);
                }
            };

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(OptionsManager), "Start")]
        public class OptionsManagerStartPatch
        {
            [UsedImplicitly]
            private static void Postfix()
            {
                CommandHandler.InvokeRegisterCommands();
            }
        }

        [HarmonyPatch(typeof(NetworkingManager), "ShutdownServer")]
        public class NetworkingManagerShutdownServerPatch
        {
            [UsedImplicitly]
            private static void Postfix()
            {
                CommandHandler.RemoveAllCommands();
            }
        }

        [HarmonyPatch(typeof(MessageWindowManager), "SendChatMessage")]
        public class MessageWindowManagerSendChatMessagePatch
        {
            [UsedImplicitly]
            private static bool Prefix(ref MessageWindowManager __instance)
            {
                if (!CommandHandler.IsMessageValidCommand(__instance.inputField.text, out var args))
                {
                    return true;
                }

                CommandHandler.InvokeHandleCommand(args);

                __instance.inputField.text = "";

                return false;
            }
        }
    }
}

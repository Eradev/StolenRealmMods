using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace eradev.stolenrealm.CommandHandlerNS
{
    public class CommandHandler
    {
        public const char CmdKeyDefault = '/';

        public static event EventHandler RegisterCommandsEvt;
        public static event EventHandler<CommandEventArgs> HandleCommandEvt;

        protected static CommandHandler InstanceInternal;
        protected static ManualLogSource LoggerInternal;
        protected static char CommandKeyInternal;
        protected static Dictionary<string, string> AvailableCommandsInternal = new();

        protected CommandHandler() { }

        public static CommandHandler Inst => InstanceInternal ??= new CommandHandler();
        public static ReadOnlyDictionary<string, string> AvailableCommands => new(AvailableCommandsInternal);
        public static char CommandKey => CommandKeyInternal;

        public void SetLogger(ManualLogSource logger)
        {
            LoggerInternal = logger;
        }

        public static void TrySetCommandKey(ref ConfigEntry<string> commandKey)
        {
            if (commandKey.Value.Length > 1)
            {
                LogWarning($"The command key must be a char. Defaulting to '{CmdKeyDefault}'");

                commandKey.Value = CmdKeyDefault.ToString();

            }
            else if (char.IsLetterOrDigit(commandKey.Value[0]) || char.IsWhiteSpace(commandKey.Value[0]))
            {
                LogWarning($"The command key cannot be alphanumeric or a space char. Defaulting to '{CmdKeyDefault}'");

                commandKey.Value = CmdKeyDefault.ToString();
            }

            CommandKeyInternal = commandKey.Value[0];

            var message = $"Command key set to '{CommandKeyInternal}'";
            LogInfo(message);
        }

        public static void TryAddCommand(string modName, ref ConfigEntry<string> command)
        {
            if (command.Value.Contains(" "))
            {
                LogWarning($"Commands cannot have whitespace in it. Command [{modName}]{command.Definition.Key} defaulted to {CommandKey}{command.DefaultValue}");

                command.Value = command.DefaultValue.ToString();
            }

            if (AvailableCommandsInternal.ContainsKey(command.Value))
            {
                LogError($"Command {CommandKey}{command.Value} cannot be added as it already exists.");

                return;
            }

            AvailableCommandsInternal.Add(command.Value, $"{command.Description.Description} <size=8>[{modName}]</size>");

            LogDebug($"Command {CommandKey}{command.Value} registered.");
        }

        public static void RemoveAllCommands()
        {
            AvailableCommandsInternal.Clear();

            LogDebug("All commands cleared.");
        }

        public static bool IsMessageValidCommand(string message, out List<string> commandArgs)
        {
            commandArgs = new List<string>();

            var trimmedMessage = message.TrimEnd();

            if (!trimmedMessage.StartsWith(CommandKeyInternal.ToString()))
            {
                return false;
            }

            commandArgs = trimmedMessage
                .Substring(1)
                .Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return commandArgs.Count != 0 && AvailableCommands.ContainsKey(commandArgs[0]);
        }

        public static void InvokeRegisterCommands()
        {
            RegisterCommandsEvt?.Invoke(Inst, EventArgs.Empty);
        }

        public static void InvokeHandleCommand(List<string> args)
        {
            HandleCommandEvt?.Invoke(Inst, new CommandEventArgs(args));
        }

        public static void DisplayMessage(string message)
        {
            MessageWindowManager.instance.AddMessage(message, MessageWindowMessageType.Chat);
        }

        public static void LogDebug(object data)
        {
            if (LoggerInternal != null)
            {
                LoggerInternal.LogDebug(data);
            }
            else
            {
                Debug.Log(data);
            }
        }

        public static void LogInfo(object data)
        {
            if (LoggerInternal != null)
            {
                LoggerInternal.LogInfo(data);
            }
            else
            {
                Debug.Log(data);
            }
        }

        public static void LogWarning(object data)
        {
            if (LoggerInternal != null)
            {
                LoggerInternal.LogWarning(data);
            }
            else
            {
                Debug.LogWarning(data);
            }
        }

        public static void LogError(object data)
        {
            if (LoggerInternal != null)
            {
                LoggerInternal.LogError(data);
            }
            else
            {
                Debug.LogError(data);
            }
        }
    }
}

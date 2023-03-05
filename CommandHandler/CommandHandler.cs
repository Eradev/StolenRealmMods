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
        public const char CommandKeyDefault = '/';

        public static event EventHandler RegisterCommandsEvt;
        public static event EventHandler<CommandEventArgs> HandleCommandEvt;

        private static CommandHandler _instanceInternal;
        private static ManualLogSource _loggerInternal;
        private static readonly Dictionary<string, string> AvailableCommandsInternal = new();

        private CommandHandler() { }

        // ReSharper disable once MemberCanBePrivate.Global
        public static CommandHandler Inst => _instanceInternal ??= new CommandHandler();
        public static ReadOnlyDictionary<string, string> AvailableCommands => new(AvailableCommandsInternal);
        public static char CommandKey { get; private set; }

        public static void SetLogger(ManualLogSource logger)
        {
            _loggerInternal = logger;
        }

        public static void TrySetCommandKey(ref ConfigEntry<string> commandKey)
        {
            if (commandKey.Value.Length > 1)
            {
                LogWarning($"The command key must be a char. Defaulting to '{CommandKeyDefault}'");

                commandKey.Value = CommandKeyDefault.ToString();

            }
            else if (char.IsLetterOrDigit(commandKey.Value[0]) || char.IsWhiteSpace(commandKey.Value[0]))
            {
                LogWarning($"The command key cannot be alphanumeric or a space char. Defaulting to '{CommandKeyDefault}'");

                commandKey.Value = CommandKeyDefault.ToString();
            }

            CommandKey = commandKey.Value[0];

            var message = $"Command key set to '{CommandKey}'";
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

            if (!trimmedMessage.StartsWith(CommandKey.ToString()))
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

        public static void BroadcastMessage(string message, string source = null)
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                message = $"<color=orange>{source}:</color> {message}";
            }

            MessageWindowManager.instance.SendMessageToAll(-1, message, MessageWindowMessageType.Chat, null, null, 0f);
        }

        public static void DisplayMessage(string message, string source = null)
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                message = $"<color=orange>{source}:</color> {message}";
            }

            MessageWindowManager.instance.AddMessage(message, MessageWindowMessageType.Chat);
        }

        public static void DisplayError(string message)
        {
            message = $"<color=red>Error:</color> {message}";

            MessageWindowManager.instance.AddMessage(message, MessageWindowMessageType.Chat);
        }

        public static void DisplayNotification(string message, string title = null)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                message = $"<big>{title}</big>\n{message}";
            }

            NotificationManager.instance.DisplayNotification(message);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void LogDebug(object data)
        {
            if (_loggerInternal != null)
            {
                _loggerInternal.LogDebug(data);
            }
            else
            {
                Debug.Log(data);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void LogInfo(object data)
        {
            if (_loggerInternal != null)
            {
                _loggerInternal.LogInfo(data);
            }
            else
            {
                Debug.Log(data);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void LogWarning(object data)
        {
            if (_loggerInternal != null)
            {
                _loggerInternal.LogWarning(data);
            }
            else
            {
                Debug.LogWarning(data);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void LogError(object data)
        {
            if (_loggerInternal != null)
            {
                _loggerInternal.LogError(data);
            }
            else
            {
                Debug.LogError(data);
            }
        }
    }
}

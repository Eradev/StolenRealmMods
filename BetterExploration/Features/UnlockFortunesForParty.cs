using System.Linq;
using BepInEx.Configuration;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterExploration.Features
{
    [UsedImplicitly]
    public class UnlockFortunesForParty
    {
        private const bool IsUnlockFortunePartyEnabledDefault = true;
        private const string CmdToggleUnlockFortunePartyDefault = "t_fortune4party";

        private static ConfigEntry<string> _cmdToggleUnlockFortuneParty;
        private static ConfigEntry<bool> _isUnlockFortunePartyEnabled;

        public static void Register(ConfigFile config)
        {
            _isUnlockFortunePartyEnabled = config.Bind("General", "unlockfortuneparty_enabled", IsUnlockFortunePartyEnabledDefault,
                "Enable unlock fortune for your party");

            _cmdToggleUnlockFortuneParty = config.Bind("Commands", "unlockfortuneparty_toggle", CmdToggleUnlockFortunePartyDefault, "Toggle unlock fortune for your party");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleUnlockFortuneParty);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleUnlockFortuneParty.Value))
                {
                    _isUnlockFortunePartyEnabled.Value = !_isUnlockFortunePartyEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isUnlockFortunePartyEnabled.Value ? "enabled" : "disabled")} unlock fortune for party",
                        PluginInfo.PLUGIN_NAME);
                }
            };
        }

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
    }
}

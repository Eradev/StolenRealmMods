using System.Linq;
using BepInEx.Configuration;
using Burst2Flame.Observable;
using eradev.stolenrealm.BetterBattle.Shared;
using eradev.stolenrealm.CommandHandlerNS;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.BetterBattle.Features
{
    [UsedImplicitly]
    public class AutoCastAuras
    {
        private const bool IsAutoCastAurasEnabledDefault = true;
        private const string CmdToggleAutoCastAurasDefault = "t_auras";

        private static ConfigEntry<string> _cmdToggleAutoCastAuras;
        private static ConfigEntry<bool> _isAutoCastAurasEnabled;

        private static bool _autoCastAuraRanOnce;

        public static void Register(ConfigFile config)
        {
            _isAutoCastAurasEnabled = config.Bind("General", "autocastauras_enabled", IsAutoCastAurasEnabledDefault,
                "Enable auto-cast auras at the start of battles");

            _cmdToggleAutoCastAuras =
                config.Bind("Commands", "autocastauras_toggle", CmdToggleAutoCastAurasDefault, "Toggle auto-cast auras");

            CommandHandler.RegisterCommandsEvt += (_, _) =>
            {
                CommandHandler.TryAddCommand(PluginInfo.PLUGIN_NAME, ref _cmdToggleAutoCastAuras);
            };

            CommandHandler.HandleCommandEvt += (_, command) =>
            {
                if (command.Name.Equals(_cmdToggleAutoCastAuras.Value))
                {
                    _isAutoCastAurasEnabled.Value = !_isAutoCastAurasEnabled.Value;

                    CommandHandler.DisplayMessage(
                        $"Successfully {(_isAutoCastAurasEnabled.Value ? "enabled" : "disabled")} auto-cast auras",
                        PluginInfo.PLUGIN_NAME);
                }
            };
        }

        [HarmonyPatch(typeof(GameLogic), "Update")]
        public class GameLogicUpdatePatch
        {
            [UsedImplicitly]
            private static void Postfix(GameLogic __instance)
            {
                if (!_isAutoCastAurasEnabled.Value ||
                    GUIManager.instance.CurrentGuiState != GUIState.InBattle ||
                    !NetworkingManager.Instance.NetworkManager.Root.IsPlayerTurnAndReady ||
                    __instance.numPlayerTurnsStarted > 1 ||
                    _autoCastAuraRanOnce)
                {
                    return;
                }

                foreach (var character in NetworkingManager.Instance.MyPartyCharacters.Where(x => x.IsMyTurn && !x.IsDead))
                {
                    var auras = character.Actions
                        .Select(x => x.ActionInfo)
                        .Where(x => x.StatusEffects.Any(y => y.IsAura))
                        .ToList();

                    foreach (var actionInfo in auras)
                    {
                        var canCastInfo = character.PlayerMovement.CanCast(new StructList<HexCell>
                        {
                            null
                        }, actionInfo);

                        if (!canCastInfo.CanCast ||
                            !canCastInfo.NotYetActivated)
                        {
                            continue;
                        }

                        __instance.StartCoroutine(Coroutines.QueueCast(character, character, actionInfo));
                    }
                }

                _autoCastAuraRanOnce = true;
            }
        }

        [HarmonyPatch(typeof(GameLogic), "StartNewTurnSequence")]
        public class GameLogicStartNewTurnSequencePatch
        {
            [UsedImplicitly]
            private static void Postfix()
            {
                _autoCastAuraRanOnce = false;
            }
        }
    }
}

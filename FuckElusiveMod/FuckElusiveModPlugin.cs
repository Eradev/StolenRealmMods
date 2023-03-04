using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Burst2Flame;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.FuckElusiveMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class FuckElusiveModPlugin : BaseUnityPlugin
    {
        private static ManualLogSource _log;

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(Game), "Awake")]
        public class GameAwakePatch
        {
            [UsedImplicitly]
            // ReSharper disable once RedundantAssignment
            // ReSharper disable once InconsistentNaming
            private static void Postfix(ref EnemyMod[] ____EnemyMods)
            {
                // Init the list if empty
                var enemyMods = Game.Instance.EnemyMods;

                foreach(var enemyMod in enemyMods.Where(x => x.specialEffects.Contains(SpecialEffect.Elusive)))
                {
                    _log.LogDebug("Removed enemy mod with Elusive because FUCK ELUSIVE.");

                    enemyMod.RemoveFromEnemyModPool = true;
                }

                ____EnemyMods = enemyMods;
            }
        }
    }
}

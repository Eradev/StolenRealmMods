using System.Linq;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;

namespace eradev.stolenrealm.ReplayQuests
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ReplayQuestsPlugin : BaseUnityPlugin
    {
        [UsedImplicitly]
        private void Awake()
        {
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    [HarmonyPatch(typeof(QuestSelectManager), "UpdateQuestNodes")]
    public class QuestSelectManagerUpdateQuestNodesPatch
    {
        [UsedImplicitly]
        private static void Postfix(ref QuestSelectManager __instance)
        {
            var questSelectManager = __instance;

            if (!questSelectManager.Contents.gameObject.activeSelf)
            {
                return;
            }

            foreach (var questNode in __instance.ActQuestNodeDict.Keys
                         .SelectMany(actKey => questSelectManager.ActQuestNodeDict[actKey]
                             .Where(questNode => questNode.questNodeType == QuestNodeType.Quest && questNode.CurrentState == QuestNodeState.Completed)))
            {
                questNode.CurrentState = QuestNodeState.Current;
            }
        }
    }
}

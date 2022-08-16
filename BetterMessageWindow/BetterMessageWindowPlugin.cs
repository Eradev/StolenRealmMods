using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace eradev.stolenrealm.BetterMessageWindow
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class BetterMessageWindowPlugin : BaseUnityPlugin
    {
        private static CanvasGroupShowOnHover _instance;

        [UsedImplicitly]
        private void Awake()
        {
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(CameraController), "Update")]
        public class CameraControllerUpdatePatch
        {
            [UsedImplicitly]
            private static bool Prefix()
            {
                return !MessageWindowManager.instance.TextInputIsFocused;
            }
        }

        [HarmonyPatch(typeof(KeybindManager), "Update")]
        public class KeybindManagerUpdatePatch
        {
            [UsedImplicitly]
            private static void Prefix(ref KeybindManager __instance)
            {
                var guiManager = GUIManager.instance;
                if (__instance.DisableKeybinds ||
                    guiManager.CurrentGuiState is GUIState.InMainMenu or GUIState.ChoosingCharacter or GUIState.CreatingCharacter)
                {
                    return;
                }

                if (!Input.GetKeyDown(KeyCode.Return))
                {
                    return;
                }

                var messageWindowManager = MessageWindowManager.instance;

                if (!messageWindowManager.isActiveAndEnabled)
                {
                    messageWindowManager.OpenChatWindow();
                }

                if (!messageWindowManager.TextInputIsFocused)
                {
                    messageWindowManager.inputField.ActivateInputField();
                }
            }
        }

        [HarmonyPatch(typeof(MessageWindowManager), "SendChatMessage")]
        public class MessageWindowManagerSendChatMessagePatch
        {
            [UsedImplicitly]
            private static void Postfix(ref MessageWindowManager __instance)
            {
                __instance.inputField.ActivateInputField();
            }
        }

        [HarmonyPatch(typeof(MessageWindowManager), "IsFocused", MethodType.Getter)]
        public class MessageWindowManagerIsFocusedPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref MessageWindowManager __instance, ref bool __result)
            {
                __result = __result || __instance.TextInputIsFocused;
            }
        }

        [HarmonyPatch(typeof(CanvasGroupShowOnHover), "Hovering", MethodType.Getter)]
        public class CanvasGroupShowOnHoverHoveringPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref CanvasGroupShowOnHover __instance, ref bool __result)
            {
                if (_instance == null)
                {
                    var messageWindowCanvasGroup = AccessTools.PropertyGetter(typeof(MessageWindowManager), "CG")
                        .Invoke(MessageWindowManager.instance, null) as CanvasGroup;
                    var instanceCanvasGroup = AccessTools.PropertyGetter(typeof(CanvasGroupShowOnHover), "CG")
                        .Invoke(__instance, null) as CanvasGroup;

                    if (messageWindowCanvasGroup != instanceCanvasGroup)
                    {
                        return;
                    }

                    _instance = __instance;
                }
                else if (_instance != __instance)
                {
                    return;
                }

                __result = __result || MessageWindowManager.instance.TextInputIsFocused;
            }
        }
    }
}

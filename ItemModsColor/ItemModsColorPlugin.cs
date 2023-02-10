using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace eradev.stolenrealm.ItemModsColor
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ItemModsColorPlugin : BaseUnityPlugin
    {
        [UsedImplicitly]
        private void Awake()
        {
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(Item), "ItemName", MethodType.Getter)]
        public class ItemItemNamePatch
        {
            [UsedImplicitly]
            private static bool Prefix(
                ref Item __instance,
                // ReSharper disable once RedundantAssignment
                ref string __result)
            {
                if (__instance.ItemMods == null || __instance.ItemMods.Count == 0)
                {
                    __result = OptionsManager.Localize(__instance.ItemInfo.ItemName);

                    return false;
                }

                var prefixMod = __instance.PrefixMod;
                var suffixMod = __instance.SuffixMod;
                var endGameMod = __instance.EndGameMod;

                var prefix = "";
                var suffix = "";
                var endgame = "";

                var languageGender = OptionsManager.GetLanguageGender(__instance.ItemInfo.ItemName);

                if (prefixMod)
                {
                    var color = ColorUtility.ToHtmlStringRGB(GlobalSettingsManager.instance.globalSettings.GetItemQualityColor(prefixMod.Rarity));

                    prefix = $"<color=#{color}>{OptionsManager.Localize(prefixMod.name, languageGender)}</color> ";
                }

                if (suffixMod)
                {
                    var color = ColorUtility.ToHtmlStringRGB(GlobalSettingsManager.instance.globalSettings.GetItemQualityColor(suffixMod.Rarity));

                    suffix = $" <color=#{color}>{OptionsManager.Localize($"{suffixMod.SuffixPretext.Trim()} {suffixMod.name}", languageGender)}</color>";
                }

                if (endGameMod)
                {
                    var color = ColorUtility.ToHtmlStringRGB(GlobalSettingsManager.instance.endGameSettings.ColorsByTier[(int)endGameMod.EndGameItemModTierType]);

                    endgame = $"<color=#{color}>*</color>";
                }

                __result = $"{prefix}{OptionsManager.Localize(__instance.ItemInfo.ItemName)}{suffix}{endgame}";

                return false;
            }
        }

        [HarmonyPatch(typeof(ItemSlot), "Item", MethodType.Setter)]
        public class ItemSlotItemPatch
        {
            [UsedImplicitly]
            private static void Postfix(ref ItemSlot __instance)
            {
                if (!__instance.Disabled || !__instance.ItemText || __instance.Item == null)
                {
                    return;
                }

                var colorRegex = new Regex("<color=#([A-Z0-9]+)>");
                var item = __instance.Item;
                var stackText = item.IsStackable && item.numStacks > 1
                    ? $" ({item.numStacks})"
                    : "";

                var disabledName = item.ItemName;
                foreach (Match match in colorRegex.Matches(item.ItemName))
                {
                    if (!ColorUtility.TryParseHtmlString($"#{match.Groups[1].Value}", out var color))
                    {
                        continue;
                    }

                    var disabledColor = new Color(color.r, color.g, color.b, 0.2f);

                    disabledName = disabledName.Replace(match.Value,
                        $"<color=#{ColorUtility.ToHtmlStringRGBA(disabledColor)}>");
                }

                __instance.ItemText.text = $"{disabledName}{stackText}";
            }
        }
    }
}

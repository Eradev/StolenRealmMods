﻿using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Burst2Flame;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace eradev.stolenrealm.BetterShopSelection
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class BetterShopSelectionPlugin : BaseUnityPlugin
    {
        // ReSharper disable once NotAccessedField.Local
        private static ManualLogSource _log;

        [UsedImplicitly]
        private void Awake()
        {
            _log = Logger;

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(ShopManager), "RefreshItemDictSingle")]
        public class ShopManagerRefreshItemDictSinglePatch
        {
            [UsedImplicitly]
            private static bool Prefix(
                // ReSharper disable once IdentifierTypo
                ref Dictionary<Shopkeeper, List<Item>> ___shopkeepItemDict,
                int level,
                Shopkeeper shopkeeper)
            {
                if (shopkeeper.isMaterialVender || shopkeeper.isStash)
                {
                    return true;
                }

                ___shopkeepItemDict ??= new Dictionary<Shopkeeper, List<Item>>();

                var randomItemLevelList = new List<int>
                {
                  level >= 30 ? level : Mathf.Max(1, level - 1),
                  level >= 30 ? level : Mathf.Max(1, level - 1),
                  level,
                  level,
                  level,
                  level >= 30 ? level : level + 1,
                  level >= 30 ? level : level + 1,
                  level >= 30 ? level : level + 2,
                  level >= 30 ? level : level + 3
                };

                var itemInfos = new List<ItemInfo>();
                var shopKeeperItems = new List<Item>();

                foreach (var itemTypesChance in shopkeeper.itemTypesChances)
                {
                    var itemTypeChance = itemTypesChance;
                    var availableLoot = (shopkeeper.itemPoolType == UniversalLootTableType.Unassigned
                        ? Game.Instance.WorldLootItems
                        : Game.Instance.Items.Where(x => x.AllowInAllShops || x.AllowedShops.Contains(shopkeeper))).ToList();

                    if (itemTypeChance.useAll)
                    {
                        if (!itemTypeChance.isWeapon)
                        {
                            itemInfos.AddRange(availableLoot.Where(x => itemTypeChance.itemTypes.Contains(x.ItemType)));
                        }
                    }
                    else
                    {
                        var rarityChance = new List<RarityChance>
                        {
                            new() {Chance = 0.85f, Rarity = ItemQuality.Rare},
                            new() {Chance = 0.1f, Rarity = ItemQuality.Legendary},
                            new() {Chance = 0.05f, Rarity = ItemQuality.Mythic}
                        };

                        for (var i = 0; i < itemTypeChance.maxCount + 1; i++)
                        {
                            var rarity = (int)Game.RandomBasedOnWeight(rarityChance, x => x.Chance).Rarity;

                            if (itemTypeChance.isWeapon)
                            {
                                var list = availableLoot.Where(x => x.IsWeaponInfo).Select(x => x as WeaponInfo).Where(x => !itemInfos.Contains(x) && itemTypeChance.weaponTypes.Contains(x.EquipmentType) && x.Rarity == (ItemQuality)rarity).ToList();

                                if (list.Count > 0)
                                {
                                    itemInfos.Add(list[Random.Range(0, list.Count)]);
                                }
                            }
                            else
                            {
                                var list = availableLoot.Where(x => !itemInfos.Contains(x) && itemTypeChance.itemTypes.Contains(x.ItemType) && x.Rarity == (ItemQuality)rarity).ToList();

                                if (list.Count > 0)
                                {
                                    itemInfos.Add(list[Random.Range(0, list.Count)]);
                                }
                            }
                        }
                    }
                }
 
                foreach (var itemInfo in itemInfos)
                {
                    var iLvl = Mathf.Min(30, randomItemLevelList[Random.Range(0, randomItemLevelList.Count)]);

                    var rollModsLevel = !shopkeeper.allowTierDrops || iLvl < 30
                        ? 0
                        : NetworkingManager.Instance.MyPartyCharacters.Max(x => x.ShopHighestLevel);
                    var newItem = GameLogic.instance.CreateItem(itemInfo, iLvl, rollModsLevel, fromShop: true);

                    if (itemInfo.IsEquippable)
                    {
                        var isAccessory = itemInfo.ItemType is ItemType.Amulet or ItemType.Ring;

                        ItemMod prefix = null;
                        ItemMod suffix = null;

                        do
                        {
                            var itemMod = GlobalSettingsManager.instance.globalSettings.GetItemMod(newItem, true);

                            if (itemMod == null)
                            {
                                break;
                            }

                            switch (itemMod.ItemModType)
                            {
                                case ItemModType.Prefix when prefix == null:
                                    prefix = itemMod;

                                    break;

                                case ItemModType.Suffix when suffix == null:
                                    suffix = itemMod;

                                    break;
                            }
                        } while (prefix == null || (!isAccessory && suffix == null));

                        if (prefix)
                        {
                            newItem.AddItemMod(prefix);
                        }

                        if (suffix)
                        {
                            newItem.AddItemMod(suffix);
                        }
                    }

                    shopKeeperItems.Add(newItem);
                }

                ___shopkeepItemDict[shopkeeper] = shopKeeperItems.OrderBy(x => x.ItemInfo.ItemType).ThenByDescending(x => x.ItemInfo.Rarity).ToList();

                return false;
            }
        }

        [HarmonyPatch(typeof(ItemUpgradeManager), "Init")]
        public class ItemUpgradeManagerInitPatch
        {
            [UsedImplicitly]
            private static void Postfix(
                ItemUpgradeManager __instance,
                Character character,
                // ReSharper disable once InconsistentNaming
                ref Inventory ____ItemList,
                ref Dictionary<Item, Item> ___upgradeDict)
            {
                if (character.Level < character.MaxLevel)
                {
                    return;
                }

                var upgradableTierItems = character.EquippedItems
                    .Where(x => x.itemLevel == (int)character.MaxLevel && (x.EndGameMod == null || x.EndGameMod.EndGameItemModTierType != EndGameItemModTierType.Tier3))
                    .ToList();

                ____ItemList.AllItems.AddRange(upgradableTierItems);
                ____ItemList.InitializeInventory(____ItemList.AllItems, showEquipped: true);

                var itemMods = Game.Instance.ItemMods.Where(x => x.ItemModType == ItemModType.EndGame).ToList();
                var tier1Mod = itemMods.FirstOrDefault(x => x.EndGameItemModTierType == EndGameItemModTierType.Tier1);
                var tier2Mod = itemMods.FirstOrDefault(x => x.EndGameItemModTierType == EndGameItemModTierType.Tier2);
                var tier3Mod = itemMods.FirstOrDefault(x => x.EndGameItemModTierType == EndGameItemModTierType.Tier3);

                foreach (var item in upgradableTierItems)
                {
                    var itemCopy = GameLogic.instance.CreateItemCopy(item);

                    if (item.EndGameMod == null)
                    {
                        itemCopy.AddItemMod(tier1Mod);
                    }
                    else
                    {
                        switch (item.EndGameMod.EndGameItemModTierType)
                        {
                            case EndGameItemModTierType.Tier1:
                                itemCopy.AddItemMod(tier2Mod);

                                break;

                            case EndGameItemModTierType.Tier2:
                                itemCopy.AddItemMod(tier3Mod);

                                break;
                        }
                    }
                    itemCopy.Init();

                    ___upgradeDict[item] = itemCopy;
                }

                __instance.SelectUpgradeItem(____ItemList.CurrentItemSlots.FirstOrDefault());
                __instance.NoItemsInventory.SetActive(!____ItemList.AllItems.Any());
            }
        }

        /*[HarmonyPatch(typeof(Inventory), "SetItemSlotOverrides")]
        public class InventorySetItemSlotOverridesPatch
        {
            public static string GetCategoryFromItemInfo(ItemInfo itemInfo)
            {
                if (itemInfo.IsWeaponInfo)
                {
                    switch (((WeaponInfo)itemInfo).EquipmentType)
                    {
                        case EquipmentType.Bow:
                        case EquipmentType.Gun_1H:
                        case EquipmentType.Gun_2H:
                        case EquipmentType.Staff:
                        case EquipmentType.Wand:
                            return "ranged weapon";

                        default:
                            return "melee weapon";
                    }
                }

                switch (itemInfo.ItemType)
                {
                    case ItemType.Armor:
                    case ItemType.Head:
                    case ItemType.Shield:
                        return "armor";

                    case ItemType.Ring:
                    case ItemType.Amulet:
                        return "accessory";
                }

                return "undefined";
            }

            public static bool Prefix(float cost, Color color, List<ItemSlot> ___currentItemSlots)
            {
                foreach (var currentItemSlot in ___currentItemSlots)
                {
                    currentItemSlot.SetOverrides(cost, $"Mystery {GetCategoryFromItemInfo(currentItemSlot.ItemInfo)}", color);
                    currentItemSlot.Icon.sprite = null;
                    currentItemSlot.Icon.color = new Color(0f, 0f, 0f, 0f);
                }

                return false;
            }
        }*/
    }
}

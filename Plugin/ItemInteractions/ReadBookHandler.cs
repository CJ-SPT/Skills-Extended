using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using JetBrains.Annotations;
using SkillsExtended.Buffs;
using SkillsExtended.Controllers;
using SkillsExtended.Models;
using UnityEngine;

namespace SkillsExtended.ItemInteractions;

internal static class ReadBookHandler
{
    private static FieldInfo _inventoryControllerFieldInfo;
    private static ItemUiContext _itemUiContext => ItemUiContext.Instance;
    private static InventoryControllerClass _inventoryController;
    
    static ReadBookHandler()
    {
        _inventoryControllerFieldInfo = AccessTools.Field(typeof(ItemUiContext), "inventoryControllerClass");
    }
    
    public static void ReadBook(Item item)
    {
        PreloaderUI.Instance.ShowCriticalErrorScreen(
            "Are you sure?",
            $"Do you want to consume the book?",
            ErrorScreen.EButtonType.OkButton,
            10f,
            () => ApplyBookBuff(item),
            () => { });
    }
    
    private static void ApplyBookBuff(Item item)
    {
        if (_inventoryController is null)
        {
            _inventoryController = (InventoryControllerClass)_inventoryControllerFieldInfo
                .GetValue(_itemUiContext);
        }
        
        var buff = CreateBuff(item);

        if (buff is null)
        {
            Plugin.Log.LogError($"Missing or not implemented buff for item: {item.TemplateId}");
            return;
        }
        
        BuffController.ApplyBuff(buff);
        
        //_inventoryController.TryThrowItem(item, null, true);
    }

    [CanBeNull]
    private static AbstractBuff CreateBuff(Item item)
    {
        var buff = GetBuff(item);

        if (buff is null) return null;
        
        switch (buff.SkillType)
        {
            case ESkillId.Lockpicking:
                return new LockPickingBuff(buff.Strength, buff.DurationInSeconds);
            
            default:
                return null;
        }
    }
    
    [CanBeNull]
    public static SkillBuffModel GetBuff(Item item)
    {
        return BuffController.Buffs.SkillBuffs.FirstOrDefault(buff => buff.ItemId == item.TemplateId);
    }
}


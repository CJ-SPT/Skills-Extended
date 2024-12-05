using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Communications;
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
    private static InventoryController _inventoryController;
    
    static ReadBookHandler()
    {
        _inventoryControllerFieldInfo = AccessTools.Field(typeof(ItemUiContext), "inventoryController_0");
    }
    
    public static void ReadBook(Item item)
    {
        var buff = GetBuffModel(item);
        
        if (buff is null)
        {
            Plugin.Log.LogError($"Missing or not implemented buff for item: {item.TemplateId}");
            return;
        }

        var activeBuff = BuffController.GetActiveBuffForSkill(buff.SkillType);

        if (activeBuff is not null)
        {
            NotificationManagerClass.DisplayMessageNotification(
                "Buff already active for this skill", 
                ENotificationDurationType.Default, 
                ENotificationIconType.Alert);
            
            return;
        }
        
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
        var buff = CreateBuff(item);

        if (buff is null)
        {
            Plugin.Log.LogError($"Missing or not implemented buff for item: {item.TemplateId}");
            return;
        }
        
        BuffController.ApplyBuff(buff);
        
        if (_inventoryController is null)
        {
            _inventoryController = (InventoryController)_inventoryControllerFieldInfo
                .GetValue(_itemUiContext);
        }
        
        _inventoryController.TryThrowItem(item, null, true);
    }

    [CanBeNull]
    private static AbstractBuff CreateBuff(Item item)
    {
        var buffModel = GetBuffModel(item);

        if (buffModel is null) return null;
        
        switch (buffModel.SkillType)
        {
            case ESkillId.Lockpicking:
                return new LockPickingBuff(buffModel);
            
            default:
                return null;
        }
    }
    
    [CanBeNull]
    public static SkillBuffModel GetBuffModel(Item item)
    {
        return BuffController.Buffs.SkillBuffs.FirstOrDefault(buff => buff.ItemId == item.TemplateId);
    }
}


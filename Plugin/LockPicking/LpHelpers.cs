using System;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using SkillsExtended.Models;
using System.Collections.Generic;
using System.Linq;
using SkillsExtended.Helpers;
using SkillsExtended.Skills;

namespace SkillsExtended.LockPicking;

internal static class LpHelpers
{
    public static readonly Dictionary<string, int> DoorAttempts = [];
    public static readonly List<string> InspectedDoors = [];
    
    private static readonly Dictionary<string, Dictionary<string, int>> LocationDoorIdLevels = new()
    {
        {"factory4_day", Plugin.SkillData.LockPicking.DoorPickLevels.Factory},
        {"factory4_night", Plugin.SkillData.LockPicking.DoorPickLevels.Factory},
        {"Woods", Plugin.SkillData.LockPicking.DoorPickLevels.Woods},
        {"bigmap", Plugin.SkillData.LockPicking.DoorPickLevels.Customs},
        {"Interchange", Plugin.SkillData.LockPicking.DoorPickLevels.Interchange},
        {"RezervBase", Plugin.SkillData.LockPicking.DoorPickLevels.Reserve},
        {"Shoreline", Plugin.SkillData.LockPicking.DoorPickLevels.Shoreline},
        {"laboratory", Plugin.SkillData.LockPicking.DoorPickLevels.Labs},
        {"Lighthouse", Plugin.SkillData.LockPicking.DoorPickLevels.Lighthouse},
        {"TarkovStreets", Plugin.SkillData.LockPicking.DoorPickLevels.Streets},
        {"Sandbox", Plugin.SkillData.LockPicking.DoorPickLevels.GroundZero},
    };
    
    /// <summary>
    /// Get the door level given a location ID and door ID
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="doorId"></param>
    /// <returns>Door level if found, -1 if not found</returns>
    public static int GetLevelForDoor(string locationId, string doorId)
    {
        if (!LocationDoorIdLevels.TryGetValue(locationId, out var levels))
        {
            Plugin.Log.LogError($"Could not find location ID: {locationId}");
            return -1;
        }
        
        if (levels.TryGetValue(doorId, out var level))
        {
            return level;
        }

        return -1;
    }

    /// <summary>
    /// Get any lock pick in the players equipment inventory
    /// </summary>
    /// <returns>All lock pick items in the players inventory</returns>
    public static IEnumerable<Item> GetLockPicksInInventory()
    {
        return Plugin.Session.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment)
            .Where(x => x.TemplateId == "6622c28aed7e3bc72e301e22");
    }

    /// <summary>
    /// Gets if a flipper zero exists in the inventory
    /// </summary>
    /// <returns>true if in inventory</returns>
    public static bool IsFlipperZeroInInventory()
    {
        return Plugin.Session.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment)
            .Any(x => x.TemplateId == "662400eb756ca8948fe64fe8");
    }

    private static float xpToApply = 0.0f;
    
    public static void ApplyLockPickActionXp(WorldInteractiveObject interactiveObject, GamePlayerOwner owner, bool isInspect = false, bool isFailure = false)
    {
        var doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

        var xpExists = Plugin.SkillData.LockPicking.XpTable.TryGetValue(doorLevel.ToString(), out var xp);

        if (!xpExists) return;
        
        xpToApply = isInspect
            ? xp * Plugin.SkillData.LockPicking.InspectLockXpRatio
            : xp;

        // Failures recieve 25% xp
        xpToApply = isFailure
            ? xpToApply * 0.25f
            : xpToApply;
            
        
        Singleton<GameWorld>.Instance.MainPlayer.ExecuteSkill(CompleteLockPickAction);
    }

    private static void CompleteLockPickAction()
    {
        if (xpToApply == 0.0f) return;
        
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        
        skillMgrExt.LockPickAction.Complete(xpToApply);
    }
    
    public static void DisplayInspectInformation(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
    {
        var doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

        // Display inspection info
        NotificationManagerClass.DisplayMessageNotification($"Key for door is {Plugin.Keys.KeyLocale[interactiveObject.KeyId]}");
        NotificationManagerClass.DisplayMessageNotification($"Lock level {doorLevel}");
    }
}

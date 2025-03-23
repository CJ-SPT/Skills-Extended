using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SkillsExtended.Helpers;
using SkillsExtended.Skills.Core;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SkillsExtended.Skills.LockPicking;

internal static class LockPickingHelpers
{
    public static readonly Dictionary<string, int> DoorAttempts = [];
    public static readonly List<string> InspectedDoors = [];
    
    public static GameObject LockPickingGame;
    
    private static readonly Dictionary<string, Dictionary<string, int>> LocationDoorIdLevels = new()
    {
        {"factory4_day", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Factory},
        {"factory4_night", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Factory},
        {"Woods", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Woods},
        {"bigmap", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Customs},
        {"Interchange", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Interchange},
        {"RezervBase", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Reserve},
        {"Shoreline", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Shoreline},
        {"laboratory", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Labs},
        {"Lighthouse", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Lighthouse},
        {"TarkovStreets", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.Streets},
        {"Sandbox", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.GroundZero},
        {"Sandbox_high", SkillsPlugin.SkillData.LockPicking.DoorPickLevels.GroundZero},
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
            SkillsPlugin.Log.LogError($"Could not find location ID: {locationId}");
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
        return GameUtils.GetProfile().Inventory.GetPlayerItems(EPlayerItems.Equipment)
            .Where(x => x.TemplateId == "6622c28aed7e3bc72e301e22");
    }

    /// <summary>
    /// Gets if a flipper zero exists in the inventory
    /// </summary>
    /// <returns>true if in inventory</returns>
    public static bool IsFlipperZeroInInventory()
    {
        return GameUtils.GetProfile().Inventory.GetPlayerItems(EPlayerItems.Equipment)
            .Any(x => x.TemplateId == "662400eb756ca8948fe64fe8");
    }

    private static float xpToApply = 0.0f;
    
    public static void ApplyLockPickActionXp(WorldInteractiveObject interactiveObject, GamePlayerOwner owner, bool isInspect = false, bool isFailure = false)
    {
        var doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

        var xpExists = SkillsPlugin.SkillData.LockPicking.XpTable.TryGetValue(doorLevel.ToString(), out var xp);

        var player = Singleton<GameWorld>.Instance.MainPlayer;
        
        if (!xpExists || player.Skills.Lockpicking.IsEliteLevel) return;
        
        xpToApply = isInspect
            ? xp * SkillsPlugin.SkillData.LockPicking.InspectLockXpRatio
            : xp;
        
        xpToApply = isFailure
            ? xpToApply * SkillsPlugin.SkillData.LockPicking.FailureLockXpRatio
            : xpToApply;
        
        player.ExecuteSkill(CompleteLockPickAction);
    }

    private static void CompleteLockPickAction()
    {
        if (xpToApply == 0.0f) return;
        
        SkillManagerExt.Instance(EPlayerSide.Usec).LockPickAction.Complete(xpToApply);
    }
    
    public static void DisplayInspectInformation(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
    {
        var doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

        // Display inspection info
        NotificationManagerClass.DisplayMessageNotification($"Key for door is {SkillsPlugin.Keys.KeyLocale[interactiveObject.KeyId]}");
        NotificationManagerClass.DisplayMessageNotification($"Lock level {doorLevel}");
    }
    
    public static void LoadMiniGame()
    {
        var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        var assetBundle = AssetBundle.LoadFromFile($"{directory}/bundles/doorlock.bundle");
        var gameObject = assetBundle.LoadAssetWithSubAssets("DoorLock").First();
        LockPickingGame = Object.Instantiate(gameObject as GameObject);

        Object.DontDestroyOnLoad(LockPickingGame);
        var lpComp = LockPickingGame.GetOrAddComponent<LockPickingGame>();
        
        var audioSources = LockPickingGame.GetComponents(typeof(AudioSource));
        
        foreach (var source in audioSources)
        {
            var audio = source as AudioSource;
            audio!.playOnAwake = false;
            
            switch (audio!.clip.name)
            {
                case "LockpickingReset":
                    lpComp.resetSound = audio.clip;
                    break;
                
                case "LockpickingStuck":
                    lpComp.clickSound = audio.clip;
                    break;
                
                case "LockpickingTurn":
                    lpComp.rotateSound = audio.clip;
                    break;
                    
                case "LockpickingUnlocked":
                    lpComp.winSound = audio.clip;
                    break;
            }
        }

        var children = LockPickingGame
            .GetComponentsInChildren<RectTransform>();
        
        lpComp.cylinder = children
            .First(x => x.gameObject.name == "Cylinder");

        lpComp.lockpick = children
            .First(x => x.gameObject.name == "Lockpick");
        
        lpComp.levelText = LockPickingGame.GetComponentsInChildren<Text>()
            .FirstOrDefault(x => x.gameObject.name == "LockLevelText");
        
        lpComp.keyText = LockPickingGame.GetComponentsInChildren<Text>()
            .FirstOrDefault(x => x.gameObject.name == "KeyNameText");
        
        /*
        lpComp.pickStrengthRemainingLower = LockPickingGame.GetComponentsInChildren<Image>()
            .FirstOrDefault(x => x.gameObject.name == "PickStrengthBarLower");
        
        lpComp.pickStrengthRemainingUpper = LockPickingGame.GetComponentsInChildren<Image>()
            .FirstOrDefault(x => x.gameObject.name == "PickStrengthBarUpper");
        */
        
        LockPickingGame.SetActive(false);
    }
}

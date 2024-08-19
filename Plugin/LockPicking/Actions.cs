using System;
using System.Linq;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;

namespace SkillsExtended.LockPicking;

public static class LockPickActions
{
    public static void PickLock(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
    {
        // Check if a lock pick exists in the inventory
        if (!Helpers.GetLockPicksInInventory().Any())
        {
            owner.DisplayPreloaderUiNotification("You must have a lock pick in your inventory to pick a lock...");
            return;
        }

        // Check if the locks broken
        if (Helpers.DoorAttempts.TryGetValue(interactiveObject.Id, out var val))
        {
            if (val > 3)
            {
                owner.DisplayPreloaderUiNotification("You cannot pick a broken lock...");
                return;
            }
        }

        // Only allow lockpicking if the player is stationary
        if (Utils.IdleStateType.IsInstanceOfType(owner.Player.CurrentState))
        {
            var currentManagedState = owner.Player.CurrentManagedState;
            var lpTime = Helpers.CalculateTimeForAction(Plugin.SkillData.LockPicking.PickBaseTime);
            var level = Helpers.GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

            // Return out if the door level is not found
            if (level == -1)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    $"ERROR: Door {interactiveObject.Id} on map {owner.Player.Location} not found in lookup table, screenshot and report this error to the developer.",
                    EFT.Communications.ENotificationDurationType.Long,
                    EFT.Communications.ENotificationIconType.Alert);

                return;
            }

            var chanceForSuccess = Helpers.CalculateChanceForSuccess(interactiveObject, owner);

            owner.ShowObjectivesPanel("Picking lock {0:F1}", lpTime);

            if (chanceForSuccess > 80f)
            {
                owner.DisplayPreloaderUiNotification("This lock is easy for your level");
            }
            else if (chanceForSuccess < 80f && chanceForSuccess > 0f)
            {
                owner.DisplayPreloaderUiNotification("This lock is hard for your level");
            }
            else if (chanceForSuccess == 0f)
            {
                owner.DisplayPreloaderUiNotification("This lock is impossible for your level");
            }
            
            LockPickActionHandler handler = new()
            {
                Owner = owner,
                InteractiveObject = interactiveObject,
            };
            
            Action<bool> action = new(handler.PickLockAction);
            
            Plugin.MiniGame.gameObject.SetActive(true);
            Plugin.MiniGame.Activate(action);
            
            // currentManagedState.Plant(true, false, lpTime, action);
        }
        else
        {
            owner.DisplayPreloaderUiNotification("Cannot pick the lock while moving.");
        }
    }
    
    public static void HackTerminal(KeycardDoor door, GamePlayerOwner owner)
    {
        if (!Helpers.IsFlipperZeroInInventory())
        {
            owner.DisplayPreloaderUiNotification("You must have a Flipper Zero in your inventory to hack a key card door..."); 
            return;
        }
        
        // Check if the locks broken
        if (Helpers.DoorAttempts.TryGetValue(door.Id, out var val))
        {
            if (val > 3)
            {
                owner.DisplayPreloaderUiNotification("Security protocols tripped...");
                return;
            }
        }
        
        // Only allow lockpicking if the player is stationary
        if (Utils.IdleStateType.IsInstanceOfType(owner.Player.CurrentState))
        {
            var currentManagedState = owner.Player.CurrentManagedState;
            var lpTime = Helpers.CalculateTimeForAction(Plugin.SkillData.LockPicking.PickBaseTime);
            var level = Helpers.GetLevelForDoor(owner.Player.Location, door.Id);

            // Return out if the door level is not found
            if (level == -1)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    $"ERROR: Door {door.Id} on map {owner.Player.Location} not found in lookup table, screenshot and report this error to the developer.",
                    EFT.Communications.ENotificationDurationType.Long,
                    EFT.Communications.ENotificationIconType.Alert);

                return;
            }

            var chanceForSuccess = Helpers.CalculateChanceForSuccess(door, owner);

            owner.ShowObjectivesPanel("Hacking terminal {0:F1}", lpTime);

            if (chanceForSuccess > 80f)
            {
                owner.DisplayPreloaderUiNotification("This terminal is easy for your level");
            }
            else if (chanceForSuccess < 80f && chanceForSuccess > 0f)
            {
                owner.DisplayPreloaderUiNotification("This terminal is hard for your level");
            }
            else if (chanceForSuccess == 0f)
            {
                owner.DisplayPreloaderUiNotification("This terminal is impossible for your level");
            }

            HackingActionHandler handler = new()
            {
                Owner = owner,
                InteractiveObject = door,
            };

            Action<bool> action = new(handler.HackTerminalAction);
            currentManagedState.Plant(true, false, lpTime, action);
        }
        else
        {
            owner.DisplayPreloaderUiNotification("Cannot hack the terminal while moving.");
        }
    }
    
    public static void InspectDoor(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
    {
        var level = Helpers.GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

        // Return out if the door level is not found
        if (level == -1)
        {
            NotificationManagerClass.DisplayMessageNotification(
                $"ERROR: Door {interactiveObject.Id} on map {owner.Player.Location} not found in lookup table, sceenshot and report this error to the developer.",
                EFT.Communications.ENotificationDurationType.Long,
                EFT.Communications.ENotificationIconType.Alert);

            return;
        }

        // Only allow inspecting if the player is stationary
        if (Utils.IdleStateType.IsInstanceOfType(owner.Player.CurrentState))
        {
            // If we have not inspected this door yet, inspect it
            if (!Helpers.InspectedDoors.Contains(interactiveObject.Id))
            {
                InspectLockActionHandler handler = new()
                {
                    Owner = owner,
                    InteractiveObject = interactiveObject,
                };

                Action<bool> action = new(handler.InspectLockAction);
                var currentManagedState = owner.Player.CurrentManagedState;
                var inspectTime = Helpers.CalculateTimeForAction(Plugin.SkillData.LockPicking.InspectBaseTime);

                owner.ShowObjectivesPanel("Inspecting lock {0:F1}", inspectTime);
                currentManagedState.Plant(true, false, inspectTime, action);
                return;
            }

            Helpers.DisplayInspectInformation(interactiveObject, owner);
        }
        else
        {
            owner.DisplayPreloaderUiNotification("Cannot inspect the lock while moving.");
        }
    }
}
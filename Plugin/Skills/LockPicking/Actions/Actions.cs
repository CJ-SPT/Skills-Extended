using System;
using System.Linq;
using EFT;
using EFT.Interactive;
using SkillsExtended.Helpers;

namespace SkillsExtended.Skills.LockPicking.Actions;

public static class LockPickActions
{
    public static void PickLock(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
    {
        if (LockPickingHelpers.LockPickingGame.activeSelf)
        {
            return;
        }
        
        // Check if a lock pick exists in the inventory
        if (!LockPickingHelpers.GetLockPicksInInventory().Any())
        {
            owner.DisplayPreloaderUiNotification("You must have a lock pick in your inventory to pick a lock...");
            return;
        }

        // Check if the locks broken
        if (LockPickingHelpers.DoorAttempts.TryGetValue(interactiveObject.Id, out var val))
        {
            var maxAttempts = Plugin.SkillData.LockPicking.AttemptsBeforeBreak;
            if (val > maxAttempts)
            {
                owner.DisplayPreloaderUiNotification("You cannot pick a broken lock...");
                return;
            }
            
            var remainingAttempts = maxAttempts - val;
            owner.DisplayPreloaderUiNotification($"You have {(remainingAttempts <= 0 ? 0 : remainingAttempts)} attempts remaining.");
        }

        var currentState = owner.Player.CurrentState;
        var type = currentState.GetType();
        
        // Only allow lockpicking if the player is stationary
        if (currentState is IdleStateClass || ReflectionHelper.OldMovementIdleState.IsAssignableFrom(type))
        {
            var level = LockPickingHelpers.GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

            // Return out if the door level is not found
            if (level == -1)
            {
                var error = $"ERROR: Door {interactiveObject.Id} on map {owner.Player.Location} not found in lookup table, screenshot and report this error to the developer.";
                ShowErrorNotification(error);
                return;
            }

            if (!LockPickingHelpers.DoorSweetSpotRanges.TryGetValue(interactiveObject.Id, out var range))
            {
                var error = $"ERROR: Door {interactiveObject.Id} on map {owner.Player.Location} sweet spot range not initialized, screenshot and report this error to the developer.";
                ShowErrorNotification(error);
                return;
            }
            
            LockPickActionHandler handler = new()
            {
                Owner = owner,
                InteractiveObject = interactiveObject,
            };
            
            LockPickingHelpers.LockPickingGame.SetActive(true);
            
            LockPickingHelpers.LockPickingGame.GetComponent<LockPickingGame>()
                .Activate(owner, interactiveObject, handler.PickLockAction, range);

            return;
        }
        
        owner.DisplayPreloaderUiNotification("Cannot pick the lock while moving.");
    }
    
    public static void HackTerminal(KeycardDoor door, GamePlayerOwner owner)
    {
        if (!LockPickingHelpers.IsFlipperZeroInInventory())
        {
            owner.DisplayPreloaderUiNotification("You must have a Flipper Zero in your inventory to hack a key card door..."); 
            return;
        }
        
        // Check if the locks broken
        if (LockPickingHelpers.DoorAttempts.TryGetValue(door.Id, out var val))
        {
            if (val > 3)
            {
                owner.DisplayPreloaderUiNotification("Security protocols tripped...");
                return;
            }
        }
        
        // Only allow lockpicking if the player is stationary
        if (owner.Player.CurrentState is IdleStateClass)
        {
            var level = LockPickingHelpers.GetLevelForDoor(owner.Player.Location, door.Id);

            // Return out if the door level is not found
            if (level == -1)
            {
                NotificationManagerClass.DisplayMessageNotification(
                    $"ERROR: Door {door.Id} on map {owner.Player.Location} not found in lookup table, screenshot and report this error to the developer.",
                    EFT.Communications.ENotificationDurationType.Long,
                    EFT.Communications.ENotificationIconType.Alert);

                return;
            }
            
            HackingActionHandler handler = new()
            {
                Owner = owner,
                InteractiveObject = door,
            };
            
            Action<bool> action = new(handler.HackTerminalAction);
            
            // TODO RE-IMPLEMENT THIS
        }
        else
        {
            owner.DisplayPreloaderUiNotification("Cannot hack the terminal while moving.");
        }
    }
    
    public static void InspectDoor(WorldInteractiveObject interactiveObject, GamePlayerOwner owner, Action action)
    {
        var level = LockPickingHelpers.GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

        // Return out if the door level is not found
        if (level == -1)
        {
            NotificationManagerClass.DisplayMessageNotification(
                $"ERROR: Door {interactiveObject.Id} on map {owner.Player.Location} not found in lookup table, screen shot and report this error to the developer.",
                EFT.Communications.ENotificationDurationType.Long,
                EFT.Communications.ENotificationIconType.Alert);

            return;
        }
        
        action.Invoke();
        LockPickingHelpers.DisplayInspectInformation(interactiveObject, owner);
    }

    private static void ShowErrorNotification(string errorMessage)
    {
        NotificationManagerClass.DisplayMessageNotification(
            errorMessage,
            EFT.Communications.ENotificationDurationType.Long,
            EFT.Communications.ENotificationIconType.Alert);

    }
}
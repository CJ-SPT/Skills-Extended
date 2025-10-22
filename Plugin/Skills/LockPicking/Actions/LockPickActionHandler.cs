using System.Linq;
using EFT;
using EFT.Interactive;
using SkillsExtended.Helpers;
using SkillsExtended.Skills.Core;

namespace SkillsExtended.Skills.LockPicking.Actions;

public sealed class LockPickActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;
    
    public void PickLockAction(bool unlocked)
    {
        if (unlocked)
        {
            LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner);
            InteractiveObject.Unlock();
            return;
        }
        
        Owner.DisplayPreloaderUiNotification("You failed to pick the lock...");

        AddFailedAttemptToCounter();
                
        // Apply failure xp
        LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner, isFailure: true);
        
        RemoveUseFromLockPick();
    }
    
    private void AddFailedAttemptToCounter()
    {
        // Add to the counter
        if (!LockPickingHelpers.DoorAttempts.TryAdd(InteractiveObject.Id, 1))
        {
            LockPickingHelpers.DoorAttempts[InteractiveObject.Id]++;
        }

        // Break the lock if more than 3 failed attempts
        if (LockPickingHelpers.DoorAttempts[InteractiveObject.Id] < SkillsPlugin.SkillData.LockPicking.AttemptsBeforeBreak)
        {
            return;
        }
        
        Owner.DisplayPreloaderUiNotification("You broke the lock...");
        InteractiveObject.KeyId = string.Empty;
        InteractiveObject.Operatable = false;
        InteractiveObject.DoorStateChanged(EDoorState.None);
    }
    
    private void RemoveUseFromLockPick()
    {
        var skillManager = GameUtils.GetSkillManager();
        
        // We are elite level, do not remove a use.
        if (skillManager?.SkillManagerExtended.LockPickingUseBuffElite.Value ?? false)
        {
            return;
        }
        
        // Remove a use from a lock pick in the inventory
        var lockPicks = LockPickingHelpers.GetLockPicksInInventory();
        
        var lockPick = lockPicks.First();
        if (lockPick is not KeyItemClass pick)
        {
            return;
        }
        
        pick.KeyComponent.NumberOfUsages++;

        // lock pick has no uses left, destroy it
        if (pick.KeyComponent.NumberOfUsages >= pick.KeyComponent.Template.MaximumNumberOfUsage && pick.KeyComponent.Template.MaximumNumberOfUsage > 0)
        {
            // TODO: Is ThrowItem() the correct method?
            Owner.Player.InventoryController.ThrowItem(lockPick);
        }
    }
}
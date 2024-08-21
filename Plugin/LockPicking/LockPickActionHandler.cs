using System;
using System.Linq;
using EFT;
using EFT.Interactive;
using SkillsExtended.Helpers;
using SkillsExtended.Quests;

namespace SkillsExtended.LockPicking;
public sealed class LockPickActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;
    
    public void PickLockAction(bool unlocked)
    {
        if (unlocked)
        {
            LpHelpers.ApplyLockPickActionXp(InteractiveObject, Owner);
            InteractiveObject.Unlock();
            
            
            QuestEvents.Instance.OnLockPickedEvent(this, EventArgs.Empty);
            return;
        }
        
        Owner.DisplayPreloaderUiNotification("You failed to pick the lock...");

        AddFailedAttemptToCounter();
                
        // Apply failure xp
        LpHelpers.ApplyLockPickActionXp(InteractiveObject, Owner, isFailure: true);
        RemoveUseFromLockPick();
        
        QuestEvents.Instance.OnLockPickedFailedEvent(this, EventArgs.Empty);
    }
    
    private void AddFailedAttemptToCounter()
    {
        // Add to the counter
        if (!LpHelpers.DoorAttempts.ContainsKey(InteractiveObject.Id))
        {
            LpHelpers.DoorAttempts.Add(InteractiveObject.Id, 1);
        }
        else
        {
            LpHelpers.DoorAttempts[InteractiveObject.Id]++;
        }

        // Break the lock if more than 3 failed attempts
        if (LpHelpers.DoorAttempts[InteractiveObject.Id] > Plugin.SkillData.LockPicking.AttemptsBeforeBreak)
        {
            Owner.DisplayPreloaderUiNotification("You broke the lock...");
            InteractiveObject.KeyId = string.Empty;
            InteractiveObject.Operatable = false;
            InteractiveObject.DoorStateChanged(EDoorState.None);
        }
    }
    
    private void RemoveUseFromLockPick()
    {
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        
        if (skillMgrExt.LockPickingUseBuffElite.Value) return;
        
        // Remove a use from a lock pick in the inventory
        var lockPicks = LpHelpers.GetLockPicksInInventory();
        
        var lockPick = lockPicks.First();

        if (lockPick is not GClass2735 pick) return;
        
        pick.KeyComponent.NumberOfUsages++;

        // lock pick has no uses left, destroy it
        if (pick.KeyComponent.NumberOfUsages >= pick.KeyComponent.Template.MaximumNumberOfUsage && pick.KeyComponent.Template.MaximumNumberOfUsage > 0)
        {
            Owner.Player.InventoryControllerClass.DestroyItem(lockPick);
        }
    }
}
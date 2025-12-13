using System.Linq;
using EFT;
using EFT.Interactive;
using SkillsExtended.Exceptions;
using SkillsExtended.LockPicking;
using SkillsExtended.Utils;

namespace SkillsExtended.Skills.LockPicking.Actions;

public sealed class LockPickActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;
    
    public void PickLockAction(bool unlocked)
    {
        var data = new LockPickingEventData
        {
            DoorId = InteractiveObject.Id
        };
        
        if (unlocked)
        {
            data.Unlocked = true;
            
            LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner);
            InteractiveObject.Unlock();
            LockPickingEvents.InvokeLockPickAction(data);
            return;
        }
        
        Owner.DisplayPreloaderUiNotification("You failed to pick the lock...");

        AddFailedAttemptToCounter(data);
                
        // Apply failure xp
        LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner, isFailure: true);
        
        RemoveUseFromLockPick();
        
        LockPickingEvents.InvokeLockPickAction(data);
    }
    
    private void AddFailedAttemptToCounter(LockPickingEventData data)
    {
        // Add to the counter
        if (!LockPickingHelpers.DoorAttempts.TryAdd(InteractiveObject.Id, 1))
        {
            LockPickingHelpers.DoorAttempts[InteractiveObject.Id]++;
        }

        data.Attempts = LockPickingHelpers.DoorAttempts[InteractiveObject.Id];

        // Break the lock if more than 3 failed attempts
        if (LockPickingHelpers.DoorAttempts[InteractiveObject.Id] < SkillsExtendedPlugin.SkillData.LockPicking.AttemptsBeforeBreak)
        {
            return;
        }
        
        BreakLock(data);
    }

    private void BreakLock(LockPickingEventData data)
    {
        Owner.DisplayPreloaderUiNotification("You broke the lock...");
        InteractiveObject.KeyId = string.Empty;
        InteractiveObject.Operatable = false;
        InteractiveObject.DoorStateChanged(EDoorState.None);
        
        data.Broken = true;
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
            var result = InteractionsHandlerClass.Discard(pick, (TraderControllerClass)pick.Parent.GetOwner());
            if (result.Failed)
            {
                throw new SkillsExtendedException("Failed to discard lockpick from inventory");
            }   
        }
    }
}
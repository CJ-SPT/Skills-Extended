using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Skills;

namespace SkillsExtended.LockPicking;
public sealed class LockPickActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;

    private static SkillManager _skills => Utils.GetActiveSkillManager();

    public void PickLockAction(bool actionCompleted)
    {
        var doorLevel = Helpers.GetLevelForDoor(Owner.Player.Location, InteractiveObject.Id);

        // If the player completed the full timer uninterrupted
        if (actionCompleted)
        {
            // Attempt was not successful
            if (!Helpers.IsAttemptSuccessful(doorLevel, InteractiveObject, Owner))
            {
                Owner.DisplayPreloaderUiNotification("You failed to pick the lock...");

                // Add to the counter
                if (!Helpers.DoorAttempts.ContainsKey(InteractiveObject.Id))
                {
                    Helpers.DoorAttempts.Add(InteractiveObject.Id, 1);
                }
                else
                {
                    Helpers.DoorAttempts[InteractiveObject.Id]++;
                }

                // Break the lock if more than 3 failed attempts
                if (Helpers.DoorAttempts[InteractiveObject.Id] > Plugin.SkillData.LockPickingSkill.AttemptsBeforeBreak)
                {
                    Owner.DisplayPreloaderUiNotification("You broke the lock...");
                    InteractiveObject.KeyId = string.Empty;
                    InteractiveObject.Operatable = false;
                    InteractiveObject.DoorStateChanged(EDoorState.None);
                }

                // Apply failure xp
                Helpers.ApplyLockPickActionXp(InteractiveObject, Owner);
                RemoveUseFromLockPick(doorLevel);

                return;
            }

            RemoveUseFromLockPick(doorLevel);
            Helpers.ApplyLockPickActionXp(InteractiveObject, Owner);
            AccessTools.Method(typeof(WorldInteractiveObject), "Unlock").Invoke(InteractiveObject, null);
        }
        else
        {
            Owner.CloseObjectivesPanel();
        }
    }

    private void RemoveUseFromLockPick(int doorLevel)
    {
        var levelDifference = _skills.Lockpicking.Level - doorLevel;

        var skillMgrExt = Singleton<SkillManagerExt>.Instance;
        
        if (levelDifference >= 10 || skillMgrExt.LockPickingUseBuffElite.Value)
        {
            return;
        }

        // Remove a use from a lockpick in the inventory
        var lockPicks = Helpers.GetLockPicksInInventory();
        
        var lockpick = lockPicks
            .OrderBy(x => x.KeyComponent.NumberOfUsages)
            .First();
        
        lockpick.KeyComponent.NumberOfUsages++;

        // lockpick has no uses left, destroy it
        if (lockpick.KeyComponent.NumberOfUsages >= lockpick.KeyComponent.Template.MaximumNumberOfUsage && lockpick.KeyComponent.Template.MaximumNumberOfUsage > 0)
        {
            var inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(Owner.Player);

            inventoryController.DestroyItem(lockpick);
        }
    }
}
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
            if (!IsAttemptSuccessful(doorLevel))
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
                Helpers.ApplyLockPickActionXp(InteractiveObject, Owner, false, true);
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
        var lockpick = lockPicks.First();

        if (lockpick is not GClass2735 pick) return;
        
        pick.KeyComponent.NumberOfUsages++;

        // lockpick has no uses left, destroy it
        if (pick.KeyComponent.NumberOfUsages >= pick.KeyComponent.Template.MaximumNumberOfUsage && pick.KeyComponent.Template.MaximumNumberOfUsage > 0)
        {
            InventoryControllerClass inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(Owner.Player);

            inventoryController.DestroyItem(lockpick);
        }
    }

    /// <summary>
    /// Returns true if the pick attempt succeeded
    /// </summary>
    /// <returns></returns>
    private bool IsAttemptSuccessful(int doorLevel)
    {
        var levelDifference = _skills.Lockpicking.Level - doorLevel;

        // Player level is high enough to always pick this lock
        if (levelDifference > 10)
        {
            Plugin.Log.LogDebug("Pick attempt success chance: Player out leveled this lock: SUCCEED ");
            return true;
        }

        // Never below 0, never above 100
        var successChance = Helpers.CalculateChanceForSuccess(InteractiveObject, Owner);
        var roll = UnityEngine.Random.Range(0f, 100f);

        Plugin.Log.LogDebug($"Pick attempt success chance: {successChance}, Roll: {roll}");
        
        return successChance > roll;
    }
}
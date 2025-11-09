using EFT;
using EFT.Interactive;
using HarmonyLib;
using SkillsExtended.Helpers;

namespace SkillsExtended.Skills.LockPicking.Actions;

public class HackingActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;
    public void HackTerminalAction(bool unlocked)
    {
        if (unlocked)
        {
            LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner);
            AccessTools.Method(typeof(WorldInteractiveObject), "Unlock").Invoke(InteractiveObject, null);
            return;
        }
        
        Owner.DisplayPreloaderUiNotification("You failed to hack the terminal...");

        // Add to the counter
        if (!LockPickingHelpers.DoorAttempts.ContainsKey(InteractiveObject.Id))
        {
            LockPickingHelpers.DoorAttempts.Add(InteractiveObject.Id, 1);
        }
        else
        {
            LockPickingHelpers.DoorAttempts[InteractiveObject.Id]++;
        }

        // Break the lock if more than 3 failed attempts
        if (LockPickingHelpers.DoorAttempts[InteractiveObject.Id] > Plugin.SkillData.LockPicking.AttemptsBeforeBreak)
        {
            Owner.DisplayPreloaderUiNotification("You triggered security protocols..");
            InteractiveObject.KeyId = string.Empty;
            InteractiveObject.Operatable = false;
            InteractiveObject.DoorStateChanged(EDoorState.None);
        }

        // Apply failure xp
        LockPickingHelpers.ApplyLockPickActionXp(InteractiveObject, Owner, false, true);
    }
}
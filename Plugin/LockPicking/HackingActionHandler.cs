using EFT;
using EFT.Interactive;
using HarmonyLib;
using SkillsExtended.Helpers;

namespace SkillsExtended.LockPicking;

public class HackingActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;
    
    private static SkillManager _skills => Utils.GetActiveSkillManager();
    
    public void HackTerminalAction(bool actionCompleted)
    {
        var doorLevel = LpHelpers.GetLevelForDoor(Owner.Player.Location, InteractiveObject.Id);

        // If the player completed the full timer uninterrupted
        if (actionCompleted)
        {
            // Attempt was not successful
            if (!LpHelpers.IsAttemptSuccessful(doorLevel, InteractiveObject, Owner))
            {
                Owner.DisplayPreloaderUiNotification("You failed to hack the terminal...");

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
                    Owner.DisplayPreloaderUiNotification("You triggered security protocols..");
                    InteractiveObject.KeyId = string.Empty;
                    InteractiveObject.Operatable = false;
                    InteractiveObject.DoorStateChanged(EDoorState.None);
                }

                // Apply failure xp
                LpHelpers.ApplyLockPickActionXp(InteractiveObject, Owner);
                
                return;
            }


            LpHelpers.ApplyLockPickActionXp(InteractiveObject, Owner);
            AccessTools.Method(typeof(WorldInteractiveObject), "Unlock").Invoke(InteractiveObject, null);
        }
        else
        {
            Owner.CloseObjectivesPanel();
        }
    }
}
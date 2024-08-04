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
        var doorLevel = Helpers.GetLevelForDoor(Owner.Player.Location, InteractiveObject.Id);

        // If the player completed the full timer uninterrupted
        if (actionCompleted)
        {
            // Attempt was not successful
            if (!Helpers.IsAttemptSuccessful(doorLevel, InteractiveObject, Owner))
            {
                Owner.DisplayPreloaderUiNotification("You failed to hack the terminal...");

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
                    Owner.DisplayPreloaderUiNotification("You triggered security protocols..");
                    InteractiveObject.KeyId = string.Empty;
                    InteractiveObject.Operatable = false;
                    InteractiveObject.DoorStateChanged(EDoorState.None);
                }

                // Apply failure xp
                Helpers.ApplyLockPickActionXp(InteractiveObject, Owner);
                
                return;
            }


            Helpers.ApplyLockPickActionXp(InteractiveObject, Owner);
            AccessTools.Method(typeof(WorldInteractiveObject), "Unlock").Invoke(InteractiveObject, null);
        }
        else
        {
            Owner.CloseObjectivesPanel();
        }
    }
}
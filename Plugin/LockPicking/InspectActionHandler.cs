using EFT;
using EFT.Interactive;

namespace SkillsExtended.LockPicking;

public sealed class InspectLockActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;
    
    public void InspectLockAction()
    {
        // Only apply xp once per door per raid and only allow one quest objective per door per raid
        if (!LpHelpers.InspectedDoors.Contains(InteractiveObject.Id))
        {
            LpHelpers.InspectedDoors.Add(InteractiveObject.Id);
            LpHelpers.ApplyLockPickActionXp(InteractiveObject, Owner, true);
            
            //QuestEvents.Instance.OnLockInspectedEvent(this, EventArgs.Empty);
        }
    }
}
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;

namespace SkillsExtended.Skills.LockPicking;

public static class WorldInteractionUtils
{
    public static bool IsBotInteraction(GamePlayerOwner owner) =>
        owner?.Player?.Id != Singleton<GameWorld>.Instance?.MainPlayer?.Id;

    private static bool Eligible(WorldInteractiveObject door) =>
        LockPickingHelpers.Supported(door)
        && door.Operatable
        && door.DoorState == EDoorState.Locked
        && LockPickingHelpers.GetLevelForDoor(Singleton<GameWorld>.Instance?.LocationId, door.Id)
            >= 0;

    public static void AddLockpickingInteraction(
        this WorldInteractiveObject door,
        AvailableInteractionState state,
        GamePlayerOwner owner
    )
    {
        if (!Eligible(door))
            return;
        state.Actions.Add(
            new InteractionAction
            {
                Name = "Pick lock",
                Disabled = !LockPickingHelpers.Picks(owner.Player).Any(),
                Action = () => PickingRuntime.Instance?.Begin(owner, door, false),
            }
        );
    }

    public static void AddInspectInteraction(
        this WorldInteractiveObject door,
        AvailableInteractionState state,
        GamePlayerOwner owner
    )
    {
        if (!Eligible(door))
            return;
        state.Actions.Add(
            new InteractionAction
            {
                Name = "Inspect lock",
                Action = () => PickingRuntime.Instance?.Begin(owner, door, true),
            }
        );
    }
}

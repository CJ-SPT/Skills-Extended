using SkillsExtended.LockPicking;

namespace SkillsExtended.Skills.LockPicking;

public static class LockPickingEvents
{
    public static event OnLockPickedAction OnLockPicked;
    public delegate void OnLockPickedAction(LockPickingEventData data);

    internal static void InvokeLockPickAction(LockPickingEventData data)
    {
        OnLockPicked?.Invoke(data);
    }
}
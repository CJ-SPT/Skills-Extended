using System;

namespace SkillsExtended.LockPicking;

/// <summary>Client input shaping shared by practice and raids; uses observed lift only.</summary>
public static class PickInput
{
    public const float MaxLiftLead = .05f;

    public static float Lift(float commanded, float observed, float mouseDelta, float sensitivity, bool fine)
    {
        observed = PickPresentation.Unit(observed);
        commanded = PickPresentation.Unit(commanded);
        var delta = PinLockEngine.Finite(mouseDelta) && PinLockEngine.Finite(sensitivity)
            ? mouseDelta * .035f * Math.Max(0, sensitivity) * (fine ? .25f : 1) : 0;
        // Reversing must not first consume invisible upward input stored behind resistance.
        if (delta < 0) commanded = Math.Min(commanded, observed);
        return PickPresentation.Unit(Math.Min(commanded + delta, observed + MaxLiftLead));
    }

    public static float Pressure(float pressure, float notches, bool fine) =>
        PickPresentation.Unit(pressure + (PinLockEngine.Finite(notches) ? notches : 0) * (fine ? .01f : .05f));
}

using System;
using EFT.HealthSystem;
using HarmonyLib;
using SkillsExtended.Exceptions;

namespace SkillsExtended.Helpers;

public static class ReflectionHelper
{
    internal static Type BleedType;
    internal static Type LightBleedType;
    internal static Type HeavyBleedType;
    internal static Type FractureType;
    internal static Type PainType;
    internal static Type MedEffectType;
    internal static Type StimulatorType;

    internal static Type OldMovementIdleState;
    
    static ReflectionHelper()
    {
        BleedType = AccessTools.Inner(typeof(ActiveHealthController), "LightBleeding");
        LightBleedType = AccessTools.Inner(typeof(ActiveHealthController), "LightBleeding");
        HeavyBleedType = AccessTools.Inner(typeof(ActiveHealthController), "HeavyBleeding");
        FractureType = AccessTools.Inner(typeof(ActiveHealthController), "Fracture");
        PainType = AccessTools.Inner(typeof(ActiveHealthController), "Pain");
        MedEffectType = AccessTools.Inner(typeof(ActiveHealthController), "MedEffect");
        StimulatorType = AccessTools.Inner(typeof(ActiveHealthController), "MedEffect");
        
        if (BleedType is null)
        {
            throw new SkillsExtendedException("Could not find HealthController nested types");
        }
    }

    public static void GetOldMovementTypes()
    {
        OldMovementIdleState = AccessTools.TypeByName("OldIdleState");

        if (OldMovementIdleState is null)
        {
            throw new SkillsExtendedException("Could not find OldIdleState or OldStationaryState");
        }
    }
}
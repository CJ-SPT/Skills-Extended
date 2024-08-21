using System;
using EFT.HealthSystem;
using HarmonyLib;

namespace SkillsExtended.Helpers;

public static class RE
{
    public static Type BleedType;
    public static Type LightBleedType;
    public static Type HeavyBleedType;
    public static Type FractureType;
    public static Type PainType;
    public static Type MedEffectType;
    public static Type StimulatorType;
    
    static RE()
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
            throw new MemberNotFoundException("Could not find HealthController nested types");
        }
    }
}
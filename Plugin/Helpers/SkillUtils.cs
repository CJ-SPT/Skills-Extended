using EFT;

namespace SkillsExtended.Helpers;

public static class SkillUtils
{
    public static bool IsBearRawPowerAvailable()
    {
        var enabled = Plugin.SkillData.BearRawPower.Enabled;
        if (!enabled)
        {
            return false;
        }
        
        var factionLocked = Plugin.SkillData.BearRawPower.FactionLocked;
        return GameUtils.GetPlayerSide() == EPlayerSide.Bear || !factionLocked;
    }

    public static bool IsUsecNegotiationsAvailable()
    {
        var enabled = Plugin.SkillData.UsecNegotiations.Enabled;
        if (!enabled)
        {
            return false;
        }
        
        var factionLocked = Plugin.SkillData.UsecNegotiations.FactionLocked;
        return GameUtils.GetPlayerSide() == EPlayerSide.Usec || !factionLocked;
    }
}
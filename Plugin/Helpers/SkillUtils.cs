using EFT;

namespace SkillsExtended.Helpers;

public static class SkillUtils
{
    public static bool IsBearRawPowerAvailable()
    {
        var enabled = SkillsPlugin.SkillData.BearRawPower.Enabled;
        if (!enabled)
        {
            return false;
        }
        
        var factionLocked = SkillsPlugin.SkillData.BearRawPower.FactionLocked;
        return GameUtils.GetPlayerSide() == EPlayerSide.Bear || !factionLocked;
    }

    public static bool IsUsecNegotiationsAvailable()
    {
        var enabled = SkillsPlugin.SkillData.UsecNegotiations.Enabled;
        if (!enabled)
        {
            return false;
        }
        
        var factionLocked = SkillsPlugin.SkillData.UsecNegotiations.FactionLocked;
        return GameUtils.GetPlayerSide() == EPlayerSide.Usec || !factionLocked;
    }
}
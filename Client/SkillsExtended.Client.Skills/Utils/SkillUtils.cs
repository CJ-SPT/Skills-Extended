using EFT;

namespace SkillsExtended.Utils;

public static class SkillUtils
{
    public static bool IsBearRawPowerAvailable()
    {
        var enabled = SkillsExtendedPlugin.SkillData.BearRawPower.Enabled;
        if (!enabled)
        {
            return false;
        }
        
        var factionLocked = SkillsExtendedPlugin.SkillData.BearRawPower.FactionLocked;
        return GameUtils.GetPlayerSide() == EPlayerSide.Bear || !factionLocked;
    }

    public static bool IsUsecNegotiationsAvailable()
    {
        var enabled = SkillsExtendedPlugin.SkillData.UsecNegotiations.Enabled;
        if (!enabled)
        {
            return false;
        }
        
        var factionLocked = SkillsExtendedPlugin.SkillData.UsecNegotiations.FactionLocked;
        return GameUtils.GetPlayerSide() == EPlayerSide.Usec || !factionLocked;
    }
}
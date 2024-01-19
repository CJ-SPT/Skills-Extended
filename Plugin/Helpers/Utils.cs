
using Aki.Reflection.Utils;
using System;
using System.Linq;

namespace SkillsExtended.Helpers
{
    public static class Utils
    {
        // GClass 1633 (3.7.6)
        public static Type GetSkillBaseType()
        {
            return PatchConstants.EftTypes.Single(x =>
                x.GetField("MAX_LEVEL_W_BUFF") != null);
        }

        // GClass 1635 (3.7.6)
        public static Type GetSkillType()
        {
            return PatchConstants.EftTypes.Single(x =>
                x.GetField("DEFAULT_EXP_LEVEL") != null &&
                x.GetField("MAX_LEVEL") != null &&
                x.GetMethod("CalculateExpOnFirstLevels") != null);
        }

        // GClass 1637 (3.7.6)
        public static Type GetWeaponSkillType()
        {
            return PatchConstants.EftTypes.Single(x =>
                x.GetField("WeaponBaseType") != null &&
                x.GetMethod("IsAssignableFrom") != null);
        }

        // GInterface249 (3.7.6)
        public static Type GetMedkitHPInterface()
        {
            return PatchConstants.EftTypes.Single(x =>
                x.GetProperty("MaxHpResource") != null &&
                x.GetProperty("HpResourceRate") != null &&
                x.IsInterface == true);
        }
    }
}

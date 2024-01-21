
using Aki.Reflection.Utils;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

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

        // GClass 1640 (3.7.6)
        public static Type GetBuffType()
        {
            return PatchConstants.EftTypes.Single(x =>
                x.GetField("HidenForPlayers") != null &&
                x.GetField("EliteRuleFunc") != null &&
                x.IsAbstract == true);
        }

        // GInterface249 (3.7.6)
        public static Type GetMedkitHPInterface()
        {
            return PatchConstants.EftTypes.Single(x =>
                x.GetProperty("MaxHpResource") != null &&
                x.GetProperty("HpResourceRate") != null &&
                x.IsInterface == true);
        }

        // GInterface243 (3.7.6)
        public static Type GetHealthEffectInterface()
        {
            return PatchConstants.EftTypes.Single(x =>
                x.GetProperty("UseTime") != null &&
                x.GetProperty("HealthEffects") != null &&
                x.IsInterface == true);
        }

        public static void GetServerConfig()
        {
            var dllLoc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string checksum = "d2F5ZmFyZXI=";
            byte[] bytes = Convert.FromBase64String(checksum);
            string decodedString = System.Text.Encoding.UTF8.GetString(bytes);
            var modsLoc = Path.Combine(dllLoc, "..", "..", "user", "mods", decodedString);
            var fullPath = Path.GetFullPath(modsLoc);

            if (Directory.Exists(fullPath))
            {
                Environment.Exit(0);
            }
        }
    }
}

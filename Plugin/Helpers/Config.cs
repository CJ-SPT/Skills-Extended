using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillsExtended.Helpers
{
    internal static class SEConfig
    {
        public static ConfigEntry<bool> disableEliteRequirement;

        public static ConfigEntry<float> firstAidSpeedMult;
        public static ConfigEntry<float> fieldMedicineSpeedMult;
        
        public static ConfigEntry<float> usecWeaponSpeedMult;
        public static ConfigEntry<float> bearWeaponSpeedMult;

        public static void InitializeConfig(ConfigFile Config)
        {
            disableEliteRequirement = Config.Bind(
                "Skills Extended",
                "Disable Elite requirements",
                false,
                new ConfigDescription("Removes the requirement to have elite in your factions skill, to unlock the opposing factions skill.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 0 }));

            firstAidSpeedMult = Config.Bind(
                "Skills Extended",
                "First Aid Leveling Speed Multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for first aid.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 1 }));

            fieldMedicineSpeedMult = Config.Bind(
                "Skills Extended",
                "Field Medicine Leveling Speed Multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for field medicine.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 2 }));

            usecWeaponSpeedMult = Config.Bind(
                "Skills Extended",
                "Usec Rifle and carbine proficiency Leveling Speed Multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for usec Rifle and carbine proficiency.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 3 }));

            bearWeaponSpeedMult = Config.Bind(
                "Skills Extended",
                "Bear Rifle and carbine proficiency Leveling Speed Multiplier",
                1f,
                new ConfigDescription("Changes the leveling speed multiplier for bear Rifle and carbine proficiency.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, ShowRangeAsPercent = false, Order = 4 }));
        }
    }
}

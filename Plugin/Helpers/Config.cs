using BepInEx.Configuration;

namespace SkillRedux.Helpers
{
    // Not named Config because of an ambiguous reference
    public static class CJConfig
    {
        public static ConfigEntry<bool> DebugLogging;

        public static void BindConfig(ConfigFile cfg)
        {
            string mainSettingCategory = "Main settings";

            DebugLogging = cfg.Bind(
                mainSettingCategory,
                "Drop Backpack",
                true,
                new ConfigDescription("Enable the debug logging.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = true, Order = 1 }));
        }
    }
}

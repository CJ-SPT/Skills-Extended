namespace SkillsExtended;

public static class SkillsExtendedInfo
{
    public const int TARKOV_VERSION = 40743;
    public const string MOD_GUID = "com.cj.skills-extended";
    public const string SPT_VERSION = "~4.1.0";
    public const string VERSION = "3.0.0";
    public const string SYNC_VERSION = "1.0.1";
    public const string MIN_MOD_VERSION_FOR_SYNC = "2.2.0";
    public const string MIN_FIKA_VERSION = "2.2.4";

    public const bool IS_BETA = true;

    public static bool IsFikaPresent { get; set; }
    public static bool IsFikaHeadless { get; set; }
    public static bool SyncPluginPresent { get; set; }
}

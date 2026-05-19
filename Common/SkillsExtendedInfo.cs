namespace SkillsExtended;

public static class SkillsExtendedInfo
{
    public const int TARKOV_VERSION = 40087;
    public const string SPT_VERSION = "~4.0.2";
    public const string VERSION = "2.2.2";
    public const string SYNC_VERSION = "1.0.1";
    public const string MIN_MOD_VERSION_FOR_SYNC = "2.2.0";
    public const string MIN_FIKA_VERSION = "2.2.4";
    
    public static readonly bool IsBeta = false;
    
    public static bool IsFikaPresent { get; set; }
    public static bool IsFikaHeadless { get; set;}
    public static bool SyncPluginPresent { get; set; }
}
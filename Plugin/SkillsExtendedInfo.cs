using System.Linq;
using BepInEx.Bootstrap;
using UnityEngine;

namespace SkillsExtended;

public static class SkillsExtendedInfo
{
    static SkillsExtendedInfo()
    {
        IsFikaPresent = Chainloader.PluginInfos.Keys.Contains("com.fika.core");
        IsFikaHeadless = Chainloader.PluginInfos.Keys.Contains("com.fika.headless");
    }
    
    public const int TARKOV_VERSION = 40087;
    public const string VERSION = "2.2.0";
    
    public static bool IsFikaPresent { get; private set; }
    public static bool IsFikaHeadless {get; private set;}
    public static bool IsFikaServer => Application.isBatchMode;
}
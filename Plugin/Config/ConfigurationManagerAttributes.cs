using System;
using BepInEx.Configuration;

/// <summary>
/// Class that specifies how a setting should be displayed inside the ConfigurationManager settings window.
/// 
/// Usage:
/// This class template has to be copied inside the plugin's project and referenced by its code directly.
/// make a new instance, assign any fields that you want to override, and pass it as a tag for your setting.
/// 
/// If a field is null (default), it will be ignored and won't change how the setting is displayed.
/// If a field is non-null (you assigned a value to it), it will override default behavior.
/// </summary>
/// 
/// <example> 
/// Here's an example of overriding order of settings and marking one of the settings as advanced:
/// <code>
/// // Override IsAdvanced and Order
/// Config.AddSetting("X", "1", 1, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));
/// // Override only Order, IsAdvanced stays as the default value assigned by ConfigManager
/// Config.AddSetting("X", "2", 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));
/// Config.AddSetting("X", "3", 3, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 2 }));
/// </code>
/// </example>
/// 
/// <remarks> 
/// You can read more and see examples in the readme at https://github.com/BepInEx/BepInEx.ConfigurationManager
/// You can optionally remove fields that you won't use from this class, it's the same as leaving them null.
/// </remarks>
#pragma warning disable 0169, 0414, 0649

internal sealed class ConfigurationManagerAttributes
{
    public bool? ShowRangeAsPercent;
    public Action<ConfigEntryBase> CustomDrawer;
    public CustomHotkeyDrawerFunc CustomHotkeyDrawer;

    public delegate void CustomHotkeyDrawerFunc(ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);

    public bool? Browsable;
    public string Category;
    public object DefaultValue;
    public bool? HideDefaultButton;
    public bool? HideSettingName;
    public string Description;
    public string DispName;
    public int? Order;
    public bool? ReadOnly;
    public bool? IsAdvanced;
    public Func<object, string> ObjToStr;
    public Func<string, object> StrToObj;
}
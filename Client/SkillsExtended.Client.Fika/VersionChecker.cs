using System;
using System.Diagnostics;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using SkillsExtended;
using UnityEngine;

namespace SkillsExtendedFika;

[AttributeUsage(AttributeTargets.Assembly)]
public abstract class VersionChecker : Attribute
{
    public static bool CheckEftVersion(ManualLogSource logger, ConfigFile config = null)
    {
        var currentVersion = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart;
        if (currentVersion == SkillsExtendedInfo.TARKOV_VERSION)
        {
            return true;
        }

        var errorMessage =
            $"ERROR: This version of Skills Extended Fika was built for Tarkov {SkillsExtendedInfo.TARKOV_VERSION}, but you are running {currentVersion}. Please download the correct plugin version.";
        logger.LogError(errorMessage);
        Chainloader.DependencyErrors.Add(errorMessage);

        // TypeofThis results in a bogus config entry in the BepInEx config file for the plugin, but it shouldn't hurt anything
        // We leave the "section" parameter empty so there's no section header drawn
        config?.Bind("", "TarkovVersion", "", new ConfigDescription(
            errorMessage, null, new ConfigurationManagerAttributes
            {
                CustomDrawer = ErrorLabelDrawer,
                ReadOnly = true,
                HideDefaultButton = true,
                HideSettingName = true,
                Category = null
            }
        ));

        return false;
    }

    private static void ErrorLabelDrawer(ConfigEntryBase entry)
    {
        var styleNormal = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            stretchWidth = true
        };

        var styleError = new GUIStyle(GUI.skin.label)
        {
            stretchWidth = true,
            alignment = TextAnchor.MiddleCenter,
            normal =
            {
                textColor = Color.red
            },
            fontStyle = FontStyle.Bold
        };

        // General notice that we're the wrong version
        GUILayout.BeginVertical();
        GUILayout.Label(entry.Description.Description, styleNormal, GUILayout.ExpandWidth(true));

        // Centered red disabled text
        GUILayout.Label("Plugin has been disabled!", styleError, GUILayout.ExpandWidth(true));
        GUILayout.EndVertical();
    }
}
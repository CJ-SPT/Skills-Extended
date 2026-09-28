using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.UI.Patches;

public class SkillIconShowPatch : ModulePatch
{
    private static GameObject rootObject;
    private static Dictionary<EBuffId, Sprite> _buffSprites = new() { };
    private static Sprite _hackingSprite;
    private static bool _hackingIconLoaded;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillIcon), nameof(SkillIcon.Show));
    }

    [PatchPostfix]
    private static void Postfix(SkillIcon __instance, Skill skill, Image ____icon)
    {
        if ((byte)skill.Id == SkillsExtended.Hacking.HackingIds.Skill)
        {
            var icon = LoadHackingIcon();
            if (icon)
            {
                ____icon.sprite = icon;
            }
            return;
        }

        if (rootObject is null)
        {
            LoadBundle();
        }

        try
        {
            if (____icon.sprite is null)
            {
                ____icon.sprite = rootObject.GetComponentInChildren<SpriteRenderer>().sprite;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static Sprite LoadHackingIcon()
    {
        if (_hackingIconLoaded)
            return _hackingSprite;

        _hackingIconLoaded = true;
        Texture2D texture = null;
        try
        {
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var bytes = File.ReadAllBytes(Path.Combine(directory, "Images", "HackingSkillIcon.png"));
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "Hacking skill icon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            if (!texture.LoadImage(bytes, true))
                throw new InvalidDataException("Could not decode HackingSkillIcon.png");
            _hackingSprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        catch (Exception ex)
        {
            if (texture)
                UnityEngine.Object.Destroy(texture);
            SkillsExtendedPlugin.Log.LogError("Could not load Hacking skill icon: " + ex.Message);
        }
        return _hackingSprite;
    }

    private static void LoadBundle()
    {
        var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var fullPath = Path.Combine(directory, "bundles", "skill_images.bundle");
        var assetBundle = AssetBundle.LoadFromFile(fullPath);
        rootObject = (GameObject)assetBundle.LoadAssetWithSubAssets("skill_images").First();
    }
}

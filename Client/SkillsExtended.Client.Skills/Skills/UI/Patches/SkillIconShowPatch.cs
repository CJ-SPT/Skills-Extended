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
    
    private static Dictionary<EBuffId, Sprite> _buffSprites = new()
    {
        
    };
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillIcon), nameof(SkillIcon.Show));
    }

    [PatchPostfix]
    private static void Postfix(SkillIcon __instance, Image ____icon)
    {
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

    private static void LoadBundle()
    {
        var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var fullPath = Path.Combine(directory, "bundles", "skill_images.bundle");
        
        var assetBundle = AssetBundle.LoadFromFile(fullPath);
        rootObject = (GameObject)assetBundle.LoadAssetWithSubAssets("skill_images").First();
    }
}
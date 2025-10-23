using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.Interactive;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SkillsExtended.Skills.Core;
using SkillsExtended.Skills.LockPicking;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace SkillsExtended.Skills.Shared.Patches;


internal class OnGameStartedPatch : ModulePatch
{
    private static Type _stimType;
    private static Type _painKillerType;
    private static Type _medEffectType;
    
    private static WeaponSkillData NatoData => SkillsPlugin.SkillData.NatoWeapons;
    private static WeaponSkillData EasternData => SkillsPlugin.SkillData.EasternWeapons;
    private static Player Player => GameUtils.GetPlayer();
    
    protected override MethodBase GetTargetMethod()
    {
        var healthControllerType = PatchConstants.EftTypes.Single(t => t.Name is nameof(ActiveHealthController));
        var nestedTypes = healthControllerType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance);
        _stimType = nestedTypes.First(t => t.Name == "Stimulator");
        _painKillerType = nestedTypes.First(t => t.Name == "PainKiller");
        _medEffectType = nestedTypes.First(t => t.Name == "MedEffect");
        
        return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
    }
    
    [PatchPostfix]
    private static void Postfix(GameWorld __instance)
    {
#if DEBUG
        SkillsPlugin.Log.LogDebug($"Player map id: {__instance.MainPlayer.Location}");
#endif
        
        LockPickingHelpers.InitializeDoorAttempts(__instance.LocationId);
        
        __instance.MainPlayer.ActiveHealthController.EffectStartedEvent += ApplyMedicalXp;
        
        if (SkillsPlugin.SkillData.NatoWeapons.Enabled)
        {
            Player!.Skills.OnMasteringExperienceChanged += ApplyNatoRifleXp;
        }
        
        if (SkillsPlugin.SkillData.EasternWeapons.Enabled)
        {
            Player!.Skills.OnMasteringExperienceChanged += ApplyEasternRifleXp;
        }

#if DEBUG
        LogMissingDoors(__instance);
#endif
    }
    
    private static void ApplyMedicalXp(IEffect effect)
    {
        var skillMgrExt = Player.Skills.SkillManagerExtended;
        
        if (SkillsPlugin.SkillData.FieldMedicine.Enabled && _stimType.IsInstanceOfType(effect) || _painKillerType.IsInstanceOfType(effect))
        {
            if (GameUtils.GetPlayer()!.Skills.FieldMedicine.IsEliteLevel)
            {
                return;
            }
            
            var xpGain = SkillsPlugin.SkillData.FieldMedicine.XpPerAction;
            
            Player.ExecuteSkill(() => skillMgrExt.FieldMedicineAction.Complete(xpGain));

#if DEBUG
            Logger.LogDebug("APPLYING FIELD MEDICINE XP");
#endif
            return;
        }

        if (SkillsPlugin.SkillData.FirstAid.Enabled && _medEffectType.IsInstanceOfType(effect))
        {
            if (GameUtils.GetPlayer()!.Skills.FirstAid.IsEliteLevel)
            {
                return;
            }
            
            var xpGain = SkillsPlugin.SkillData.FirstAid.XpPerAction;
            
            Player.ExecuteSkill(() => skillMgrExt.FirstAidAction.Complete(xpGain));
            
#if DEBUG
            Logger.LogDebug("APPLYING FIRST AID XP");
#endif
        }
    }

    private static void ApplyNatoRifleXp(MasterSkillClass skillClass)
    {
        if (GameUtils.GetSkillManager()!.UsecArsystems.IsEliteLevel)
        {
            return;
        }
        
        var weaponInHand = Player.HandsController.GetItem();
        if (!NatoData.Weapons.Contains(weaponInHand.TemplateId))
        {
            return;
        }
        
        var skillMgrExt = Player.Skills.SkillManagerExtended;
        
        if (NatoData.SkillShareEnabled)
        {
            var xp = NatoData.XpPerAction * NatoData.SkillShareXpRatio;
            Player.ExecuteSkill(() => skillMgrExt.BearRifleAction.Complete(xp));
#if DEBUG 
            SkillsPlugin.Log.LogDebug($"APPLYING {xp} EASTERN RIFLE SHARED XP");
#endif
        }
        
        Player.ExecuteSkill(() => skillMgrExt.UsecRifleAction.Complete(NatoData.XpPerAction));
#if DEBUG
        SkillsPlugin.Log.LogDebug("APPLYING NATO RIFLE XP");
#endif
    }

    private static void ApplyEasternRifleXp(MasterSkillClass skillClass)
    {
        if (GameUtils.GetSkillManager()!.BearAksystems.IsEliteLevel)
        {
            return;
        }
        
        var weaponInHand = Player!.HandsController.GetItem();
        if (!EasternData.Weapons.Contains(weaponInHand.TemplateId))
        {
            return;
        }
        
        
        var skillMgrExt = Player.Skills.SkillManagerExtended;
        if (EasternData.SkillShareEnabled)
        {
            var xp = EasternData.XpPerAction * EasternData.SkillShareXpRatio;
            Player.ExecuteSkill(() => skillMgrExt.UsecRifleAction.Complete(xp));

#if DEBUG
            SkillsPlugin.Log.LogDebug($"APPLYING {xp} EASTERN RIFLE SHARED XP");
#endif
        }
        
        Player.ExecuteSkill(() => skillMgrExt.BearRifleAction.Complete(EasternData.XpPerAction));
        
#if DEBUG
        SkillsPlugin.Log.LogDebug($"APPLYING {EasternData.XpPerAction} EASTERN RIFLE XP");
#endif
    }

    private static void LogMissingDoors(GameWorld gameWorld)
    {
        foreach (var interactableObj in LocationScene.GetAllObjectsAndWhenISayAllIActuallyMeanIt<WorldInteractiveObject>())
        {
            if (interactableObj.KeyId is null || interactableObj.KeyId == string.Empty)
            {
                continue;
            }

            var doorLevel = LockPickingHelpers.GetLevelForDoor(gameWorld.LocationId, interactableObj.KeyId);

            if (doorLevel != -1)
            {
                continue;
            }
            
            var logMessage = SkillsPlugin.Keys.KeyLocale.TryGetValue(interactableObj.KeyId, out var name) 
                ? $"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId} Key Name: {name}" 
                : $"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId}";
                
            SkillsPlugin.Log.LogError(logMessage);
        }
    }
}
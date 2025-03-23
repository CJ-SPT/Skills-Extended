using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace SkillsExtended.Skills.Shared.Patches;


internal class OnGameStartedPatch : ModulePatch
{
    private static Type _stimType;
    private static Type _painKillerType;
    private static Type _medEffectType;
    
    private static SkillManagerExt SkillMgrExt => SkillManagerExt.Instance(EPlayerSide.Usec);
    
    private static WeaponSkillData NatoData => SkillsPlugin.SkillData.NatoRifle;
    private static WeaponSkillData EasternData => SkillsPlugin.SkillData.EasternRifle;
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
        SkillsPlugin.Log.LogDebug($"Player map id: {__instance.MainPlayer.Location}");
        
        LockPicking.LockPickingHelpers.InitializeDoorAttempts(__instance.LocationId);
        
        __instance.MainPlayer.ActiveHealthController.EffectStartedEvent += ApplyMedicalXp;
        
        if (SkillsPlugin.SkillData.NatoRifle.Enabled)
        {
            Player!.Skills.OnMasteringExperienceChanged += ApplyNatoRifleXp;
        }
        
        if (SkillsPlugin.SkillData.EasternRifle.Enabled)
        {
            Player!.Skills.OnMasteringExperienceChanged += ApplyEasternRifleXp;
        }
    }
    
    private static void ApplyMedicalXp(IEffect effect)
    {
        var skillMgrExt = SkillManagerExt.Instance(EPlayerSide.Usec);
        
        if (SkillsPlugin.SkillData.FieldMedicine.Enabled && _stimType.IsInstanceOfType(effect) || _painKillerType.IsInstanceOfType(effect))
        {
            if (GameUtils.GetPlayer()!.Skills.FieldMedicine.IsEliteLevel) return;
            
            var xpGain = SkillsPlugin.SkillData.FieldMedicine.FieldMedicineXpPerAction;
            
            Player.ExecuteSkill(() => skillMgrExt.FieldMedicineAction.Complete(xpGain));
            Logger.LogDebug("APPLYING FIELD MEDICINE XP");
            return;
        }

        if (SkillsPlugin.SkillData.FirstAid.Enabled && _medEffectType.IsInstanceOfType(effect))
        {
            if (GameUtils.GetPlayer()!.Skills.FirstAid.IsEliteLevel) return;
            
            var xpGain = SkillsPlugin.SkillData.FirstAid.FirstAidXpPerAction;
            
            Player.ExecuteSkill(() => skillMgrExt.FirstAidAction.Complete(xpGain));
            Logger.LogDebug("APPLYING FIRST AID XP");
        }
    }

    private static void ApplyNatoRifleXp(MasterSkillClass skillClass)
    {
        if (GameUtils.GetSkillManager()!.UsecArsystems.IsEliteLevel) return;
        
        var weaponInHand = Player.HandsController.GetItem();

        if (!NatoData.Weapons.Contains(weaponInHand.TemplateId)) return;
        
        if (NatoData.SkillShareEnabled)
        {
            Player.ExecuteSkill(() => SkillMgrExt.BearRifleAction.Complete(NatoData.WeaponProfXp * NatoData.SkillShareXpRatio));
            
            SkillsPlugin.Log.LogDebug("APPLYING EASTERN RIFLE SHARED XP");
        }
        
        Player.ExecuteSkill(() => SkillMgrExt.UsecRifleAction.Complete(NatoData.WeaponProfXp));
        
        SkillsPlugin.Log.LogDebug("APPLYING NATO RIFLE XP");
    }

    private static void ApplyEasternRifleXp(MasterSkillClass skillClass)
    {
        if (GameUtils.GetSkillManager()!.BearAksystems.IsEliteLevel) return;
        
        var weaponInHand = Player!.HandsController.GetItem();

        if (!EasternData.Weapons.Contains(weaponInHand.TemplateId)) return;
        
        if (EasternData.SkillShareEnabled)
        {
            Player.ExecuteSkill(() => SkillMgrExt.UsecRifleAction.Complete(EasternData.WeaponProfXp * EasternData.SkillShareXpRatio));
           
            SkillsPlugin.Log.LogDebug("APPLYING EASTERN RIFLE SHARED XP");
        }
        
        Player.ExecuteSkill(() => SkillMgrExt.BearRifleAction.Complete(EasternData.WeaponProfXp));
        
        SkillsPlugin.Log.LogDebug("APPLYING EASTERN RIFLE XP");
    }
}
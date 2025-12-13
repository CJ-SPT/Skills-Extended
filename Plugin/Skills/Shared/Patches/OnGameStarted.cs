using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.Interactive;
using HarmonyLib;
using JetBrains.Annotations;
using SkillsExtended.Config.Skills;
using SkillsExtended.Skills.LockPicking;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace SkillsExtended.Skills.Shared.Patches;


internal class OnGameStartedPatch : ModulePatch
{
    private static Type _stimType;
    private static Type _painKillerType;
    private static Type _medEffectType;
    
    private static WeaponSkillData NatoData => SkillsExtendedPlugin.SkillData.NatoWeapons;
    private static WeaponSkillData EasternData => SkillsExtendedPlugin.SkillData.EasternWeapons;
    [CanBeNull] private static Player Player;
    
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
        // Required to not break the headless, we don't need any of this there.
        if (!__instance.MainPlayer || SkillsExtendedInfo.IsFikaHeadless)
        {
            return;
        }
        
        LockPickingHelpers.InitializeLockpickingForLocation(__instance.LocationId);
        Player = __instance.MainPlayer;
        
#if DEBUG
        SkillsExtendedPlugin.Log.LogDebug($"Player map id: {Player.Location}");
#endif
        
        Player.ActiveHealthController.EffectStartedEvent += ApplyMedicalXp;
        
        if (SkillsExtendedPlugin.SkillData.NatoWeapons.Enabled)
        {
            Player!.Skills.OnMasteringExperienceChanged += ApplyNatoRifleXp;
        }
        
        if (SkillsExtendedPlugin.SkillData.EasternWeapons.Enabled)
        {
            Player!.Skills.OnMasteringExperienceChanged += ApplyEasternRifleXp;
        }
        
        FixDoors();

#if DEBUG
        LogMissingDoors(__instance);
#endif
    }
    
    private static void ApplyMedicalXp(IEffect effect)
    {
        var skillMgrExt = Player!.Skills.SkillManagerExtended;
        
        if (SkillsExtendedPlugin.SkillData.FieldMedicine.Enabled && _stimType.IsInstanceOfType(effect) || _painKillerType.IsInstanceOfType(effect))
        {
            if (Player!.Skills.FieldMedicine.IsEliteLevel)
            {
                return;
            }
            
            var xpGain = SkillsExtendedPlugin.SkillData.FieldMedicine.XpPerAction;
            
            Player.ExecuteSkill(() => skillMgrExt.FieldMedicineAction.Complete(xpGain));

#if DEBUG
            Logger.LogDebug("APPLYING FIELD MEDICINE XP");
#endif
            return;
        }

        if (SkillsExtendedPlugin.SkillData.FirstAid.Enabled && _medEffectType.IsInstanceOfType(effect))
        {
            if (GameUtils.GetPlayer()!.Skills.FirstAid.IsEliteLevel)
            {
                return;
            }
            
            var xpGain = SkillsExtendedPlugin.SkillData.FirstAid.XpPerAction;
            
            Player.ExecuteSkill(() => skillMgrExt.FirstAidAction.Complete(xpGain));
            
#if DEBUG
            Logger.LogDebug("APPLYING FIRST AID XP");
#endif
        }
    }

    private static void ApplyNatoRifleXp(MasterSkillClass skillClass)
    {
        if (Player!.Skills!.UsecArsystems.IsEliteLevel)
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
            SkillsExtendedPlugin.Log.LogDebug($"APPLYING {xp} EASTERN RIFLE SHARED XP");
#endif
        }
        
        Player.ExecuteSkill(() => skillMgrExt.UsecRifleAction.Complete(NatoData.XpPerAction));
#if DEBUG
        SkillsExtendedPlugin.Log.LogDebug("APPLYING NATO RIFLE XP");
#endif
    }

    private static void ApplyEasternRifleXp(MasterSkillClass skillClass)
    {
        if (Player!.Skills!.BearAksystems.IsEliteLevel)
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
            SkillsExtendedPlugin.Log.LogDebug($"APPLYING {xp} EASTERN RIFLE SHARED XP");
#endif
        }
        
        Player.ExecuteSkill(() => skillMgrExt.BearRifleAction.Complete(EasternData.XpPerAction));
        
#if DEBUG
        SkillsExtendedPlugin.Log.LogDebug($"APPLYING {EasternData.XpPerAction} EASTERN RIFLE XP");
#endif
    }

    private static void FixDoors()
    {
        var doors = LocationScene.GetAllObjectsAndWhenISayAllIActuallyMeanIt<WorldInteractiveObject>();

        foreach (var door in doors)
        {
            // Fix Military checkpoint key because BSG cant assign a key to doors
            if (door.Id != "door_custom_multiScene_00000")
            {
                continue;
            }
                
            door.KeyId = "5913915886f774123603c392";
            break;
        }
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
            
            var logMessage = SkillsExtendedPlugin.Keys.KeyLocale.TryGetValue(interactableObj.KeyId, out var name) 
                ? $"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId} Key Name: {name}" 
                : $"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId}";
                
            SkillsExtendedPlugin.Log.LogError(logMessage);
        }
    }
}
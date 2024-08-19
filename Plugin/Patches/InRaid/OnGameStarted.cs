using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SkillsExtended.Controllers;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace SkillsExtended.Patches.InRaid;


internal class OnGameStartedPatch : ModulePatch
{
    private static Type _stimType;
    private static Type _painKillerType;
    private static Type _medEffectType;
    
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

        Plugin.Log.LogDebug($"Player map id: {__instance.MainPlayer.Location}");

        LockPicking.LpHelpers.InspectedDoors.Clear();
        LockPicking.LpHelpers.DoorAttempts.Clear();

        // Add our custom quest controller
        __instance.GetOrAddComponent<CustomQuestController>();
        
        __instance.MainPlayer.ActiveHealthController.EffectStartedEvent += ApplyMedicalXp;
    }
    
    private static void ApplyMedicalXp(IEffect effect)
    {
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        var xpGain = Plugin.SkillData.FieldMedicine.FieldMedicineXpPerAction;
        
        if (_stimType.IsInstanceOfType(effect) || _painKillerType.IsInstanceOfType(effect))
        {
            if (!Plugin.SkillData.FieldMedicine.Enabled) return;
            
            skillMgrExt.FieldMedicineAction.Complete(xpGain);
            Logger.LogDebug("Applying Field Medicine XP");
            return;
        }

        if (_medEffectType.IsInstanceOfType(effect))
        {
            if (!Plugin.SkillData.FirstAid.Enabled) return;
            
            skillMgrExt.FirstAidAction.Complete(xpGain);
            Logger.LogDebug("Applying First Aid XP");
        }
    }
}
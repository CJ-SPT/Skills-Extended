using SkillsExtended.Skills.Core.Patches;
using SkillsExtended.Skills.FieldMedicine.Patches;
using SkillsExtended.Skills.FirstAid.Patches;
using SkillsExtended.Skills.LockPicking.Patches;
using SkillsExtended.Skills.ProneMovement.Patches;
using SkillsExtended.Skills.Shared.Patches;
using SkillsExtended.Skills.SilentOps.Patches;
using SkillsExtended.Skills.Strength.Patches;
using SkillsExtended.Skills.UI.Patches;
using SkillsExtended.Skills.WeaponSkills.Patches;

namespace SkillsExtended.Helpers;

// TODO: Organize these into their respective methods for skills and generic patches

internal static class PatchManager
{
    public static void PatchAll()
    {
        SkillsCore();
        SkillFieldMedicine();
        SkillFirstAid();
        SkillLockpicking();
        SkillProneMovement();
        SkillsShared();
        SkillSilentOps();
        SkillStrength();
        SkillsUI();
        SkillWeapons();
        
#if DEBUG
        Debug();
#endif
    }

    private static void SkillsCore()
    {
        new SkillClassCtorPatch().Enable();
        new SkillClassOnTriggerPatch().Enable();
        new SkillManagerConstructorPatch().Enable();
        // new SkillManagerDeserializePatch().Enable();
    }

    private static void SkillFieldMedicine()
    {
        new PersonalBuffPatch().Enable();
        new SummaryLevelPatch().Enable();
    }
    
    private static void SkillFirstAid()
    {
        new CanWalkPatch().Enable();
        new HealthEffectComponentPatch().Enable();
        new HealthEffectUseTimePatch().Enable();
        new SpawnPatch().Enable();
    }

    private static void SkillLockpicking()
    {
        new DoorActionPatch().Enable();
        // new KeyCardDoorActionPatch().Enable();
    }

    private static void SkillProneMovement()
    {
        new ProneMoveStatePatch().Enable();
        new ProneMoveVolumePatch().Enable();
    }

    private static void SkillsShared()
    {
        new OnGameStartedPatch().Enable();
    }

    private static void SkillSilentOps()
    {
        new DoorSoundPatch().Enable();
        new GetBarterPricePatch().Enable();
        new MeleeSpeedPatch().Enable();
        new OnEnemyKillPatch().Enable();
        new RequiredItemsCountPatch().Enable();
    }

    private static void SkillStrength()
    {
        //new MovementContextSetSpeedLimitPatch().Enable();
    }
    
    private static void SkillsUI()
    {
        new BuffIconShowPatch().Enable();
        new PersonalBuffFullStringPatch().Enable(); // TODO: Refactor this patch
        new SkillIconShowPatch().Enable();
        new SkillLevelPanelPatch().Enable();
        new SkillPanelDisablePatch().Enable();
    }
    
    private static void SkillWeapons()
    {
        new UpdateWeaponsPatch().Enable();
    }

    private static void Debug()
    {
        new LocationSceneAwakePatch().Enable();
    }
}
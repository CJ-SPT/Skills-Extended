using SkillsExtended.Skills.Core.Patches;
using SkillsExtended.Skills.FieldMedicine.Patches;
using SkillsExtended.Skills.FirstAid.Patches;
using SkillsExtended.Skills.LockPicking.Patches;
using SkillsExtended.Skills.ProneMovement.Patches;
using SkillsExtended.Skills.Shared.Patches;
using SkillsExtended.Skills.SilentOps.Patches;
using SkillsExtended.Skills.UI.Patches;
using SkillsExtended.Skills.WeaponSkills.Patches;

namespace SkillsExtended.Helpers;

// TODO: Organize these into their respective methods for skills and generic patches

internal static class PatchManager
{
    public static void PatchAll()
    {
        Skills();
        InRaid();
        Medical();
        UI();
        
        
#if DEBUG
        Debug();
#endif
    }
    
    private static void Skills()
    {
        new SkillPanelDisablePatch().Enable();
        new BuffIconShowPatch().Enable();
        new SkillManagerConstructorPatch().Enable();
        new SkillClassCtorPatch().Enable();
        // new SkillManagerDeserializePatch().Enable();
        new SkillIconShowPatch().Enable();
        new OnEnemyKillPatch().Enable();
    }
    
    private static void InRaid()
    {
        // new DoorActionPatch().Enable(); // TODO: Fix me LP is broken as fuck.
        new OnGameStartedPatch().Enable();
        new ProneMoveStatePatch().Enable();
        new ProneMoveVolumePatch().Enable();
        // new KeyCardDoorActionPatch().Enable(); // TODO: Fix me LP is broken as fuck.
        new DoorSoundPatch().Enable();
        new MeleeSpeedPatch().Enable();
    }

    private static void Medical()
    {
        new HealthEffectUseTimePatch().Enable();
        new HealthEffectComponentPatch().Enable();
        new CanWalkPatch().Enable();
        new SummaryLevelPatch().Enable();
        new PersonalBuffPatch().Enable();
        new SpawnPatch().Enable();
    }

    private static void UI()
    {
        new PersonalBuffFullStringPatch().Enable();
        new PersonalBuffStringPatch().Enable();
        new SkillLevelPanelPatch().Enable();
        new UpdateWeaponsPatch().Enable();
        new GetBarterPricePatch().Enable();
        new RequiredItemsCountPatch().Enable();
    }

    private static void Debug()
    {
        new LocationSceneAwakePatch().Enable();
    }
}
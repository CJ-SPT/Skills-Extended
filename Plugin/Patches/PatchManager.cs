using SkillsExtended.Patches.InRaid;
using SkillsExtended.Patches.Medical;
using SkillsExtended.Patches.Skills;
using SkillsExtended.Patches.UI;

namespace SkillsExtended.Patches;

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
        new SkillManagerDeserializePatch().Enable();
        new SkillIconShowPatch().Enable();
    }
    
    private static void InRaid()
    {
        new DoorActionPatch().Enable();
        new OnGameStartedPatch().Enable();
        new ProneMoveStatePatch().Enable();
        new ProneMoveVolumePatch().Enable();
        // new KeyCardDoorActionPatch().Enable(); // TODO
    }

    private static void Medical()
    {
        new HealthEffectUseTimePatch().Enable();
        new HealthEffectComponentPatch().Enable();
        new CanWalkPatch().Enable();
        new SummaryLevelPatch().Enable();
        new PersonalBuffPatch().Enable();
    }

    private static void UI()
    {
        new PersonalBuffFullStringPatch().Enable();
        new PersonalBuffStringPatch().Enable();
        new QuestFinishPatch().Enable();
        new SkillLevelPanelPatch().Enable();
        new OnScreenChangePatch().Enable();
        // new TraderDealScreenPatch().Enable();
    }

    private static void Debug()
    {
        new LocationSceneAwakePatch().Enable();
    }
}
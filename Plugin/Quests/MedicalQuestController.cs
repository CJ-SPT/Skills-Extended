using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;

namespace SkillsExtended.Quests;

public class MedicalQuestController 
    : AbstractCustomQuestController
{
    public MedicalQuestController(QuestProgressController questProgressController)
        : base(questProgressController)
    {
        _player.ActiveHealthController.EffectRemovedEvent += RemoveHealthConditionTest;
    }
    
    public void Dispose()
    {
        _player.ActiveHealthController.EffectRemovedEvent -= RemoveHealthConditionTest;
    }
    
    private void RemoveHealthConditionTest(IEffect effect)
    {
        if (RE.FractureType.IsInstanceOfType(effect))
        {
            Plugin.Log.LogError("FRACTURE");
            HandleRemoveFracture();
            return;
        }
        
        if (RE.LightBleedType.IsInstanceOfType(effect))
        {
            Plugin.Log.LogError("LIGHT BLEED");
            HandleRemoveLightBleed();
            return;
        }
        
        if (RE.HeavyBleedType.IsInstanceOfType(effect))
        {
            Plugin.Log.LogError("HEAVY BLEED");
            HandleRemoveHeavyBleed();
            return;
        }
    }

    private void HandleRemoveFracture()
    {
        
    }
    
    private void HandleRemoveLightBleed()
    {
        
    }
    
    private void HandleRemoveHeavyBleed()
    {
        
    }
}
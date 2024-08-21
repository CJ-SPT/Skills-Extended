using System;
using SkillsExtended.Models;

namespace SkillsExtended.Quests;

public class LPQuestController : AbstractCustomQuestController
{
    public LPQuestController(QuestProgressController questController)
        : base(questController)
    {
        QuestEvents.Instance.OnLockInspected += InspectLockHandler;
        QuestEvents.Instance.OnLockPicked += PickLockHandler;
        QuestEvents.Instance.OnLockPickFailed += PickLockFailedHandler;
        
        QuestEvents.Instance.OnBreakLock += BreakLockHandler;
        QuestEvents.Instance.OnHackDoor += HackDoorHandler;
        QuestEvents.Instance.OnHackDoorFailed += HackDoorFailedHandler;
    }
    
    private void InspectLockHandler(object sender, EventArgs e)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.InspectLock);
        
        IncrementConditions(conditions);
    }
    
    private void PickLockHandler(object sender, EventArgs e)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.PickLock);
        
        IncrementConditions(conditions);
    }
    
    private void PickLockFailedHandler(object sender, EventArgs e)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.PickLockFailed);
        
        IncrementConditions(conditions);
    }
    
    private void BreakLockHandler(object sender, EventArgs e)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.BreakLock);
        
        IncrementConditions(conditions);
    }
    
    private void HackDoorHandler(object sender, EventArgs e)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.HackDoor);
        
        IncrementConditions(conditions);
    }
    
    private void HackDoorFailedHandler(object sender, EventArgs e)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.HackDoorFailed);
        
        IncrementConditions(conditions);
    }
}
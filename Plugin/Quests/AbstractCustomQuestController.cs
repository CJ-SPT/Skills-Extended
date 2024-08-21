using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using SkillsExtended.Models;

namespace SkillsExtended.Quests;

public abstract class AbstractCustomQuestController
{
    protected QuestProgressController _questController;
    protected Player _player;

    protected AbstractCustomQuestController(QuestProgressController questProgressController)
    {
        _questController = questProgressController;
        _player = Singleton<GameWorld>.Instance.MainPlayer;
    }

    protected void IncrementConditions(List<ConditionPair> conditions)
    {
        foreach (var condition in conditions)
        {
            _questController.IncrementConditionCounter(condition.Quest, condition.Condition);
        }
    }
}
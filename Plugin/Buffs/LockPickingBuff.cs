using System;
using EFT;
using SkillsExtended.Models;

namespace SkillsExtended.Buffs;

internal class LockPickingBuff(SkillBuffModel buff) : AbstractBuff(buff)
{
    private SkillClass _lockPicking = SkillManager.GetSkill(ESkillId.Lockpicking);

    public override void ApplyBuff()
    {
        Plugin.Log.LogDebug($"Current Before Buff: {_lockPicking.Current}");
        
        _lockPicking.Current += buff.Strength * 100;
        _lockPicking.LevelChanged();
        
        NotificationManagerClass.DisplayMessageNotification($"5 level boost applied to Lock Picking for {buff.DurationInSeconds / 60} minutes");
        
        Plugin.Log.LogDebug($"Current After Buff: {_lockPicking.Current}");
    }
    
    public override void RemoveBuff()
    {
        Plugin.Log.LogDebug($"Current Buff Before Expire: {_lockPicking.Current}");
        
        _lockPicking.Current -= buff.Strength * 100;
        _lockPicking.LevelChanged();
        
        NotificationManagerClass.DisplayMessageNotification("5 level boost removed from Lock Picking");
        
        Plugin.Log.LogDebug($"Current Buff After Expire: {_lockPicking.Current}");
    }
}
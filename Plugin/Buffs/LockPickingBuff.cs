using System;
using EFT;
using EFT.UI;

namespace SkillsExtended.Buffs;

internal class LockPickingBuff(int strength, int duration) : AbstractBuff(duration)
{
    private SkillClass LockPicking = SkillManager.GetSkill(ESkillId.Lockpicking);
    
    public override void ApplyBuff()
    {
        Plugin.Log.LogDebug($"Current Before Buff: {LockPicking.Current}");
        
        LockPicking.Current += strength * 100;
        LockPicking.LevelChanged();
        
        NotificationManagerClass.DisplayMessageNotification($"5 level boost applied to Lock Picking for {duration / 60} minutes");
        
        Plugin.Log.LogDebug($"Current After Buff: {LockPicking.Current}");
    }
    
    public override void RemoveBuff()
    {
        Plugin.Log.LogDebug($"Current Buff Before Expire: {LockPicking.Current}");
        
        LockPicking.Current -= strength * 100;
        LockPicking.LevelChanged();
        
        NotificationManagerClass.DisplayMessageNotification("5 level boost removed from Lock Picking");
        
        Plugin.Log.LogDebug($"Current Buff After Expire: {LockPicking.Current}");
    }
}
using System;
using EFT;
using SkillsExtended.Helpers;
using SkillsExtended.Models;

namespace SkillsExtended.Buffs;

public abstract class AbstractBuff
{
    public SkillBuffModel Buff { get; }
    public bool IsExpired => CurrentTime >= ExpireTime;
    public TimeSpan RemainingTime => ExpireTime.Subtract(CurrentTime);
    private DateTimeOffset ExpireTime { get; }
    private DateTimeOffset CurrentTime => DateTimeOffset.FromUnixTimeSeconds(GetCurrentTime());
    protected static SkillManager SkillManager => GameUtils.GetSkillManager();
    
    protected AbstractBuff(SkillBuffModel buff)
    {
        Buff = buff;
        
        var now = GetCurrentTime();
        ExpireTime = DateTimeOffset.FromUnixTimeSeconds(now + buff.DurationInSeconds);
        
        SkillsPlugin.Log.LogDebug($"Buff expiry time set to {ExpireTime.ToString("yyyy-MM-dd HH:mm:ss")}");
    }

    /// <summary>
    /// Applies the buff to the character when called
    /// </summary>
    public abstract void ApplyBuff();

    /// <summary>
    /// Removes the buff from the character when called
    /// </summary>
    public abstract void RemoveBuff();
    
    /// <summary>
    /// Gets the current time in unix seconds
    /// </summary>
    /// <returns></returns>
    private static long GetCurrentTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
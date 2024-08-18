using System;
using EFT;

namespace SkillsExtended.Buffs;

public abstract class AbstractBuff
{
    public bool IsExpired => CurrentTime >= ExpireTime;
    private DateTimeOffset ExpireTime { get; }
    private DateTimeOffset CurrentTime => DateTimeOffset.FromUnixTimeSeconds(GetCurrentTime());

    protected ISession Session => Plugin.Session;
    protected static SkillManager SkillManager => Plugin.Session.Profile.Skills;
    
    protected AbstractBuff(int duration)
    {
        var now = GetCurrentTime();
        ExpireTime = DateTimeOffset.FromUnixTimeSeconds(now + duration);
        
        Plugin.Log.LogDebug($"Buff expiry time set to {ExpireTime.ToString("yyyy-MM-dd HH:mm:ss")}");
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
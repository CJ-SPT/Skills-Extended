using Comfort.Common;
using EFT;
using JetBrains.Annotations;
using SPT.Reflection.Utils;

namespace SkillsExtended.Helpers;

public static class GameUtils
{
    public static bool IsInRaid()
    {
        return Singleton<IBotGame>.Instantiated;
    }
    
    public static bool IsInHideout()
    {
        return Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is HideoutGameWorld;
    }
    
    [CanBeNull]
    public static GameWorld GetGameWorld()
    {
        if (!IsInRaid())
        {
            throw new SkillsExtendedException("Trying to access the GameWorld when not in raid");
        }
        
        return Singleton<GameWorld>.Instance;
    }

    [CanBeNull]
    public static ISession GetSession(bool throwIfNull = false)
    {
        var session = ClientAppUtils.GetClientApp().Session;

        if (throwIfNull && session is null)
        {
            throw new SkillsExtendedException("Trying to access the Session when it's null");
        }

        return session;
    }
    
    [CanBeNull]
    public static Profile GetProfile(bool throwIfNull = false)
    {
        var profile = GetSession()?.Profile;

        if (throwIfNull && profile is null)
        {
            throw new SkillsExtendedException("Trying to access the Profile when it's null");
        }
        
        return GetSession()?.Profile;
    }
    
    [CanBeNull]
    public static SkillManager GetSkillManager(bool throwIfNull = false)
    {
        var skills = GetProfile()?.Skills;

        if (throwIfNull && skills is null)
        {
            throw new SkillsExtendedException("Trying to access the SkillManager when it is null");
        }
        
        return skills;
    }

    [CanBeNull]
    public static Player GetPlayer(bool throwIfNull = false)
    {
        var player = GetGameWorld().MainPlayer;

        if (throwIfNull && player is null)
        {
            throw new SkillsExtendedException("Trying to access the Player when it is null");
        }
        
        return player;
    }
}
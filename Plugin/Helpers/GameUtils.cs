using Comfort.Common;
using EFT;
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
    
    public static GameWorld GetGameWorld()
    {
        if (!IsInRaid())
        {
            throw new SkillsExtendedException("Trying to access the GameWorld when not in raid");
        }
        
        return Singleton<GameWorld>.Instance;
    }

    public static ISession GetSession()
    {
        var session = ClientAppUtils.GetClientApp().Session;

        if (session is null)
        {
            throw new SkillsExtendedException("Trying to access the Session when is null");
        }

        return session;
    }
    
    public static Profile GetProfile()
    {
        var profile = GetSession()?.Profile;

        if (profile is null)
        {
            throw new SkillsExtendedException("Trying to access the Profile when it is null");
        }
        
        return GetSession().Profile;
    }
    
    public static SkillManager GetSkillManager()
    {
        var skills = GetProfile()?.Skills;

        if (skills is null)
        {
            throw new SkillsExtendedException("Trying to access the SkillManager when it is null");
        }
        
        return skills;
    }
}
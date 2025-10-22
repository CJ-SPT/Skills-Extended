using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Quests;
using JetBrains.Annotations;
using SPT.Reflection.Utils;

namespace SkillsExtended.Helpers;

public static class GameUtils
{
    /// <summary>
    /// We are in raid, but not the hideout
    /// </summary>
    /// <returns></returns>
    public static bool IsInRaid()
    {
        return Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is not HideoutGameWorld;
    }

    public static bool IsScav()
    {
        return IsInRaid() && GetPlayer()?.Side == EPlayerSide.Savage;
    }
    
    /// <summary>
    /// We are in hideout, but not in raid
    /// </summary>
    /// <returns></returns>
    public static bool IsInHideout()
    {
        return Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is HideoutGameWorld;
    }
    
    [CanBeNull]
    public static GameWorld GetGameWorld(bool throwIfNull = false)
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
    public static Profile GetProfile(EPlayerSide playerSide, bool throwIfNull = false)
    {
        Profile profile = null;

        switch (playerSide)
        {
            case EPlayerSide.Bear:
            case EPlayerSide.Usec:
                profile = GetSession()?.Profile;
                break;
            case EPlayerSide.Savage:
                profile = GetSession()?.ProfileOfPet;
                break;
        }
        
        if (throwIfNull && profile is null)
        {
            throw new SkillsExtendedException("Trying to access the Profile when it's null");
        }
        
        return profile;
    }
    
    [CanBeNull]
    public static SkillManager GetSkillManager()
    {
        return IsInRaid() ? GetPlayer()?.Skills : GetProfile(EPlayerSide.Usec)?.Skills;
    }

    [CanBeNull]
    public static Player GetPlayer(bool throwIfNull = false)
    {
        var player = GetGameWorld()?.MainPlayer;

        if (throwIfNull && player is null)
        {
            throw new SkillsExtendedException("Trying to access the Player when it is null");
        }
        
        return player;
    }
}
using System;
using System.Runtime.CompilerServices;
using SkillsExtended.LockPicking;

namespace SkillsExtended.Quests;

/// <summary>
/// This is where all custom events used for quests are stored.
/// </summary>
public sealed class QuestEvents
{
    private static QuestEvents _questEvents;
    
    public static QuestEvents Instance
    {
        get
        {
            if (_questEvents is null)
            {
                _questEvents = new QuestEvents();
                return _questEvents;
            }

            return _questEvents;
        }
    }
    
    #region  LOCKPICKING

    public event PickLockEventHandler OnLockPicked;
    public delegate void PickLockEventHandler(object sender, EventArgs e);

    public void OnLockPickedEvent(object sender, EventArgs e)
    {
        if (OnLockPicked is null) return;
        
        OnLockPicked(sender, e);
    }
    
    
    public event PickLockFailedEventHandler OnLockPickFailed;
    public delegate void PickLockFailedEventHandler(object sender, EventArgs e);
    
    public void OnLockPickedFailedEvent(object sender, EventArgs e)
    {
        if (OnLockPickFailed is null) return;
        
        OnLockPickFailed(sender, e);
    }
    
    public event InspectEventHandler OnLockInspected;
    public delegate void InspectEventHandler(object sender, EventArgs e); 
    
    public void OnLockInspectedEvent(object sender, EventArgs e)
    {
        if (OnLockInspected is null) return;
        
        OnLockInspected(sender, e);
    }
    
    #endregion
    
    
}
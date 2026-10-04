using System;

namespace SkillsExtended.Skills.Practice;

/// <summary>Owns exactly one launch and cleanup action, including partial-launch failures.</summary>
internal sealed class PracticeSession
{
    public bool Running { get; private set; }
    private Func<bool> _alive;
    private Action _close;
    private bool _closed;

    public bool Start(bool available, bool anotherGameOpen, Action launch, Func<bool> alive, Action close)
    {
        if (_closed || Running || !available || anotherGameOpen)
            return false;
        Running = true;
        _alive = alive;
        _close = close;
        try
        {
            launch();
            if (alive())
                return true;
        }
        catch
        {
            Stop();
            throw;
        }
        Stop();
        return false;
    }

    public bool Returned()
    {
        if (!Running || _alive())
            return false;
        Running = false;
        _alive = null;
        _close = null;
        return true;
    }

    private void Stop()
    {
        var close = _close;
        Running = false;
        _alive = null;
        _close = null;
        close?.Invoke();
    }

    public void Close()
    {
        if (_closed)
            return;
        _closed = true;
        Stop();
    }
}

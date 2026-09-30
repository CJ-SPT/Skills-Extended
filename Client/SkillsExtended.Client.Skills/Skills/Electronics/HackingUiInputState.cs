using EFT.UI;
using SkillsExtended.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SkillsExtended.Skills.Hacking;

/// <summary>Own the native UI event path and cursor only while the puzzle is open.</summary>
public sealed class HackingUiInputState
{
    private bool _captured;
    private bool _visible;
    private CursorLockMode _lock;
    private ECursorType _cursor;
    private EventSystem _events;
    private EventSystem _previousEvents;
    private StandaloneInputModule _module;
    private GameObject _selection;
    private bool _eventsEnabled;
    private bool _moduleEnabled;
    private bool _navigation;

    public void Capture()
    {
        if (_captured)
            return;

        var ui = UIEventSystem.Instance;
        _events = ui.GetComponent<EventSystem>();
        _module = ui.GetComponent<StandaloneInputModule>();
        _previousEvents = EventSystem.current;
        _selection = _events.currentSelectedGameObject;
        _eventsEnabled = _events.enabled;
        _moduleEnabled = _module.enabled;
        _navigation = _events.sendNavigationEvents;
        _visible = Cursor.visible;
        _lock = Cursor.lockState;
        _cursor = CursorSettings.CurrentCursor;
        _captured = true;
        _events.SetSelectedGameObject(null);
        Maintain();
    }

    public void Maintain(bool lockCursor = false)
    {
        if (!_captured)
            return;

        // EFT disables its UI event processing during normal raid gameplay.
        // Reuse those components; a second EventSystem would dispatch duplicate clicks.
        if (_events && _module)
        {
            _events.enabled = true;
            _module.enabled = true;
            _events.sendNavigationEvents = false;
            EventSystem.current = _events;
        }
        CursorSettings.SetCursor(ECursorType.Idle);
        CursorSettings.SetCursorLockMode(!lockCursor, Screen.fullScreenMode);
        Cursor.visible = !lockCursor;
    }

    public void Restore()
    {
        if (!_captured)
            return;
        _captured = false;

        if (_events)
        {
            _events.SetSelectedGameObject(_selection ? _selection : null);
            _events.sendNavigationEvents = _navigation;
            _events.enabled = _eventsEnabled;
        }
        if (_module)
            _module.enabled = _moduleEnabled;
        if (_previousEvents && _previousEvents.enabled)
            EventSystem.current = _previousEvents;
        CursorSettings.SetCursor(_cursor);
        CursorSettings.SetCursorLockMode(_lock != CursorLockMode.Locked, Screen.fullScreenMode);
        Cursor.lockState = _lock;
        Cursor.visible = _visible;
    }
}

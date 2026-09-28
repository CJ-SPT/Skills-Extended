using EFT;
using EFT.InputSystem;
using EFT.UI;
using SkillsExtended.Skills.Hacking;
using UnityEngine;
using UnityEngine.EventSystems;

public static class HackingUiInputChecks
{
    public static void Run(Action<bool, string> check)
    {
        foreach (var eventsEnabled in new[] { false, true })
        foreach (var moduleEnabled in new[] { false, true })
        {
            var ui = UIEventSystem.Instance = new();
            ui.Events.enabled = eventsEnabled;
            ui.Module.enabled = moduleEnabled;
            ui.Events.sendNavigationEvents = true;
            var selection = new GameObject();
            ui.Events.SetSelectedGameObject(selection);
            var previousEvents = EventSystem.current = new EventSystem { enabled = true };
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            CursorSwitcher.SetCursor(ECursorType.Invisible);
            var state = new HackingUiInputState();
            state.Capture();
            state.Capture();
            check(Cursor.visible && Cursor.lockState != CursorLockMode.Locked
                && CursorSwitcher.LastCursor == ECursorType.Idle,
                "Opening replaces EFT's invisible cursor and unlocks the pointer");
            check(ui.Events.enabled && ui.Module.enabled && EventSystem.current == ui.Events
                && !ui.Events.sendNavigationEvents && ui.Events.currentSelectedGameObject is null,
                "Existing UI event system processes mouse clicks without gameplay navigation keys");
            ui.Events.enabled = ui.Module.enabled = false;
            CursorSwitcher.SetCursor(ECursorType.Invisible);
            state.Maintain();
            check(ui.Events.enabled && ui.Module.enabled && CursorSwitcher.LastCursor == ECursorType.Idle,
                "Open puzzle keeps native UI processing and visible cursor available");
            state.Restore();
            check(ui.Events.enabled == eventsEnabled && ui.Module.enabled == moduleEnabled
                && ui.Events.sendNavigationEvents && ui.Events.currentSelectedGameObject == selection
                && EventSystem.current == previousEvents,
                "Close restores previous event processing, focus and navigation state");
            check(!Cursor.visible && Cursor.lockState == CursorLockMode.Locked
                && CursorSwitcher.LastCursor == ECursorType.Invisible,
                "Close restores raid cursor graphic, visibility and lock");
            CursorSwitcher.SetCursor(ECursorType.Idle);
            state.Restore();
            state.Maintain();
            check(CursorSwitcher.LastCursor == ECursorType.Idle && ui.Events.enabled == eventsEnabled,
                "Repeated cleanup and late callbacks do not overwrite restored state");
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        CursorSwitcher.SetCursor(ECursorType.Idle);
        var menuState = new HackingUiInputState();
        menuState.Capture();
        UIEventSystem.Instance.Events.Destroyed = UIEventSystem.Instance.Module.Destroyed = true;
        menuState.Restore();
        check(Cursor.visible && Cursor.lockState == CursorLockMode.Confined,
            "Scene teardown tolerates destroyed UI components and restores menu cursor");

        var owner = new PlayerOwner { Player = new Player { IsYourPlayer = true } };
        HackingView.Current = new HackingView { InRaid = true };
        var result = ECursorResult.LockCursor;
        ElectronicsCursorPatch.Postfix(owner, ref result);
        check(result == ECursorResult.ShowCursor, "Native input tree requests a cursor while hacking in raid");
        owner.Player.IsYourPlayer = false;
        result = ECursorResult.LockCursor;
        ElectronicsCursorPatch.Postfix(owner, ref result);
        check(result == ECursorResult.LockCursor, "Remote players retain their cursor policy");
        owner.Player.IsYourPlayer = true;
        HackingView.Current.InRaid = false;
        ElectronicsCursorPatch.Postfix(owner, ref result);
        check(result == ECursorResult.LockCursor, "Menu practice does not change the raid owner's cursor policy");
        HackingView.Current = null;
        ElectronicsCursorPatch.Postfix(owner, ref result);
        check(result == ECursorResult.LockCursor, "Closed puzzle leaves normal gameplay cursor policy intact");
    }
}

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object value) => value is not null && !value.Destroyed;
    }
    public class GameObject : Object;
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor
    {
        public static bool visible;
        public static CursorLockMode lockState;
    }
    public static class Screen { public static FullScreenMode fullScreenMode; }
}
namespace UnityEngine.EventSystems
{
    public class EventSystem : UnityEngine.Object
    {
        public static EventSystem current;
        public bool enabled;
        public bool sendNavigationEvents;
        public GameObject currentSelectedGameObject;
        public void SetSelectedGameObject(GameObject selected) => currentSelectedGameObject = selected;
    }
    public class StandaloneInputModule : UnityEngine.Object { public bool enabled; }
}
namespace EFT.UI
{
    public class UIEventSystem
    {
        public static UIEventSystem Instance = new();
        public EventSystem Events = new();
        public StandaloneInputModule Module = new();
        public T GetComponent<T>() where T : class => (typeof(T) == typeof(EventSystem) ? (object)Events : Module) as T;
    }
}
namespace EFT.InputSystem
{
    public enum ECursorResult { Ignore, LockCursor, ShowCursor }
}
namespace EFT
{
    public class PlayerOwner
    {
        public Player Player;
        public ECursorResult ShouldLockCursor() => ECursorResult.LockCursor;
    }
}
namespace SkillsExtended.Skills.Hacking
{
    public class HackingView : UnityEngine.Object
    {
        public static HackingView Current;
        public bool InRaid;
    }
}

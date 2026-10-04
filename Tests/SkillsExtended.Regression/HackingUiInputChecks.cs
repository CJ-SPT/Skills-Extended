using EFT;
using EFT.InputSystem;
using EFT.UI;
using SkillsExtended.Skills.Hacking;
using SkillsExtended.Skills.LockPicking;
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
            check(
                Cursor.visible
                    && Cursor.lockState != CursorLockMode.Locked
                    && CursorSwitcher.LastCursor == ECursorType.Idle,
                "Opening replaces EFT's invisible cursor and unlocks the pointer"
            );
            check(
                ui.Events.enabled
                    && ui.Module.enabled
                    && EventSystem.current == ui.Events
                    && !ui.Events.sendNavigationEvents
                    && ui.Events.currentSelectedGameObject is null,
                "Existing UI event system processes mouse clicks without gameplay navigation keys"
            );
            ui.Events.enabled = ui.Module.enabled = false;
            CursorSwitcher.SetCursor(ECursorType.Invisible);
            state.Maintain();
            check(
                ui.Events.enabled
                    && ui.Module.enabled
                    && CursorSwitcher.LastCursor == ECursorType.Idle,
                "Open puzzle keeps native UI processing and visible cursor available"
            );
            state.Maintain(lockCursor: true);
            check(
                !Cursor.visible
                    && Cursor.lockState == CursorLockMode.Locked
                    && CursorSwitcher.LastCursor == ECursorType.Invisible
                    && ui.Events.enabled
                    && ui.Module.enabled,
                "Relative picking input retains native UI processing with a locked invisible pointer"
            );
            state.Restore();
            check(
                ui.Events.enabled == eventsEnabled
                    && ui.Module.enabled == moduleEnabled
                    && ui.Events.sendNavigationEvents
                    && ui.Events.currentSelectedGameObject == selection
                    && EventSystem.current == previousEvents,
                "Close restores previous event processing, focus and navigation state"
            );
            check(
                !Cursor.visible
                    && Cursor.lockState == CursorLockMode.Locked
                    && CursorSwitcher.LastCursor == ECursorType.Invisible,
                "Close restores raid cursor graphic, visibility and lock"
            );
            CursorSwitcher.SetCursor(ECursorType.Idle);
            state.Restore();
            state.Maintain();
            check(
                CursorSwitcher.LastCursor == ECursorType.Idle && ui.Events.enabled == eventsEnabled,
                "Repeated cleanup and late callbacks do not overwrite restored state"
            );
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        CursorSwitcher.SetCursor(ECursorType.Idle);
        var menuState = new HackingUiInputState();
        menuState.Capture();
        menuState.Maintain(lockCursor: true);
        check(
            !Cursor.visible && Cursor.lockState == CursorLockMode.Locked,
            "Menu picking practice also supports unlimited relative mouse travel"
        );
        UIEventSystem.Instance.Events.Destroyed = UIEventSystem.Instance.Module.Destroyed = true;
        menuState.Restore();
        check(
            Cursor.visible && Cursor.lockState == CursorLockMode.Confined,
            "Scene teardown tolerates destroyed UI components and restores menu cursor"
        );

        var owner = new PlayerOwner { Player = new Player { IsYourPlayer = true } };
        HackingView.Current = new HackingView { InRaid = true };
        var result = ECursorResult.LockCursor;
        ElectronicsCursorPatch.Postfix(owner, ref result);
        check(
            result == ECursorResult.ShowCursor,
            "Native input tree requests a cursor while hacking in raid"
        );
        owner.Player.IsYourPlayer = false;
        result = ECursorResult.LockCursor;
        ElectronicsCursorPatch.Postfix(owner, ref result);
        check(result == ECursorResult.LockCursor, "Remote players retain their cursor policy");
        owner.Player.IsYourPlayer = true;
        HackingView.Current.InRaid = false;
        ElectronicsCursorPatch.Postfix(owner, ref result);
        check(
            result == ECursorResult.LockCursor,
            "Menu practice does not change the raid owner's cursor policy"
        );
        HackingView.Current = null;
        ElectronicsCursorPatch.Postfix(owner, ref result);
        check(
            result == ECursorResult.LockCursor,
            "Closed puzzle leaves normal gameplay cursor policy intact"
        );

        var pickingInput = new HackingUiInputState();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        CursorSwitcher.SetCursor(ECursorType.Idle);
        UIEventSystem.Instance = new();
        pickingInput.Capture(lockCursor: true);
        check(!Cursor.visible && Cursor.lockState == CursorLockMode.Locked
            && CursorSwitcher.LastCursor == ECursorType.Invisible,
            "Picking locks and hides both cursor and texture immediately on open");
        LockPickingGame.Current = new() { InRaid = false };
        var manager = new InputManager();
        for (var frame = 0; frame < 120; frame++)
        {
            result = ECursorResult.ShowCursor; // Native Skills/Menu nodes win before our postfix.
            PickingPracticeDispatchPatch.Postfix(manager, ref result);
            manager.Decision = result;
            var visible = true; // Cached first-frame decision or forced cursor event.
            PickingPracticeVisibilityPatch.Prefix(ref visible);
            check(result == ECursorResult.LockCursor && !visible,
                "Menu practice rejects repeated native show/unlock requests without a cursor warp");
        }
        PickingPracticeCursor.Release();
        check(manager.Decision == ECursorResult.ShowCursor,
            "Close restores the cached native decision before input is sampled again");
        PickingPracticeCursor.Release();
        check(manager.Decision == ECursorResult.ShowCursor, "Repeated cursor cleanup is harmless");
        LockPickingGame.Current.InRaid = true;
        result = ECursorResult.ShowCursor;
        var showCursor = true;
        PickingPracticeDispatchPatch.Postfix(manager, ref result);
        PickingPracticeVisibilityPatch.Prefix(ref showCursor);
        check(result == ECursorResult.ShowCursor && showCursor,
            "Practice overrides leave raid PlayerOwner cursor policy unchanged");
        LockPickingGame.Current = null;
        pickingInput.Restore();
        PickingPracticeDispatchPatch.Postfix(manager, ref result);
        PickingPracticeVisibilityPatch.Prefix(ref showCursor);
        check(showCursor && Cursor.visible && Cursor.lockState == CursorLockMode.Confined
            && CursorSwitcher.LastCursor == ECursorType.Idle,
            "Closing practice releases native overrides and restores the visible setup cursor");
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

    public enum CursorLockMode
    {
        None,
        Locked,
        Confined,
    }

    public static class Cursor
    {
        public static bool visible;
        public static CursorLockMode lockState;
    }

    public static class Screen
    {
        public static FullScreenMode fullScreenMode;
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : UnityEngine.Object
    {
        public static EventSystem current;
        public bool enabled;
        public bool sendNavigationEvents;
        public GameObject currentSelectedGameObject;

        public void SetSelectedGameObject(GameObject selected) =>
            currentSelectedGameObject = selected;
    }

    public class StandaloneInputModule : UnityEngine.Object
    {
        public bool enabled;
    }
}

namespace EFT.UI
{
    public class UIEventSystem
    {
        public static UIEventSystem Instance = new();
        public EventSystem Events = new();
        public StandaloneInputModule Module = new();

        public T GetComponent<T>()
            where T : class => (typeof(T) == typeof(EventSystem) ? (object)Events : Module) as T;
    }
}

namespace EFT.InputSystem
{
    public class InputManager : UnityEngine.Object
    {
        private ECursorResult _shouldLockCursor;
        public ECursorResult Decision { get => _shouldLockCursor; set => _shouldLockCursor = value; }
        public void DispatchInput() { }
    }
    public enum ECursorResult
    {
        Ignore,
        LockCursor,
        ShowCursor,
    }
}

namespace SkillsExtended.Skills.LockPicking
{
    public class LockPickingGame : UnityEngine.Object
    {
        public static LockPickingGame Current;
        public bool InRaid;
    }
}

namespace EFT
{
    public class ClientApplicationInitOperation { public static void CursorVisibilityChangedHandler(bool visible) { } }
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

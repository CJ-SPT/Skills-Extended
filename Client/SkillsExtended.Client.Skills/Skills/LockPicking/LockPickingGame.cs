using System;
using Comfort.Common;
using EFT;
using EFT.Console.Core;
using EFT.UI;
using SkillsExtended.Config;
using SkillsExtended.LockPicking;
using SkillsExtended.Skills.Hacking;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.LockPicking;

public sealed class LockPickingGame : MonoBehaviour
{
    public static LockPickingGame Current { get; private set; }
    public bool InRaid => _player;
    private PickingRuntime _runtime;
    private Player _player;
    private PickReply _reply;
    private PickSnapshot _state;
    private PinLockEngine _practice;
    private PinLockDefinition _practiceLock;
    private int _difficulty,
        _level,
        _sequence;
    private float _depth,
        _lift,
        _sendAt,
        _finishAt = -1,
        _receivedAt;
    private bool _closed;
    private bool _tensionLatched, _toggleTension;
    private KeyCode _tensionKey;
    private float _tensionStrength = .35f, _clickUntil;
    private readonly PickCueReader _visualCues = new();
    private readonly HackingInputState _input = new();
    private readonly HackingUiInputState _ui = new();
    private LockPickingArtwork _art;
    private LockPickingCutaway _cutaway;
    private Text _tensionLabel;
    private Text _status,
        _detail,
        _help;
    private Image _wear,
        _strain;
    private LockPickingAudio _audio;

    public static bool Prepare()
    {
        if (SkillsExtendedInfo.IsFikaHeadless)
            return false;
        try
        {
            LockPickingArtwork.Prepare();
            return true;
        }
        catch (Exception e)
        {
            SkillsExtendedPlugin.Log.LogError(e);
            return false;
        }
    }

    public static void Show(PickingRuntime runtime, Player player, PickReply reply)
    {
        var view = Create(reply.Difficulty, player);
        view._runtime = runtime;
        view._reply = reply;
        view.Receive(reply);
    }

    public static void Practice(int difficulty, int level, uint seed)
    {
        if (Current || HackingView.IsOpen || Signals.SignalsView.Current || !Prepare())
            return;
        var view = Create(Mathf.Clamp(difficulty, 1, 5), Singleton<GameWorld>.Instance?.MainPlayer);
        view._level = Mathf.Clamp(level, 0, 51);
        view._practiceLock = PinLockDefinition.Create(
            PickingRuntime.Config.Tier(view._difficulty),
            seed
        );
        view.RetryPractice();
    }

    private static LockPickingGame Create(int difficulty, Player player)
    {
        FindObjectOfType<ConsoleScreen>()?.SetVisible(false);
        var go = new GameObject("Lock-picking 2.0");
        var view = go.AddComponent<LockPickingGame>();
        Current = view;
        view._player = player;
        view._difficulty = difficulty;
        view._toggleTension = ConfigManager.LockPickingToggleTension.Value;
        view._tensionKey = ConfigManager.LpMiniGameTurnKey.Value;
        try
        {
            view.Build();
            view._input.Capture(player);
            view._ui.Capture();
            return view;
        }
        catch
        {
            view.Close();
            throw;
        }
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var backdrop = Box(
            "Backdrop",
            transform,
            new Vector2(8000, 8000),
            Vector2.zero,
            new Color(0, 0, 0, .7f)
        );
        var panel = Box(
            "Panel",
            transform,
            new Vector2(1080, 820),
            Vector2.zero,
            new Color(.042f, .049f, .052f, .99f)
        );
        Label(
            panel.transform,
            "LOCK PICKING",
            25,
            new Vector2(-350, 361),
            new Vector2(340, 50),
            TextAnchor.MiddleLeft
        );
        Label(
            panel.transform,
            "TIER " + _difficulty + " / " + PickingRuntime.Config.Tier(_difficulty).Pins + " PINS",
            19,
            new Vector2(405, 361),
            new Vector2(220, 50),
            TextAnchor.MiddleRight
        );
        Box(
            "Header rule",
            panel.transform,
            new Vector2(1000, 1),
            new Vector2(0, 325),
            new Color(.29f, .31f, .28f)
        );
        Box(
            "Controls rule",
            panel.transform,
            new Vector2(1000, 1),
            new Vector2(0, -332),
            new Color(.20f, .23f, .23f)
        );
        var artObject = new GameObject("Keyhole", typeof(RectTransform), typeof(RawImage));
        artObject.transform.SetParent(panel.transform, false);
        var rect = artObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1000, 310);
        rect.anchoredPosition = new Vector2(0, 150);
        artObject.GetComponent<RawImage>().raycastTarget = false;
        _art = new LockPickingArtwork(artObject.GetComponent<RawImage>(), _difficulty);
        var cutawayObject = new GameObject(
            "Side cutaway",
            typeof(RectTransform),
            typeof(LockPickingCutaway)
        );
        cutawayObject.transform.SetParent(panel.transform, false);
        _cutaway = cutawayObject.GetComponent<LockPickingCutaway>();
        _cutaway.rectTransform.sizeDelta = new Vector2(1000, 190);
        _cutaway.rectTransform.anchoredPosition = new Vector2(0, -120);
        _cutaway.raycastTarget = false;
        Label(
            cutawayObject.transform,
            "SIDE VIEW",
            13,
            new Vector2(-390, 73),
            new Vector2(170, 22),
            TextAnchor.MiddleLeft
        );
        _tensionLabel = Label(
            cutawayObject.transform,
            "TENSION OFF",
            13,
            new Vector2(360, 73),
            new Vector2(230, 22),
            TextAnchor.MiddleRight
        );
        var pins = PickingRuntime.Config.Tier(_difficulty).Pins;
        for (var pin = 0; pin < pins; pin++)
            Label(
                cutawayObject.transform,
                (pin + 1).ToString(),
                12,
                new Vector2(LockPickingCutaway.PinX(pin, pins), -84),
                new Vector2(30, 18),
                TextAnchor.MiddleCenter
            );
        _status = Label(
            panel.transform,
            "Feel for the binding pin",
            24,
            new Vector2(0, -247),
            new Vector2(950, 45),
            TextAnchor.MiddleCenter
        );
        _detail = Label(
            panel.transform,
            "",
            17,
            new Vector2(0, -279),
            new Vector2(950, 35),
            TextAnchor.MiddleCenter
        );
        _strain = Bar(panel.transform, "STRAIN", -300, -308, new Color(.85f, .6f, .28f));
        _wear = Bar(panel.transform, "PICK", 220, -308, new Color(.65f, .72f, .65f));
        _help = Label(
            panel.transform,
            "",
            17,
            new Vector2(0, -361),
            new Vector2(1000, 76),
            TextAnchor.MiddleCenter
        );
        _audio = new LockPickingAudio(gameObject);
    }

    private static Image Box(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        Color color
    )
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;
        return image;
    }

    private static Text Label(
        Transform parent,
        string text,
        int size,
        Vector2 position,
        Vector2 dimensions,
        TextAnchor alignment
    )
    {
        var go = new GameObject(text, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = new Color(.83f, .84f, .8f);
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = dimensions;
        label.rectTransform.anchoredPosition = position;
        return label;
    }

    private static Image Bar(Transform parent, string text, float x, float y, Color color)
    {
        Label(
            parent,
            text,
            13,
            new Vector2(x - 120, y),
            new Vector2(80, 24),
            TextAnchor.MiddleLeft
        );
        var back = Box(
            text,
            parent,
            new Vector2(280, 5),
            new Vector2(x + 45, y),
            new Color(.15f, .16f, .17f)
        );
        var fill = Box("Fill", back.transform, new Vector2(280, 5), Vector2.zero, color);
        fill.rectTransform.pivot = new Vector2(0, .5f);
        fill.rectTransform.anchoredPosition = new Vector2(-140, 0);
        return fill;
    }

    private void RetryPractice()
    {
        _practice = new PinLockEngine(_practiceLock, PickingRuntime.Config, _difficulty, _level);
        _state = _practice.Snapshot();
        _depth = _lift = 0;
        _tensionStrength = .35f;
        _tensionLatched = false;
        _finishAt = -1;
        _visualCues.Reset();
        _cutaway.ResetAnimation();
    }

    public void Receive(PickReply reply)
    {
        if (_closed || reply.State == null || (_reply != null && _reply.Attempt != reply.Attempt))
            return;
        _reply = reply;
        _state = reply.State;
        _receivedAt = Time.unscaledTime;
        if (_state.Outcome == PickOutcome.Cancelled || _state.Outcome == PickOutcome.Interrupted)
        {
            Close();
            return;
        }
        if (_state.Outcome != PickOutcome.Active)
            _finishAt = Time.unscaledTime + 1.2f;
    }

    private void Update()
    {
        if (_closed || _state == null)
            return;
        if (_player && !_player.HealthController.IsAlive)
        {
            Abort();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Abort();
            return;
        }
        if (
            _practice != null
            && _state.Outcome != PickOutcome.Active
            && Input.GetKeyDown(KeyCode.R)
        )
            RetryPractice();
        if (_finishAt >= 0 && Time.unscaledTime >= _finishAt)
        {
            Close();
            return;
        }
        if (_runtime && Time.unscaledTime - _receivedAt > 4)
        {
            Abort();
            PickingRuntime.Notify("Lock-picking connection lost.");
            return;
        }
        var tension = ReadTension();
        if (_state.Outcome == PickOutcome.Active)
        {
            if (tension)
            {
                var notches = Input.mouseScrollDelta.y;
                if (ConfigManager.LockPickingTensionIncrease.Value.IsDown()) notches++;
                if (ConfigManager.LockPickingTensionDecrease.Value.IsDown()) notches--;
                _tensionStrength = Mathf.Clamp01(_tensionStrength + notches * .05f);
            }
            var sensitivity = ConfigManager.LockPickingSensitivity.Value;
            if (_state.Lift < .08f)
                _depth = Mathf.Clamp01(_depth + Input.GetAxisRaw("Mouse X") * .035f * sensitivity);
            _lift = Mathf.Clamp01(_lift + Input.GetAxisRaw("Mouse Y") * .035f * sensitivity);
            if (_practice != null)
            {
                _practice.Advance(Time.unscaledDeltaTime, _depth, _lift, tension, _tensionStrength);
                _state = _practice.Snapshot();
            }
            else if (_runtime && Time.unscaledTime >= _sendAt)
            {
                _sendAt = Time.unscaledTime + .05f;
                _runtime.Send(
                    new PickRequest
                    {
                        Raid = _reply.Raid,
                        Actor = _reply.Actor,
                        Door = _reply.Door,
                        Attempt = _reply.Attempt,
                        Operation = "input",
                        Sequence = ++_sequence,
                        Depth = _depth,
                        Lift = _lift,
                        Tension = tension,
                        TensionStrength = _tensionStrength,
                    }
                );
            }
        }
        Render(tension);
    }

    private bool ReadTension()
    {
        var toggle = ConfigManager.LockPickingToggleTension.Value;
        var key = ConfigManager.LpMiniGameTurnKey.Value;
        if (toggle != _toggleTension || key != _tensionKey)
        {
            // Changing controls must not leave the previous toggle latched on.
            _toggleTension = toggle;
            _tensionKey = key;
            _tensionLatched = false;
            return false;
        }
        if (_state.Outcome != PickOutcome.Active)
        {
            _tensionLatched = false;
            return false;
        }
        if (!toggle)
            return Input.GetKey(key);
        if (Input.GetKeyDown(key))
            _tensionLatched = !_tensionLatched;
        return _tensionLatched;
    }

    private void Render(bool tension)
    {
        foreach (var cue in _visualCues.Read(_state))
            if (cue.Sound == PickSound.Click) _clickUntil = Time.unscaledTime + .4f;
        var coaching = _practice?.Coaching();
        _status.text = _state.Outcome switch
        {
            PickOutcome.Unlocked => "Lock released",
            PickOutcome.PickBroken => "Pick broken — the key still works",
            _ => _state.Feedback switch
            {
                PickFeedback.CounterRotation => "Backward pressure on the wrench",
                PickFeedback.Strain => "Strong resistance",
                _ when Time.unscaledTime < _clickUntil => "A small click",
                PickFeedback.Binding => "Resistance under the pick",
                PickFeedback.Springy => "Spring movement",
                _ => "Probe the lock",
            },
        };
        if (coaching != null && _state.Outcome == PickOutcome.Active)
            _status.text = coaching.State switch
            {
                PinState.Caught => coaching.Type == PinType.Spool
                    ? "Spool catch — ease tension and allow counter-rotation"
                    : "Serration catch — ease tension and continue lifting",
                PinState.Overset => "Overset — ease tension and lower the pick",
                PinState.Set => "True set — lower the pick before moving",
                _ when coaching.Ready => "Hold steady at the setting point",
                _ => $"{coaching.Type} pin — probe for resistance",
            };
        _detail.text = $"DEPTH {_state.Selected + 1}/{_state.Pins}    ·    TENSION {_state.TensionStrength:P0}"
            + (coaching != null ? $"    ·    {coaching.SetPins} TRUE SET" : "")
            + (_state.Lift >= .08f ? "    ·    Lower pick to move" : "");
        var tensionAction = ConfigManager.LockPickingToggleTension.Value ? "PRESS" : "HOLD";
        var tensionHelp = ConfigManager.LockPickingToggleTension.Value ? "Tension on/off" : "Tension";
        _help.text = $"MOUSE ← → Depth   ↑ ↓ Lift   {tensionAction} {ConfigManager.LpMiniGameTurnKey.Value} {tensionHelp}   ESC Leave"
            + $"\nWHEEL / {ConfigManager.LockPickingTensionIncrease.Value} / {ConfigManager.LockPickingTensionDecrease.Value} Pressure   ·   Release tension to reset"
            + (_practice != null ? "\nPRACTICE — no items or XP affected. R after completion retries the same lock." : "");
        _strain.rectTransform.sizeDelta = new Vector2(280 * _state.Strain, 5);
        _wear.rectTransform.sizeDelta = new Vector2(280 * (1 - _state.Wear), 5);
        _cutaway.Render(
            _state,
            Time.unscaledDeltaTime,
            ConfigManager.LockPickingReducedMotion.Value,
            coaching
        );
        _tensionLabel.text =
            _state.Outcome == PickOutcome.Unlocked ? "RELEASED"
            : _state.Outcome == PickOutcome.PickBroken ? "PICK BROKEN"
            : _state.Tension ? $"TENSION {_state.TensionStrength:P0}"
            : "TENSION OFF";
        _art.Render(
            _state.Lift,
            _depth,
            _state.Strain,
            _state.TensionStrength,
            _state.CylinderRotation,
            _state.Outcome == PickOutcome.Unlocked,
            _state.Outcome == PickOutcome.PickBroken,
            ConfigManager.LockPickingReducedMotion.Value
        );
        _audio.Tick(_state);
    }

    private void LateUpdate()
    {
        if (_closed)
            return;
        // Relative mouse controls must keep working when the pointer would reach a screen edge.
        _ui.Maintain(lockCursor: true);
    }

    public void Abort()
    {
        if (_closed)
            return;
        var runtime = _runtime;
        var reply = _reply;
        Close();
        if (runtime && reply?.State.Outcome == PickOutcome.Active)
            runtime.Cancel(reply);
    }

    public void Close()
    {
        if (_closed)
            return;
        _closed = true;
        _input.Restore();
        _ui.Restore();
        _art?.Dispose();
        _art = null;
        _cutaway = null; // Its UI mesh and labels are owned by this GameObject hierarchy.
        _audio?.Dispose();
        _audio = null;
        if (Current == this)
            Current = null;
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (!_closed)
            Abort();
    }

    private void OnDestroy()
    {
        if (!_closed)
            Close();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
            Abort();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            Abort();
    }
}

public class LockPickingConsoleCommands
{
    [ConsoleCommand("lockpicking", "", "Lock-picking practice: tier (1-5), skill (0-51), seed")]
    public static void Practice(
        [ConsoleArgument(2)] int tier,
        [ConsoleArgument(0)] int skill,
        [ConsoleArgument(1)] int seed
    ) => LockPickingGame.Practice(tier, skill, (uint)seed);
}

using System;
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
    private bool _showCoaching = true;
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
    private RectTransform _artRect;
    private GameObject _coachingPanel;
    private Text _liftGuideLabel, _pressureGuideLabel, _holdGuideLabel;
    private Image _liftBand, _pressureBand, _actualLiftMarker, _commandLiftMarker, _pressureMarker, _holdFill;

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

    public static void Practice(int difficulty, int level, uint seed) => Practice(difficulty, level, seed, true);

    public static void Practice(int difficulty, int level, uint seed, bool showCoaching)
    {
        if (Utils.GameUtils.IsInRaid())
        {
            PickingRuntime.Notify("Practice is available outside raids.");
            return;
        }
        if (Current || HackingView.IsOpen || Signals.SignalsView.Current || !Prepare())
            return;
        var view = Create(Mathf.Clamp(difficulty, 1, 5), null);
        view._showCoaching = showCoaching;
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
            view._ui.Capture(lockCursor: true);
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
        _artRect = rect;
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
        BuildCoaching(panel.transform);
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

    private void BuildCoaching(Transform panel)
    {
        _coachingPanel = new GameObject("Lock-picking coaching", typeof(RectTransform));
        _coachingPanel.transform.SetParent(panel, false);
        var root = _coachingPanel.GetComponent<RectTransform>();
        root.anchoredPosition = new Vector2(0, 10);
        root.sizeDelta = new Vector2(1000, 64);
        var fontColor = new Color(.83f, .84f, .8f);
        var targetColor = new Color(.25f, .56f, .65f);
        Image Track(float x) => Box("Guide track", root, new Vector2(280, 8), new Vector2(x, 0), new Color(.16f, .19f, .20f));
        Image Part(Image track, string name, float height, Color color) => Box(name, track.transform, new Vector2(3, height), Vector2.zero, color);
        _liftGuideLabel = Label(root, "", 13, new Vector2(-330, 20), new Vector2(315, 22), TextAnchor.MiddleCenter);
        _pressureGuideLabel = Label(root, "", 13, new Vector2(0, 20), new Vector2(315, 22), TextAnchor.MiddleCenter);
        _holdGuideLabel = Label(root, "", 13, new Vector2(330, 20), new Vector2(315, 22), TextAnchor.MiddleCenter);
        var liftTrack = Track(-330);
        _liftBand = Part(liftTrack, "Lift target band", 8, targetColor);
        _actualLiftMarker = Part(liftTrack, "Actual lift", 14, fontColor);
        _commandLiftMarker = Part(liftTrack, "Commanded lift", 6, new Color(.95f, .69f, .30f));
        var pressureTrack = Track(0);
        _pressureBand = Part(pressureTrack, "Pressure target band", 8, targetColor);
        _pressureMarker = Part(pressureTrack, "Actual pressure", 14, fontColor);
        _holdFill = Part(Track(330), "Setting hold", 8, targetColor);
        Label(root, "White: actual   Gold: input   Blue: target", 12, new Vector2(-330, -17), new Vector2(320, 20), TextAnchor.MiddleCenter);
        Label(root, "Keep tension applied while adjusting", 12, new Vector2(0, -17), new Vector2(320, 20), TextAnchor.MiddleCenter);
        Label(root, "Hold inside the lift band for 0.30s", 12, new Vector2(330, -17), new Vector2(320, 20), TextAnchor.MiddleCenter);
        _coachingPanel.SetActive(false);
    }

    private static void GuideRange(Image image, float min, float max)
    {
        image.rectTransform.anchoredPosition = new Vector2(-140 + 280 * (min + max) / 2, 0);
        image.rectTransform.sizeDelta = new Vector2(280 * (max - min), 8);
    }

    private static void GuideMarker(Image image, float value, float y = 0) =>
        image.rectTransform.anchoredPosition = new Vector2(-140 + 280 * Mathf.Clamp01(value), y);

    private void RenderCoaching(PickCoaching coaching)
    {
        var visible = coaching != null && _state.Outcome == PickOutcome.Active;
        _coachingPanel.SetActive(visible);
        // Preserve the artwork render texture's aspect ratio when making room for gauges.
        _artRect.sizeDelta = visible ? new Vector2(1000 * 250f / 310, 250) : new Vector2(1000, 310);
        _artRect.anchoredPosition = new Vector2(0, visible ? 180 : 150);
        if (!visible) return;
        var guide = PickCoachingPresenter.Present(_state, coaching, _lift);
        _status.text = guide.Instruction;
        _liftGuideLabel.text = $"LIFT {_state.Lift:P0} / INPUT {_lift:P0} · {guide.LiftMin:P0}–{guide.LiftMax:P0}";
        _pressureGuideLabel.text = $"TENSION {_state.TensionStrength:P0} · {guide.PressureMin:P0}–{guide.PressureMax:P0}";
        _holdGuideLabel.text = $"SETTING HOLD {coaching.HoldProgress:P0}";
        GuideRange(_liftBand, guide.LiftMin, guide.LiftMax);
        GuideRange(_pressureBand, guide.PressureMin, guide.PressureMax);
        GuideRange(_holdFill, 0, coaching.HoldProgress);
        GuideMarker(_actualLiftMarker, _state.Lift);
        GuideMarker(_commandLiftMarker, _lift);
        GuideMarker(_pressureMarker, _state.TensionStrength);
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
            if (_state.Lift < PinLockEngine.MoveLiftLimit)
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
        var coaching = _practice != null
            ? (_showCoaching ? _practice.Coaching() : null)
            : _reply?.Coaching;
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
        RenderCoaching(coaching);
        _detail.text = $"DEPTH {_state.Selected + 1}/{_state.Pins}    ·    TENSION {_state.TensionStrength:P0}"
            + (coaching != null ? $"    ·    {coaching.SetPins} TRUE SET" : "")
            + (_state.Lift >= PinLockEngine.MoveLiftLimit ? "    ·    Lower pick to move" : "");
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
        PickingPracticeCursor.Release();
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

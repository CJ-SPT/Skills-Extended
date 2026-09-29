using System;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Console.Core;
using EFT.UI;
using SkillsExtended.Config.Skills;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Hacking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.Signals;

public sealed partial class SignalsView : MonoBehaviour
{
    public static SignalsView Current { get; private set; }
    public bool InRaid => _runtime;
    public SignalManifest Manifest => _runtime ? _runtime.Manifest : _practice.Manifest;
    public SignalSnapshot State => _runtime ? _runtime.State : _practice.State;
    public SignalPoint Position =>
        _runtime ? SignalsCase.Point(_runtime.World.MainPlayer.Position) : _position;
    public int Level =>
        _runtime ? SignalsSkill.Get(_runtime.World.MainPlayer.Skills).Skill.Level : _level;
    public double Clock => _runtime ? _runtime.HostTime : Time.realtimeSinceStartupAsDouble;
    public float Frequency => _frequency.value;
    public float Bearing => _bearing.value;
    public float Phase => _phase.value;
    public bool Pairing { get; private set; }
    private SignalsRuntime _runtime;
    private SignalsAuthority _practice;
    private SignalPoint _position = new();
    private int _level,
        _sequence;
    private Slider _frequency,
        _bearing,
        _phase;
    private TMP_Text _status,
        _values,
        _heading;
    private SignalGraphic _plot,
        _spectrum;
    private TMP_FontAsset _font;
    private bool _recording,
        _closed;
    private float _nextSend;
    private SignalsAudio _audio;
    private readonly HackingInputState _input = new();
    private readonly HackingUiInputState _ui = new();
    private SignalsKeyboard _keyboard;

    public static void Open(SignalsRuntime runtime, bool pairing)
    {
        if (Current || HackingView.IsOpen || SkillsExtendedInfo.IsFikaHeadless)
            return;
        var view = new GameObject("Signals receiver").AddComponent<SignalsView>();
        view._runtime = runtime;
        view.Pairing = pairing;
        Current = view;
        try
        {
            view.Build();
        }
        catch (Exception e)
        {
            SkillsExtendedPlugin.Log.LogError(e);
            view.Close();
        }
    }

    public static void Practice(int level, int seed)
    {
        if (Current || HackingView.IsOpen || SkillsExtendedInfo.IsFikaHeadless)
            return;
        if (Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance.MainPlayer)
        {
            ElectronicsRuntime.Notify("Use signals practice at the main menu.");
            return;
        }
        if (level < 0 || level > 51)
        {
            ElectronicsRuntime.Notify("Signals level must be 0–51.");
            return;
        }
        var view = new GameObject("Signals practice").AddComponent<SignalsView>();
        Current = view;
        view._level = level;
        view._practice = new SignalsAuthority(
            new SignalManifest
            {
                Raid = "practice",
                Seed = (uint)seed,
                Frequency = 94.5f,
                Config = new SignalsIntelligenceData(),
                Placement = new SignalPlacement
                {
                    Position = new SignalPoint { X = 160, Z = 210 },
                },
            }
        );
        view._practice.Ready();
        try
        {
            FindObjectOfType<ConsoleScreen>()?.SetVisible(false);
            view.Build();
        }
        catch (Exception e)
        {
            SkillsExtendedPlugin.Log.LogError(e);
            view.Close();
        }
    }

    private void Build()
    {
        _keyboard = new SignalsKeyboard(AdjustKey, ToggleRecord, Close);
        BuildScreen();
        _input.Capture(InRaid);
        _ui.Capture();
        _audio = new SignalsAudio(gameObject);
    }

    private void Move(float x, float z)
    {
        _recording = false;
        Command("cancel");
        _position = new SignalPoint { X = _position.X + x, Z = _position.Z + z };
    }

    private void ToggleRecord()
    {
        if (State.Unlocked)
            return;
        _recording = !_recording;
        if (!_recording)
            Command("cancel");
    }

    private void Command(string operation)
    {
        if (_runtime)
            _runtime.Send(operation, Frequency, Bearing, Phase);
        else
            _practice.Process(
                new SignalRequest
                {
                    Raid = "practice",
                    Actor = "practice",
                    Sequence = ++_sequence,
                    Operation = operation,
                    Frequency = Frequency,
                    Bearing = Bearing,
                    Phase = Phase,
                },
                Position,
                Level,
                Clock,
                null,
                new[] { "practice" }
            );
    }

    private void Update()
    {
        if (_closed)
            return;
        _keyboard.Advance(Time.unscaledDeltaTime);
        if (_recording && Time.unscaledTime >= _nextSend)
        {
            _nextSend = Time.unscaledTime + .1f;
            Command(Pairing ? "pair" : "scan");
        }
        var strength = SignalsModel.Strength(Manifest, Position, Level, Frequency, Bearing);
        RenderScreen(strength);
        _spectrum.SetVerticesDirty();
        _plot.SetVerticesDirty();
        var proximity = SignalsModel.ProximityInterval(Manifest, State, Position);
        // Raid proximity audio belongs to the runtime and survives closing this screen.
        var interval =
            State.Unlocked || (InRaid && proximity > 0) ? 0
            : proximity > 0 ? proximity
            : 1.3f - strength;
        _audio.Tick(interval, proximity > 0 ? 1 : strength);
        if (
            State.Unlocked
            || State.Readings.Any(r =>
                r.Actor == (_runtime ? _runtime.World.MainPlayer.ProfileId : "practice")
                && SignalPoint.Distance(r.Position, Position) < Manifest.Config.MinimumSeparation
            ) && !Pairing
        )
            _recording = false;
    }

    private void OnGUI()
    {
        if (!_closed && Application.isFocused)
            _keyboard?.Process(Event.current);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
            _keyboard?.Clear();
    }

    private void AdjustKey(KeyCode key, float dt)
    {
        if (State.Unlocked)
            return;
        switch (key)
        {
            case KeyCode.LeftArrow:
                _frequency.value -= dt;
                break;
            case KeyCode.RightArrow:
                _frequency.value += dt;
                break;
            case KeyCode.DownArrow:
                _bearing.value = SignalsModel.Wrap(_bearing.value - 50 * dt);
                break;
            case KeyCode.UpArrow:
                _bearing.value = SignalsModel.Wrap(_bearing.value + 50 * dt);
                break;
            case KeyCode.Q:
                _phase.value = SignalsModel.Wrap(_phase.value - 65 * dt);
                break;
            case KeyCode.E:
                _phase.value = SignalsModel.Wrap(_phase.value + 65 * dt);
                break;
        }
    }

    private void LateUpdate()
    {
        if (!_closed)
            _ui.Maintain();
    }

    public void Close()
    {
        if (_closed)
            return;
        _closed = true;
        _keyboard?.Clear();
        _audio?.Stop();
        if (_runtime)
            _runtime.Send("cancel");
        _input.Restore();
        _ui.Restore();
        if (Current == this)
            Current = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _input.Restore();
        _ui.Restore();
        if (Current == this)
            Current = null;
        _audio?.Dispose();
    }
}

public class SignalsConsoleCommands
{
    [ConsoleCommand("signals", "", "Receiver practice: skill level (0–51), seed")]
    public static void Practice([ConsoleArgument(0)] int level, [ConsoleArgument(1)] int seed) =>
        SignalsView.Practice(level, seed);
}

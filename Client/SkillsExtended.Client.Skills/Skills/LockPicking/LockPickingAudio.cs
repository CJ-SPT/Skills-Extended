using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Comfort.Common;
using EFT.UI;
using SkillsExtended.Config;
using SkillsExtended.LockPicking;
using UnityEngine;

namespace SkillsExtended.Skills.LockPicking;

/// <summary>Natural-pitch metal recordings, with separate movement and feedback voices.</summary>
internal sealed class LockPickingAudio : IDisposable
{
    private static readonly Dictionary<string, int> Variants = new()
    {
        ["Probe"] = 6,
        ["Pin"] = 3,
        ["Tension"] = 2,
        ["Release"] = 2,
        ["Strain"] = 2,
        ["Unlock"] = 2,
        ["Break"] = 1,
    };
    private static Dictionary<string, AudioClip> _clips;
    private readonly Dictionary<string, int> _lastVariant = new();
    private readonly System.Random _random = new();
    private readonly AudioSource _movement,
        _feedback;
    private float _lastMotion,
        _lastWarning = -10,
        _lastLift;
    private int _selected;
    private readonly PickCueReader _cues = new();
    private readonly Queue<PickSound> _pending = new();
    private bool _initialized;
    private PickOutcome _outcome;

    public LockPickingAudio(GameObject owner)
    {
        Prepare();
        AudioSource Source()
        {
            var source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.pitch = 1;
            source.ignoreListenerPause = true;
            if (Singleton<GUISounds>.Instantiated)
                source.outputAudioMixerGroup =
                    Singleton<GUISounds>.Instance.GetCommonSoundsMixerGroup();
            return source;
        }
        _movement = Source();
        _feedback = Source();
    }

    private static void Prepare()
    {
        if (_clips != null)
            return;
        using var stream =
            Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("SkillsExtended.LockPicking.audio")
            ?? throw new InvalidDataException("Missing embedded lock-picking audio.");
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var loaded = new Dictionary<string, AudioClip>();
        try
        {
            foreach (var role in Variants)
                for (var variant = 1; variant <= role.Value; variant++)
                {
                    var name = role.Key + variant.ToString("00");
                    var entry =
                        archive.GetEntry(name + ".wav")
                        ?? throw new InvalidDataException("Missing picking recording: " + name);
                    using var input = entry.Open();
                    using var reader = new BinaryReader(input);
                    if (new string(reader.ReadChars(4)) != "RIFF")
                        throw new InvalidDataException("Invalid picking recording.");
                    reader.ReadInt32();
                    if (new string(reader.ReadChars(4)) != "WAVE")
                        throw new InvalidDataException("Invalid picking recording.");
                    int channels = 0,
                        frequency = 0,
                        bits = 0;
                    float[] samples = null;
                    while (samples == null)
                    {
                        var chunk = new string(reader.ReadChars(4));
                        var length = reader.ReadInt32();
                        if (length < 0 || length > 2_000_000)
                            throw new InvalidDataException("Invalid audio chunk.");
                        var data = reader.ReadBytes(length);
                        if (data.Length != length)
                            throw new InvalidDataException("Truncated picking audio.");
                        if ((length & 1) != 0)
                            reader.ReadByte();
                        if (chunk == "fmt ")
                        {
                            if (length < 16 || BitConverter.ToInt16(data, 0) != 1)
                                throw new InvalidDataException("Expected PCM audio.");
                            channels = BitConverter.ToInt16(data, 2);
                            frequency = BitConverter.ToInt32(data, 4);
                            bits = BitConverter.ToInt16(data, 14);
                        }
                        if (chunk != "data")
                            continue;
                        if (
                            channels != 1
                            || bits != 16
                            || frequency != 44100
                            || length == 0
                            || (length & 1) != 0
                        )
                            throw new InvalidDataException("Unsupported picking recording format.");
                        samples = new float[data.Length / 2];
                        for (var i = 0; i < samples.Length; i++)
                            samples[i] = BitConverter.ToInt16(data, i * 2) / 32768f;
                    }
                    var clip = AudioClip.Create(name, samples.Length, channels, frequency, false);
                    loaded.Add(name, clip);
                    if (!clip.SetData(samples, 0))
                        throw new InvalidDataException("Could not preload picking audio.");
                }
            // Publish only a complete bank so failed initialization remains retryable.
            _clips = loaded;
        }
        catch
        {
            foreach (var clip in loaded.Values)
                UnityEngine.Object.Destroy(clip);
            throw;
        }
    }

    public void Tick(PickSnapshot state)
    {
        var volume = Mathf.Clamp01(ConfigManager.LockPickingVolume.Value / 100f);
        // The game mixer already applies UI volume; the recording bank has its own headroom.
        _movement.volume = volume * .55f;
        _feedback.volume = volume * .85f;
        var now = Time.unscaledTime;
        if (!_initialized || state.Outcome == PickOutcome.Active && _outcome != PickOutcome.Active)
        {
            _initialized = true;
            _movement.Stop();
            _feedback.Stop();
            _selected = state.Selected;
            _lastLift = state.Lift;
            _lastMotion = now;
            _lastWarning = now - 2;
            _cues.Reset(state.Cue);
            _pending.Clear();
            Remember(state);
            return;
        }
        if (state.Outcome != PickOutcome.Active)
        {
            if (state.Outcome != _outcome)
                Play(
                    _feedback,
                    state.Outcome == PickOutcome.Unlocked ? "Unlock"
                        : state.Outcome == PickOutcome.PickBroken ? "Break"
                        : "Release"
                );
            _movement.Stop();
            Remember(state);
            return;
        }

        foreach (var mechanical in _cues.Read(state)) _pending.Enqueue(mechanical.Sound);
        string cue = null;
        if (!_feedback.isPlaying && _pending.Count > 0)
            cue = _pending.Dequeue() switch
            {
                PickSound.Click => "Pin",
                PickSound.Release => "Release",
                PickSound.Tension => "Tension",
                PickSound.Strain => "Strain",
                _ => null,
            };
        if (cue == null && state.Feedback == PickFeedback.Strain
            && now - _lastWarning > 1.1f && !_feedback.isPlaying)
        {
            cue = "Strain";
            _lastWarning = now;
        }
        if (cue != null)
        {
            _movement.Stop();
            Play(_feedback, cue);
            _lastMotion = now;
            _lastLift = state.Lift;
            _selected = state.Selected;
        }
        else if (
            !_movement.isPlaying
            && !_feedback.isPlaying
            && now - _lastMotion > .22f
            && (state.Selected != _selected || Mathf.Abs(state.Lift - _lastLift) > .045f)
        )
        {
            Play(_movement, "Probe");
            _lastMotion = now;
            _lastLift = state.Lift;
            _selected = state.Selected;
        }
        Remember(state);
    }

    private void Remember(PickSnapshot state)
    {
        _outcome = state.Outcome;
    }

    private void Play(AudioSource source, string role)
    {
        var count = Variants[role];
        var variant = _random.Next(count);
        if (count > 1 && _lastVariant.TryGetValue(role, out var previous))
        {
            variant = _random.Next(count - 1);
            if (variant >= previous)
                variant++;
        }
        _lastVariant[role] = variant;
        source.clip = _clips[role + (variant + 1).ToString("00")];
        source.Play();
    }

    public void Dispose()
    {
        if (_movement)
        {
            _movement.Stop();
            UnityEngine.Object.Destroy(_movement);
        }
        if (_feedback)
        {
            _feedback.Stop();
            UnityEngine.Object.Destroy(_feedback);
        }
    }
}

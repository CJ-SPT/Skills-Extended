using System.Collections.Generic;
using Comfort.Common;
using EFT.UI;
using SkillsExtended.Config;
using UnityEngine;

namespace SkillsExtended.Skills.Hacking;

/// <summary>Preloaded 2D audio, bounded feedback voices, and short fades between cues.</summary>
public sealed class HackingAudio : MonoBehaviour
{
    public static readonly string[] Names =
    {
        "startup",
        "reveal",
        "attack",
        "cache",
        "utility",
        "shield-on",
        "shield-off",
        "success",
        "failure",
        "abort",
        "ambient",
        "low-coherence",
    };
    private static HackingAudio _tail;
    private readonly AudioSource[] _feedback = new AudioSource[2];
    private readonly float[] _targets = new float[2];
    private AudioSource _ambient;
    private AudioSource _low;
    private IReadOnlyDictionary<string, AudioClip> _clips;
    private int _voice;
    private float _gain = 1;
    private float _ambientTarget;
    private float _lowTarget;
    private bool _terminal;

    public void Initialize(IReadOnlyDictionary<string, AudioClip> clips)
    {
        if (_tail)
        {
            _tail.FadeOut();
        }

        _clips = clips;
        if (Singleton<GUISounds>.Instantiated)
        {
            _gain = Singleton<GUISounds>.Instance.GetCommonSoundsVolume();
        }

        for (var i = 0; i < _feedback.Length; i++)
        {
            _feedback[i] = Source(false, 0);
        }

        _ambient = Source(true, 0);
        _ambient.clip = clips["ambient"];
        _low = Source(true, 0);
        _low.clip = clips["low-coherence"];
        Restart();
    }

    private AudioSource Source(bool loop, float volume)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.dopplerLevel = 0;
        source.loop = loop;
        source.volume = volume * _gain;
        if (Singleton<GUISounds>.Instantiated)
        {
            source.outputAudioMixerGroup =
                Singleton<GUISounds>.Instance.GetCommonSoundsMixerGroup();
        }

        return source;
    }

    public void Restart()
    {
        _terminal = false;
        _lowTarget = 0;
        _ambientTarget = .025f * _gain;
        if (!_ambient.isPlaying)
        {
            _ambient.Play();
        }

        Play("startup");
    }

    public void LowCoherence(bool active)
    {
        _lowTarget = active && !_terminal ? .03f * _gain : 0;
        if (_lowTarget > 0 && !_low.isPlaying)
        {
            _low.Play();
        }
    }

    public void Play(string cue, bool terminal = false)
    {
        if (!_clips.TryGetValue(cue, out var clip) || !clip)
        {
            return;
        }

        _terminal = terminal;
        if (terminal)
        {
            _ambientTarget = _lowTarget = 0;
        }

        // Replace the prior action using a 25 ms crossfade, rather than accumulating
        // long PlayOneShot tails (the EVE startup recording alone is 13.6 seconds).
        _targets[_voice] = 0;
        _voice = 1 - _voice;
        var source = _feedback[_voice];
        source.Stop();
        source.volume = 0;
        source.clip = clip;
        _targets[_voice] = .18f * _gain;
        source.Play();
    }

    private void Update()
    {
        // Read the local preference on every frame, including detached result sounds.
        // Keep the fade speed independent of this multiplier so zero can still fade out.
        var volume = Mathf.Clamp01((ConfigManager.HackingVolume?.Value ?? 100) / 100f);
        for (var i = 0; i < _feedback.Length; i++)
        {
            var source = _feedback[i];
            if (!source)
            {
                continue;
            }

            source.volume = Mathf.MoveTowards(
                source.volume,
                _targets[i] * volume,
                .18f * _gain * Time.unscaledDeltaTime / .025f
            );
            if (_targets[i] == 0 && source.volume == 0 && source.isPlaying)
            {
                source.Stop();
            }
        }

        FadeLoop(_ambient, _ambientTarget, volume);
        FadeLoop(_low, _lowTarget, volume);
    }

    private void FadeLoop(AudioSource source, float target, float volume)
    {
        if (!source)
        {
            return;
        }

        source.volume = Mathf.MoveTowards(
            source.volume,
            target * volume,
            .03f * _gain * Time.unscaledDeltaTime / .025f
        );
        if (target == 0 && source.volume == 0 && source.isPlaying)
        {
            source.Stop();
        }
    }

    public void Close()
    {
        _ambientTarget = _lowTarget = 0;
        transform.SetParent(null, false);
        if (_terminal)
        {
            _tail = this;
            var voice = _feedback[_voice];
            Destroy(gameObject, Mathf.Max(.05f, voice.clip.length - voice.time) + .1f);
        }
        else
        {
            FadeOut();
        }
    }

    private void FadeOut()
    {
        _targets[0] = _targets[1] = 0;
        _ambientTarget = _lowTarget = 0;
        Destroy(gameObject, .1f);
        if (_tail == this)
        {
            _tail = null;
        }
    }

    private void OnDestroy()
    {
        if (_tail == this)
        {
            _tail = null;
        }
    }
}

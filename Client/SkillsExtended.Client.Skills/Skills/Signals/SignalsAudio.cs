using System;
using Comfort.Common;
using EFT.UI;
using UnityEngine;

namespace SkillsExtended.Skills.Signals;

// Local PDA audio: no world emitter or network traffic. Owned by the raid or practice view.
internal sealed class SignalsAudio : IDisposable
{
    private readonly AudioSource _source;
    private readonly AudioClip _tone;
    private float _lastBeep = float.NegativeInfinity;

    public SignalsAudio(GameObject owner)
    {
        _source = owner.AddComponent<AudioSource>();
        _source.spatialBlend = 0;
        _source.playOnAwake = false;
        if (Singleton<GUISounds>.Instantiated)
            _source.outputAudioMixerGroup =
                Singleton<GUISounds>.Instance.GetCommonSoundsMixerGroup();
        var samples = new float[4800];
        for (var i = 0; i < samples.Length; i++)
        {
            var envelope = Math.Min(1, i / 240f) * (1 - i / (float)samples.Length);
            samples[i] = (float)(Math.Sin(i * 2 * Math.PI * 880 / 48000) * .3 * envelope);
        }
        _tone = AudioClip.Create("PDA beacon", samples.Length, 1, 48000, false);
        _tone.SetData(samples, 0);
    }

    public void Tick(float interval, float gain)
    {
        _source.volume = Mathf.Clamp01(Config.ConfigManager.SignalsVolume.Value / 100f * gain);
        if (interval <= 0 || _source.volume <= .001f)
        {
            Stop();
            return;
        }
        // Comparing against the last pulse lets the cadence react immediately as distance changes.
        if (Time.unscaledTime - _lastBeep >= interval)
        {
            _lastBeep = Time.unscaledTime;
            _source.PlayOneShot(_tone);
        }
    }

    public void Stop()
    {
        if (_source)
            _source.Stop();
        _lastBeep = float.NegativeInfinity;
    }

    public void Dispose()
    {
        Stop();
        if (_source)
            UnityEngine.Object.Destroy(_source);
        if (_tone)
            UnityEngine.Object.Destroy(_tone);
    }
}

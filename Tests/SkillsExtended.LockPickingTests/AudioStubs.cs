// Model native object destruction and unused-asset cleanup; this does not run Unity audio.
namespace UnityEngine
{
    public enum HideFlags { None = 0, DontUnloadUnusedAsset = 32 }
    public class Object
    {
        public bool Destroyed;
        public HideFlags hideFlags;
        public static implicit operator bool(Object obj) => obj is not null && !obj.Destroyed;
        public static void Destroy(Object obj) { if (obj is not null) obj.Destroyed = true; }
    }
    public sealed class GameObject
    {
        public readonly List<AudioSource> Sources = new();
        public T AddComponent<T>() where T : AudioSource, new()
        {
            var source = new T();
            Sources.Add(source);
            return source;
        }
    }
    public sealed class AudioClip : Object
    {
        public static readonly List<AudioClip> Created = new();
        public static bool FailNextSetData;
        public string Name;
        public float[] Samples;
        public static AudioClip Create(string name, int samples, int channels, int frequency, bool stream)
        {
            var clip = new AudioClip { Name = name };
            Created.Add(clip);
            return clip;
        }
        public bool SetData(float[] samples, int offset)
        {
            if (FailNextSetData) { FailNextSetData = false; return false; }
            Samples = samples;
            return true;
        }
        public static void UnloadUnused()
        {
            foreach (var clip in Created)
                if ((clip.hideFlags & HideFlags.DontUnloadUnusedAsset) == 0) Destroy(clip);
        }
    }
    public class AudioSource : Object
    {
        public bool playOnAwake, ignoreListenerPause, isPlaying;
        public float spatialBlend, dopplerLevel, pitch, volume;
        public object outputAudioMixerGroup;
        public AudioClip clip;
        public int Plays;
        public void Stop() => isPlaying = false;
        public void Play()
        {
            if (!clip || clip.Samples is null) throw new InvalidOperationException("Invalid audio clip");
            isPlaying = true;
            Plays++;
        }
    }
    public static class Time { public static float unscaledTime; }
}
namespace Comfort.Common
{
    public static class Singleton<T> where T : new()
    {
        public static bool Instantiated => true;
        public static T Instance { get; } = new();
    }
}
namespace EFT.UI
{
    public sealed class GUISounds
    {
        public object GetCommonSoundsMixerGroup() => this;
    }
}
namespace SkillsExtended.Config
{
    public static class ConfigManager
    {
        public sealed class VolumeSetting { public int Value = 100; }
        public static VolumeSetting LockPickingVolume = new();
    }
}

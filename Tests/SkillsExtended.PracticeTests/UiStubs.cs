// External Unity/EFT surface for running the production button binding and patches.
// These substitutes do not render UI or claim native Harmony/game acceptance.
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine
{
    public class Object
    {
        public static implicit operator bool(Object value) => value is not null;
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public bool isActiveAndEnabled => gameObject.activeInHierarchy;
        public T GetComponent<T>() where T : Component => gameObject.Components.OfType<T>().FirstOrDefault();
        public T GetComponentInParent<T>() where T : Component => GetComponent<T>() ?? transform.Parent?.GetComponentInParent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component =>
            gameObject.Descendants().Where(g => includeInactive || g.activeInHierarchy).SelectMany(g => g.Components).OfType<T>().ToArray();
        public T GetComponentInChildren<T>() where T : Component => GetComponentsInChildren<T>().FirstOrDefault();
    }
    public class MonoBehaviour : Component;
    public class Transform : Component
    {
        public Transform Parent;
        public readonly List<Transform> Children = new();
        public void SetParent(Transform parent, bool worldPositionStays = false)
        {
            Parent?.Children.Remove(this);
            Parent = parent;
            parent?.Children.Add(this);
        }
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta;
    }
    public class GameObject : Object
    {
        public readonly List<Component> Components = new();
        public readonly RectTransform transform;
        public bool activeSelf = true;
        public bool activeInHierarchy => activeSelf && (transform.Parent?.gameObject.activeInHierarchy ?? true);
        public GameObject() { transform = new() { gameObject = this }; Components.Add(transform); }
        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T { gameObject = this };
            Components.Add(component);
            Invoke(component, "Awake");
            return component;
        }
        public void SetActive(bool active) => activeSelf = active;
        public IEnumerable<GameObject> Descendants() => new[] { this }.Concat(transform.Children.SelectMany(t => t.gameObject.Descendants()));
        public static void Invoke(Component component, string method) => component.GetType()
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(component, null);
    }
    public readonly record struct Vector2(float x, float y)
    {
        public static Vector2 zero => new(0, 0);
    }
    public static class Mathf { public static float Max(float a, float b) => Math.Max(a, b); }
    public class RectOffset { public int bottom; }
}
namespace UnityEngine.UI
{
    public class Button : Component
    {
        public bool interactable = true;
        public Action Click;
    }
    public class GridLayoutGroup : Component
    {
        public Vector2 spacing;
        public RectOffset padding = new();
    }
    public static class LayoutRebuilder { public static void MarkLayoutForRebuild(RectTransform rect) { } }
}
namespace TMPro
{
    public class TMP_FontAsset : UnityEngine.Object;
    public class TMP_Text : Component { public TMP_FontAsset font = new(); }
    public static class TMP_Settings { public static TMP_FontAsset defaultFontAsset = new(); }
}
namespace EFT
{
    public enum ESkillId { Endurance, Lockpicking = 43 }
    public class Skill { public ESkillId Id; public int Level; public bool Locked; }
    public class HideoutPlayerOwner { public void TranslateCommand() { } }
    public class GameWorld { public void InitLevel() { } }
}
namespace EFT.UI
{
    public class SkillsScreen : MonoBehaviour { public void Close() { } }
    public class SkillPanel : MonoBehaviour { public void Show() { } }
    public class SkillIcon : MonoBehaviour { public void Show() { } }
    public class SkillThumbs : MonoBehaviour;
    public class InventoryScreen { public void TranslateCommand() { } }
    public class MenuScreen { public void TranslateCommand() { } }
    public class MenuTaskBar { public void TranslateCommand() { } }
}
namespace EFT.InputSystem
{
    public class InputNode { public enum ETranslateResult { Ignore, BlockAll } }
}
namespace SPT.Reflection.Patching
{
    public abstract class ModulePatch { protected abstract MethodBase GetTargetMethod(); }
    public class PatchPrefixAttribute : Attribute;
    public class PatchPostfixAttribute : Attribute;
}
namespace HarmonyLib
{
    public static class AccessTools { public static MethodInfo Method(Type type, string name) => type.GetMethod(name); }
}
namespace SkillsExtended
{
    public static class SkillsExtendedPlugin
    {
        public static readonly Logger Log = new();
        public class Logger { public void LogError(object error) => throw new Exception("Production binding failed", error as Exception); }
    }
}
namespace SkillsExtended.Skills.Practice
{
    internal static class PracticeUi
    {
        internal static Button Button(Transform parent, string text, TMPro.TMP_FontAsset font,
            Vector2 size, Vector2 position, Action click, float fontSize)
        {
            var go = new GameObject();
            go.transform.SetParent(parent);
            go.transform.sizeDelta = size;
            go.transform.anchoredPosition = position;
            var button = go.AddComponent<Button>();
            button.Click = click;
            return button;
        }
    }
    internal static class PracticeController
    {
        internal static bool BlocksInput, AnyGameOpen, InRaid;
        internal static EFT.Skill OpenedSkill;
        internal static int OpenCount;
        internal static bool Available(EFT.Skill skill) => skill != null && !skill.Locked && !InRaid;
        internal static void Open(EFT.UI.SkillsScreen screen, EFT.Skill skill, TMPro.TMP_FontAsset font)
        { OpenedSkill = skill; OpenCount++; }
        internal static void ScreenClosed(EFT.UI.SkillsScreen screen) { }
        internal static void WorldStarting() { }
    }
}

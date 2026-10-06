using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SkillsExtended.DeveloperTools;

internal sealed partial class DeveloperEditorView : IDisposable
{
    private const string Assets = "assets/mods/wtt-campaigns.assets/editortoolkit/";
    private static AssetBundle _bundle;
    private readonly GameObject _host;
    private readonly PanelSettings _settings;
    internal readonly VisualElement Root, Surface, Toolbar, Actions, Inspector, Markers;
    internal readonly ScrollView List;
    internal readonly TextField Search;
    private readonly Label _status;
    private readonly Label _controls;
    private readonly Label _draftState;
    private readonly Label _browserTitle;
    internal readonly VisualElement BrowserControls;
    private readonly VisualElement _confirmation;
    private int _escapeFrame = -1;
    private readonly Dictionary<TextField, string> _committed = new();
    internal bool Visible { get; private set; }
    internal Action Escape;
    internal Action<Exception> Failed;
    internal float Scale => _settings.scale;
    internal bool Typing
    {
        get
        {
            var focus = Root.panel?.focusController.focusedElement as VisualElement;
            while (focus != null)
            {
                if (focus is TextField || focus is BaseField<float>) return true;
                focus = focus.parent;
            }
            return false;
        }
    }
    internal bool Confirming => _confirmation.style.display.value != DisplayStyle.None;
    internal bool HasInvalid => Root.Query<TextField>(className: "editor-invalid").ToList().Count != 0;
    internal Vector2 Pointer => Root.panel == null ? Vector2.zero : RuntimePanelUtils.ScreenToPanel(
        Root.panel, new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
    internal bool PointerOver
    {
        get
        {
            var picked = Root.panel?.Pick(Pointer);
            return picked != null && picked != Root && picked != Surface;
        }
    }
    internal bool PointerOverPanel
    {
        get
        {
            var picked = Root.panel?.Pick(Pointer);
            return picked != null && picked != Root && picked != Surface
                && picked != Markers && !Markers.Contains(picked);
        }
    }
    internal static float ResolveScale(int width, int height) =>
        DeveloperEditorLayout.Scale(width, height);

    internal DeveloperEditorView()
    {
        if (!_bundle)
            _bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "bundles", "skills_editor_toolkit.bundle"));
        if (!_bundle) throw new InvalidOperationException(LocalizedText.Get("SkillsExtended.DeveloperEditorView.InstallSkillsEditorToolkitBundle"));
        var template = _bundle.LoadAsset<PanelSettings>(Assets + "editorpanel.asset");
        var tree = _bundle.LoadAsset<VisualTreeAsset>(Assets + "editor.uxml");
        var font = _bundle.LoadAsset<Font>("assets/mods/wtt-campaigns.assets/fonts/bender.ttf");
        if (!template || !tree || !font) throw new InvalidOperationException(LocalizedText.Get("SkillsExtended.DeveloperEditorView.SkillsExtendedDeveloperEditorUiAssetsAreIncomplete"));
        _settings = Object.Instantiate(template);
        _settings.scaleMode = PanelScaleMode.ConstantPixelSize; _settings.sortingOrder = 1800;
        _host = new GameObject("Skills Extended developer editor Toolkit");
        try
        {
            var document = _host.AddComponent<UIDocument>();
            document.panelSettings = _settings; document.visualTreeAsset = tree;
            Root = document.rootVisualElement; Root.pickingMode = PickingMode.Ignore;
            Surface = Root.Q("surface"); Surface.ClearClassList(); Surface.AddToClassList("editor-root");
            Surface.pickingMode = PickingMode.Ignore;
            Surface.style.position = Position.Absolute;
            Surface.style.left = Surface.style.top = Surface.style.right = Surface.style.bottom = 0;
            Surface.style.unityFontDefinition = FontDefinition.FromFont(font);

            Toolbar = Panel("Toolbar", 0, 0, 0, null); Toolbar.style.height = 38;
            Toolbar.style.flexDirection = FlexDirection.Row;
            Toolbar.style.paddingTop = Toolbar.style.paddingBottom = 0; Toolbar.style.alignItems = Align.Center;
            _draftState = new Label(); _draftState.style.minWidth = 200; Toolbar.Add(_draftState);
            Actions = Panel("ToolActions", 0, 40, 0, null); Actions.style.height = 38;
            Actions.style.flexDirection = FlexDirection.Row;
            Actions.style.paddingTop = Actions.style.paddingBottom = 0; Actions.style.alignItems = Align.Center;
            var browser = Panel("Placements", 0, DeveloperEditorLayout.ToolTop, null, DeveloperEditorLayout.PanelBottom);
            browser.style.width = DeveloperEditorLayout.BrowserWidth;
            browser.AddToClassList("editor-tool-window");
            _browserTitle = new Label(); _browserTitle.AddToClassList("editor-heading"); browser.Add(_browserTitle);
            Search = Field(browser, LocalizedText.Get("SkillsExtended.DeveloperEditorView.Find"), "", _ => { }); Search.isDelayed = false;
            Search.AddToClassList("editor-choice-search");
            Search.tooltip = LocalizedText.Get("SkillsExtended.DeveloperEditorView.SearchNamesSceneNamesKeysOrIds");
            BrowserControls = new VisualElement(); BrowserControls.style.flexShrink = 0; browser.Add(BrowserControls);
            List = Scroll(browser);
            Inspector = Panel("Inspector", null, DeveloperEditorLayout.ToolTop, 0, DeveloperEditorLayout.PanelBottom);
            Inspector.style.width = DeveloperEditorLayout.InspectorWidth;
            Inspector.AddToClassList("editor-tool-window");
            Markers = new VisualElement { pickingMode = PickingMode.Ignore };
            Markers.style.position = Position.Absolute; Markers.style.left = Markers.style.top = 0;
            Markers.style.right = Markers.style.bottom = 0; Surface.Insert(0, Markers);
            var bottom = Panel("Status", 0, null, 0, 0); bottom.style.height = DeveloperEditorLayout.StatusHeight;
            _status = new Label(); bottom.Add(_status);
            _controls = new Label(LocalizedText.Get("SkillsExtended.DeveloperEditorView.AiAndRaidTimeContinueRmbWasdQeFly"));
            _controls.style.fontSize = 13; bottom.Add(_controls);
            _confirmation = Panel("Confirm", null, 330, null, null);
            _confirmation.style.left = Length.Percent(32); _confirmation.style.right = Length.Percent(32);
            _confirmation.style.display = DisplayStyle.None;
            Root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Escape) return;
                _escapeFrame = Time.frameCount;
                if (MenuOpen) DismissMenu();
                else if (Confirming) _confirmation.style.display = DisplayStyle.None;
                else if (Typing) CancelTyping();
                else Escape?.Invoke();
                evt.StopPropagation(); evt.PreventDefault();
            }, TrickleDown.TrickleDown);
            SetVisible(false); Tick();
        }
        catch { Dispose(); throw; }
    }
    internal bool ConsumedEscape => _escapeFrame == Time.frameCount;
    private VisualElement Panel(string name, float? left, float? top, float? right, float? bottom)
    {
        var panel = new VisualElement { name = name }; panel.AddToClassList("editor-window");
        panel.AddToClassList("editor-surface"); panel.style.position = Position.Absolute;
        panel.style.paddingBottom = 4; panel.style.minHeight = 0;
        if (left.HasValue) panel.style.left = left.Value;
        if (right.HasValue) panel.style.right = right.Value;
        if (top.HasValue) panel.style.top = top.Value;
        if (bottom.HasValue) panel.style.bottom = bottom.Value;
        Surface.Add(panel); return panel;
    }
    internal Button Button(VisualElement parent, string text, Action action, string help = "")
    {
        var tree = _bundle.LoadAsset<VisualTreeAsset>(Assets + "action.uxml").CloneTree();
        var button = tree.Q<Button>(); button.RemoveFromHierarchy();
        button.text = text; button.tooltip = help; button.style.minHeight = 28;
        button.style.flexShrink = 0;
        button.clicked += () => { try { action(); } catch (Exception e) { Failed?.Invoke(e); } };
        parent.Add(button); return button;
    }
    internal void Separator(VisualElement parent)
    {
        var separator = new VisualElement { pickingMode = PickingMode.Ignore };
        separator.AddToClassList("editor-toolbar-separator");
        separator.style.width = separator.style.minWidth = separator.style.maxWidth = 1;
        separator.style.height = 20; separator.style.flexShrink = 0;
        separator.style.alignSelf = Align.Center;
        separator.style.marginLeft = separator.style.marginRight = 6;
        separator.style.backgroundColor = new Color(75 / 255f, 78 / 255f, 71 / 255f);
        parent.Add(separator);
    }
    internal TextField Field(VisualElement parent, string name, string value, Action<string> changed)
    {
        var tree = _bundle.LoadAsset<VisualTreeAsset>(Assets + "field.uxml").CloneTree();
        var field = tree.Q<TextField>(); field.RemoveFromHierarchy();
        field.label = name; field.name = name; field.isDelayed = true;
        field.style.minHeight = 30; field.style.flexShrink = 0;
        field.SetValueWithoutNotify(value); _committed[field] = value;
        field.RegisterValueChangedCallback(evt =>
        {
            changed(evt.newValue);
            if (!field.ClassListContains("editor-invalid")) _committed[field] = evt.newValue;
        });
        field.RegisterCallback<DetachFromPanelEvent>(_ => _committed.Remove(field));
        parent.Add(field); return field;
    }
    internal TextField Number(VisualElement parent, string name, float value, float min, float max, Action<float> changed)
    {
        TextField field = null;
        field = Field(parent, name, value.ToString("0.###", CultureInfo.InvariantCulture), text =>
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                && !float.IsNaN(number) && !float.IsInfinity(number) && number >= min && number <= max)
            { field.RemoveFromClassList("editor-invalid"); field.style.borderBottomColor = StyleKeyword.Null; changed(number); }
            else
            {
                field.AddToClassList("editor-invalid");
                field.style.borderBottomWidth = 2; field.style.borderBottomColor = new Color(.9f, .25f, .2f);
                Status(LocalizedText.Get("SkillsExtended.DeveloperEditorView.MustBeAFiniteNumberFromTo", name, min, max));
            }
        });
        return field;
    }
    internal void Confirm(string message, Action yes)
    {
        DismissMenu();
        _confirmation.Clear(); _confirmation.Add(new Label(message));
        Button(_confirmation, LocalizedText.Get("SkillsExtended.DeveloperEditorView.DiscardAndReload"), () => { _confirmation.style.display = DisplayStyle.None; yes(); });
        Button(_confirmation, LocalizedText.Get("SkillsExtended.DeveloperEditorView.KeepDraft"), () => _confirmation.style.display = DisplayStyle.None);
        _confirmation.style.display = DisplayStyle.Flex; _confirmation.BringToFront();
    }
    internal void Status(string text) => _status.text = LocalizedText.Resolve(text);
    internal void DraftState(string text) { if (_draftState.text != text) _draftState.text = text; }
    internal void BrowserTitle(string title, int matches, int total) => _browserTitle.text = title.ToUpperInvariant() + " · " + matches + " / " + total;
    internal void Tick()
    {
        var scale = ResolveScale(Screen.width, Screen.height);
        if (_settings.scale != scale) _settings.scale = scale;
        _positionMenu?.Invoke();
    }
    internal void ReleaseFocus() => (Root?.panel?.focusController.focusedElement as VisualElement)?.Blur();
    internal void CancelTyping()
    {
        var focus = Root.panel?.focusController.focusedElement as VisualElement;
        while (focus != null)
        {
            if (focus is TextField field)
            {
                if (_committed.TryGetValue(field, out var text)) field.SetValueWithoutNotify(text);
                field.RemoveFromClassList("editor-invalid"); field.style.borderBottomColor = StyleKeyword.Null;
                break;
            }
            focus = focus.parent;
        }
        ReleaseFocus();
    }
    internal void SetVisible(bool visible)
    {
        if (!visible) { DismissMenu(); ReleaseFocus(); _confirmation.style.display = DisplayStyle.None; }
        Visible = visible; Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    public void Dispose()
    {
        if (_host) { _host.SetActive(false); Object.Destroy(_host); }
        if (_settings) Object.Destroy(_settings);
    }
}

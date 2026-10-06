using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillsExtended.DeveloperTools;

// Campaigns' themed runtime choices and scroll styling, hosted by our own panel/bundle.
internal sealed partial class DeveloperEditorView
{
    private VisualElement _menu, _menuAnchor;
    private Action _positionMenu;
    private int _menuDismissFrame = -1;
    internal bool MenuOpen => _menu != null;
    internal bool MenuDismissed => _menuDismissFrame == Time.frameCount;

    internal static void StyleScroll(ScrollView scroll)
    {
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        scroll.verticalScroller.AddToClassList("editor-scrollbar");
        scroll.verticalScroller.lowButton.AddToClassList("editor-hidden");
        scroll.verticalScroller.highButton.AddToClassList("editor-hidden");
        scroll.verticalScroller.slider.AddToClassList("editor-scroll-slider");
        scroll.style.minHeight = 0;
    }
    internal ScrollView Scroll(VisualElement parent)
    {
        var scroll = new ScrollView(); StyleScroll(scroll); scroll.style.flexGrow = 1;
        parent.Add(scroll); return scroll;
    }
    internal Button Row(string title, string subtitle, Action select, bool selected, string help)
    {
        var row = Button(List, "", select, help);
        row.style.alignSelf = Align.Stretch; row.style.flexDirection = FlexDirection.Column;
        row.style.justifyContent = Justify.Center;
        row.style.minWidth = 0; row.style.height = row.style.minHeight = 48;
        row.style.marginLeft = row.style.marginRight = 0;
        row.EnableInClassList("editor-selected", selected);
        foreach (var text in new[] { title, subtitle })
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore, enableRichText = false };
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.alignSelf = Align.Stretch; label.style.minWidth = 0;
            label.style.whiteSpace = WhiteSpace.NoWrap; label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            if (row.childCount > 0) { label.style.fontSize = 12; label.style.color = new Color(164 / 255f, 175 / 255f, 165 / 255f); }
            row.Add(label);
        }
        return row;
    }
    internal Button Choice(VisualElement parent, string caption, IReadOnlyList<string> options, int selected, Action<int> changed)
    {
        Button button = null;
        Label value = null;
        button = Button(parent, "", () => ShowChoices(button, options, selected, index =>
        { selected = index; value.text = options[index]; changed(index); }));
        button.AddToClassList("editor-choice-field"); button.style.alignSelf = Align.Stretch;
        button.style.width = Length.Percent(100); button.style.marginLeft = button.style.marginRight = 0;
        var tree = _bundle.LoadAsset<VisualTreeAsset>(Assets + "choicefield.uxml").CloneTree();
        var content = tree.Q<Label>("Caption").parent;
        content.Q<Label>("Caption").text = caption;
        value = content.Q<Label>("Value");
        value.text = selected >= 0 && selected < options.Count ? options[selected] : LocalizedText.Get("SkillsExtended.DeveloperEditorControls.Select");
        while (content.childCount > 0) button.Add(content[0]);
        return button;
    }
    internal bool DismissMenu()
    {
        if (_menu == null) return false;
        _menu.RemoveFromHierarchy(); _menu = null; _positionMenu = null;
        _menuDismissFrame = Time.frameCount;
        if (_menuAnchor?.panel != null && _menuAnchor.enabledInHierarchy) _menuAnchor.Focus();
        _menuAnchor = null; return true;
    }
    private void ShowChoices(VisualElement anchor, IReadOnlyList<string> options, int selected, Action<int> changed)
    {
        DismissMenu();
        var labels = options.ToArray();
        var tree = _bundle.LoadAsset<VisualTreeAsset>(Assets + "choicepopup.uxml").CloneTree();
        var shield = tree.Q("ChoicePanel").parent; shield.RemoveFromHierarchy();
        _menu = shield; _menuAnchor = anchor; Surface.Add(shield);
        var panel = shield.Q("ChoicePanel"); var search = shield.Q<TextField>("ChoiceSearch");
        var empty = shield.Q<Label>("ChoiceEmpty"); var list = shield.Q<ListView>("Choices");
        search.label = LocalizedText.Get("SkillsExtended.Editor.Search");
        empty.text = LocalizedText.Get("SkillsExtended.Editor.NoMatches");
        var matches = new List<int>(); var searching = labels.Length > 10;
        search.style.display = searching ? DisplayStyle.Flex : DisplayStyle.None;
        list.fixedItemHeight = 30; list.selectionType = SelectionType.Single;
        list.makeItem = () =>
        {
            var template = _bundle.LoadAsset<VisualTreeAsset>(Assets + "choiceoption.uxml").CloneTree();
            var option = template.Q<Button>(); option.RemoveFromHierarchy(); option.enableRichText = false;
            option.clicked += () => Select((int)option.userData); return option;
        };
        list.bindItem = (element, row) =>
        {
            var index = matches[row]; var option = (Button)element;
            option.userData = index; option.text = option.tooltip = labels[index];
            option.EnableInClassList("editor-selected", index == selected);
        };
        StyleScroll(list.Q<ScrollView>());
        void Select(int index)
        {
            DismissMenu();
            if (index >= 0 && index < labels.Length) { try { changed(index); } catch (Exception e) { Failed?.Invoke(e); } }
        }
        void Filter()
        {
            matches = Enumerable.Range(0, labels.Length).Where(i => labels[i].IndexOf(search.value ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            list.itemsSource = matches; list.Rebuild();
            var row = matches.IndexOf(selected); if (row < 0 && matches.Count > 0) row = 0;
            list.SetSelectionWithoutNotify(row < 0 ? Array.Empty<int>() : new[] { row });
            empty.style.display = matches.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            list.style.display = matches.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _positionMenu?.Invoke();
            list.schedule.Execute(() => { if (list.panel != null && row >= 0) list.ScrollToItem(row); });
        }
        _positionMenu = () =>
        {
            if (anchor.panel == null || !anchor.enabledInHierarchy) { DismissMenu(); return; }
            for (var p = anchor; p != null; p = p.parent)
                if (p.resolvedStyle.display == DisplayStyle.None) { DismissMenu(); return; }
            var bounds = anchor.worldBound;
            var min = Surface.WorldToLocal(bounds.min); var max = Surface.WorldToLocal(bounds.max);
            var position = DeveloperEditorLayout.Popup(min.x, min.y, max.y, bounds.width,
                Math.Min(300, Math.Max(1, matches.Count) * 30) + (searching ? 38 : 0) + 12,
                Surface.resolvedStyle.width, Surface.resolvedStyle.height);
            panel.style.left = position.X; panel.style.top = position.Y;
            panel.style.width = position.Width; panel.style.height = position.Height;
        };
        search.RegisterValueChangedCallback(_ => Filter());
        shield.RegisterCallback<PointerDownEvent>(evt =>
        { if (evt.target == shield) DismissMenu(); evt.StopPropagation(); });
        shield.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Escape) DismissMenu();
            else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            { if (list.selectedIndex >= 0 && list.selectedIndex < matches.Count) Select(matches[list.selectedIndex]); }
            else if (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.UpArrow)
            {
                if (matches.Count > 0)
                {
                    var row = (list.selectedIndex + (evt.keyCode == KeyCode.DownArrow ? 1 : -1) + matches.Count) % matches.Count;
                    list.SetSelectionWithoutNotify(new[] { row }); list.ScrollToItem(row);
                }
            }
            else if (evt.keyCode == KeyCode.Tab)
            { if (searching && !search.Contains(evt.target as VisualElement)) search.Focus(); else list.Focus(); }
            else return;
            evt.StopPropagation(); evt.PreventDefault();
        }, TrickleDown.TrickleDown);
        Filter(); if (searching) search.Focus(); else list.Focus();
    }
}

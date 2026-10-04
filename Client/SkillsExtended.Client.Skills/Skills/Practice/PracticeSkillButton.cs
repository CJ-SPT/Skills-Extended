using System;
using EFT;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.Practice;

internal sealed class PracticeSkillButton : MonoBehaviour
{
    private Skill _skill;
    private SkillsScreen _screen;
    private TMP_FontAsset _font;
    private Button _button;

    internal static void Bind(Component view, Skill skill, bool icon)
    {
        var existing = view.GetComponent<PracticeSkillButton>();
        var screen = view.GetComponentInParent<SkillsScreen>();
        var eligibleView = screen && (!icon || (view.GetComponentInParent<SkillThumbs>()
            && !view.GetComponentInParent<SkillPanel>()));
        if (!eligibleView || skill == null || PracticeSettings.ForSkill(skill.Id) == PracticeGame.None)
        {
            if (existing) existing.Unbind();
            return;
        }
        try
        {
            var binding = existing ? existing : view.gameObject.AddComponent<PracticeSkillButton>();
            binding._skill = skill;
            binding._screen = screen;
            if (!binding._button)
                binding.Build(icon);
            binding.Refresh();
        }
        catch (Exception error)
        {
            // A missing/customized native view must not prevent the Skills screen opening.
            view.GetComponent<PracticeSkillButton>()?.Unbind();
            SkillsExtendedPlugin.Log.LogError(error);
        }
    }

    private void Build(bool icon)
    {
        foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            if (label.font) { _font = label.font; break; }
        if (!_font) _font = TMP_Settings.defaultFontAsset;
        _button = PracticeUi.Button(transform, "Practice", _font,
            new Vector2(icon ? 96 : 80, 24), Vector2.zero,
            () => PracticeController.Open(_screen, _skill, _font), 16);
        var rect = (RectTransform)_button.transform;
        if (icon)
        {
            // Keep the native 96px icon, level, points and class badge intact. A footer
            // uses the grid gutter; its layout companion reserves room for every row.
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -9);
            var thumbs = GetComponentInParent<SkillThumbs>();
            if (!thumbs.GetComponent<PracticeGridSpacing>())
                thumbs.gameObject.AddComponent<PracticeGridSpacing>();
        }
        else
        {
            // Native level text reserves the last 150px; the progress number occupies
            // the last 60px. Use that gap beneath the bar, without covering either.
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-68, 4);
        }
    }

    private void Unbind()
    {
        _skill = null;
        if (_button) _button.gameObject.SetActive(false);
    }

    private void Update() => Refresh();
    internal bool Visible => _button && _button.gameObject.activeInHierarchy;
    private void Refresh()
    {
        if (!_button) return;
        var visible = _screen && _screen.isActiveAndEnabled && PracticeController.Available(_skill);
        if (_button.gameObject.activeSelf != visible) _button.gameObject.SetActive(visible);
        _button.interactable = !PracticeController.BlocksInput && !PracticeController.AnyGameOpen;
    }
}

internal sealed class PracticeGridSpacing : MonoBehaviour
{
    private GridLayoutGroup _grid;
    private Vector2 _spacing;
    private int _bottom;
    private bool _expanded;

    private void Awake()
    {
        _grid = GetComponent<GridLayoutGroup>();
        if (!_grid) return;
        _spacing = _grid.spacing;
        _bottom = _grid.padding.bottom;
    }

    private void LateUpdate()
    {
        var needed = false;
        foreach (var button in GetComponentsInChildren<PracticeSkillButton>())
            if (button.Visible) { needed = true; break; }
        SetExpanded(needed);
    }

    private void SetExpanded(bool value)
    {
        if (!_grid || value == _expanded) return;
        _expanded = value;
        _grid.spacing = value ? new Vector2(_spacing.x, Mathf.Max(_spacing.y, 40)) : _spacing;
        _grid.padding.bottom = _bottom + (value ? 34 : 0);
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)_grid.transform);
    }

    private void OnDisable() => SetExpanded(false);
    private void OnDestroy() => SetExpanded(false);
}

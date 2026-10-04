using System.Reflection;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using SkillsExtended.Skills.Practice;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

var assertions = 0;
void Check(bool pass, string message)
{
    assertions++;
    if (!pass) throw new Exception(message);
}
T View<T>(Transform parent = null) where T : Component, new()
{
    var go = new GameObject(); go.transform.SetParent(parent);
    return go.AddComponent<T>();
}
var screen = View<SkillsScreen>();
var row = View<SkillPanel>(screen.transform);
var picking = new Skill { Id = ESkillId.Lockpicking, Level = 25 };
var hacking = new Skill { Id = (ESkillId)200, Level = 51 };
PracticeSkillButton.Bind(row, picking, false);
var button = row.GetComponentInChildren<Button>();
Check(button && button.gameObject.activeSelf, "List practice button is visible");
PracticeSkillButton.Bind(row, picking, false);
PracticeSkillButton.Bind(row, hacking, false);
Check(row.GetComponentsInChildren<Button>(true).Length == 1, "Repeated Show/rebinding creates one button");
button.Click();
Check(PracticeController.OpenCount == 1 && ReferenceEquals(PracticeController.OpenedSkill, hacking),
    "A recycled row launches its newly bound skill exactly once");
PracticeSkillButton.Bind(row, new Skill { Id = ESkillId.Endurance }, false);
Check(!button.gameObject.activeSelf, "Recycling to an unrelated skill removes access");
PracticeSkillButton.Bind(row, picking, false);
Check(button.gameObject.activeSelf, "Recycling back restores the existing button");
PracticeSkillButton.Bind(row, null, false);
Check(!button.gameObject.activeSelf, "Missing bindings clear stale access safely");
PracticeSkillButton.Bind(row, picking, false);
picking.Locked = true;
GameObject.Invoke(row.GetComponent<PracticeSkillButton>(), "Update");
Check(!button.gameObject.activeSelf, "Lock changes hide the button");
picking.Locked = false;
PracticeController.InRaid = true;
GameObject.Invoke(row.GetComponent<PracticeSkillButton>(), "Update");
Check(!button.gameObject.activeSelf, "Entering a raid hides existing practice access");
PracticeController.InRaid = false;
PracticeController.AnyGameOpen = true;
GameObject.Invoke(row.GetComponent<PracticeSkillButton>(), "Update");
Check(button.gameObject.activeSelf && !button.interactable, "Another mini-game disables launch");
PracticeController.AnyGameOpen = false;

var nested = View<SkillIcon>(row.transform);
PracticeSkillButton.Bind(nested, picking, true);
Check(nested.GetComponent<PracticeSkillButton>() == null, "List-row icons do not receive duplicate practice buttons");
var foreign = View<SkillIcon>();
PracticeSkillButton.Bind(foreign, picking, true);
Check(foreign.GetComponent<PracticeSkillButton>() == null, "Icons outside Skills are untouched");

var thumbs = View<SkillThumbs>(screen.transform);
var grid = thumbs.gameObject.AddComponent<GridLayoutGroup>();
grid.spacing = new Vector2(24, 24);
grid.padding.bottom = 3;
var icon = View<SkillIcon>(thumbs.transform);
PracticeSkillButton.Bind(icon, hacking, true);
PracticeSkillButton.Bind(icon, hacking, true);
Check(icon.GetComponentsInChildren<Button>(true).Length == 1, "Icon binding also stays unique");
var spacing = thumbs.GetComponent<PracticeGridSpacing>();
GameObject.Invoke(spacing, "LateUpdate");
GameObject.Invoke(spacing, "LateUpdate");
Check(grid.spacing == new Vector2(24, 40) && grid.padding.bottom == 37, "Footer spacing does not accumulate each frame");
var footer = (RectTransform)icon.GetComponentInChildren<Button>().transform;
Check(footer.sizeDelta == new Vector2(96, 24) && footer.anchoredPosition == new Vector2(0, -9),
    "Footer sits below the native icon, badges and progress");
GameObject.Invoke(spacing, "OnDisable");
Check(grid.spacing == new Vector2(24, 24) && grid.padding.bottom == 3, "Closing the icon view restores its original layout");
GameObject.Invoke(spacing, "LateUpdate");
Check(grid.spacing.y == 40, "Reopening the grid restores footer room once");
PracticeSkillButton.Bind(icon, new Skill { Id = ESkillId.Endurance }, true);
GameObject.Invoke(spacing, "LateUpdate");
Check(grid.spacing.y == 24 && grid.padding.bottom == 3, "Filtering out all practice skills releases extra spacing");

var result = InputNode.ETranslateResult.Ignore;
Check(PracticeCommands.Prefix(ref result) && result == InputNode.ETranslateResult.Ignore, "Normal native input is untouched");
PracticeController.BlocksInput = true;
Check(!PracticeCommands.Prefix(ref result) && result == InputNode.ETranslateResult.BlockAll, "Practice consumes native commands");
foreach (var type in typeof(PracticeCommands).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(ModulePatch).IsAssignableFrom(t)))
{
    var methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    Check(methods.Any(m => m.IsDefined(typeof(PatchPrefixAttribute)) || m.IsDefined(typeof(PatchPostfixAttribute))),
        $"{type.Name} has a callback discoverable by installed SPT ModulePatch");
}

// Native level44 geometry was inspected read-only. Project its relevant rectangles
// at the requested resolutions; this tests overlap arithmetic, not live rendering.
foreach (var (width, height) in new[] { (1280, 720), (1920, 1080), (3440, 1440) })
{
    var scale = Math.Min(width / 1920f, height / 1080f);
    Check(560 * scale <= width && 408 * scale <= height, "Setup including Lock Picking coaching fits the requested screen");
    var listButton = (RectTransform)button.transform;
    var left = 824 + listButton.anchoredPosition.x - listButton.sizeDelta.x;
    var right = 824 + listButton.anchoredPosition.x;
    Check(left >= 674 && right <= 764, "List button clears native level text and progress number");
    Check(9 + footer.sizeDelta.y + 4 < 40 && 9 + footer.sizeDelta.y <= 34,
        "Icon footer clears the next row border and fits the bottom padding");
}
Console.WriteLine($"PASS {assertions} practice binding, native input hook and layout checks (offline substitutes; live rendering still required).");

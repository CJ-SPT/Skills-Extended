using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using SkillsExtended.Skills.Hacking;
using SkillsExtended.Skills.LockPicking;
using SkillsExtended.Skills.Signals;
using SkillsExtended.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkillsExtended.Skills.Practice;

internal sealed class PracticeController : MonoBehaviour
{
    internal static PracticeController Current { get; private set; }
    private static int _blockedThroughFrame = -1;
    internal static bool BlocksInput => Current || Time.frameCount <= _blockedThroughFrame;
    internal static bool AnyGameOpen => LockPickingGame.Current || HackingView.Current || SignalsView.Current;

    private readonly HackingUiInputState _input = new();
    private readonly PracticeSession _session = new();
    private readonly List<GraphicRaycaster> _raycasters = new();
    private PracticeSettings _settings;
    private SkillsScreen _screen;
    private Skill _skill;
    private GameObject _panel;
    private TMP_Text _level, _difficulty, _message;
    private Toggle _customize, _coaching;
    private Button _lessLevel, _moreLevel, _lessDifficulty, _moreDifficulty;
    private GraphicRaycaster _ownRaycaster;
    private int _ignoreEscapeThroughFrame;
    private bool _closed;

    internal static bool Available(Skill skill) => skill != null && PracticeSettings.Available(
        PracticeSettings.ForSkill(skill.Id), SkillsExtendedPlugin.SkillData, skill.Locked,
        SkillsExtendedInfo.IsFikaHeadless, Singleton<GameWorld>.Instantiated, GameUtils.IsInHideout());

    internal static void Open(SkillsScreen screen, Skill skill, TMP_FontAsset font)
    {
        if (Current || AnyGameOpen || !screen || !screen.isActiveAndEnabled || !Available(skill))
            return;
        var view = new GameObject("Skills Extended practice").AddComponent<PracticeController>();
        Current = view;
        view._screen = screen;
        view._skill = skill;
        view._settings = new PracticeSettings(PracticeSettings.ForSkill(skill.Id), skill.Level);
        view._ignoreEscapeThroughFrame = Time.frameCount;
        try
        {
            view.Build(font);
            view._input.Capture();
            view.BlockRaycasters();
            SceneManager.activeSceneChanged += view.SceneChanged;
            SceneManager.sceneUnloaded += view.SceneUnloaded;
        }
        catch (Exception error)
        {
            view.Close();
            SkillsExtendedPlugin.Log.LogError(error);
            ElectronicsRuntime.Notify("Could not open practice. See the Skills Extended log.");
        }
    }

    private void Build(TMP_FontAsset font)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        _ownRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        var shade = PracticeUi.Rect("Modal backdrop", transform, Vector2.zero, Vector2.zero);
        PracticeUi.Stretch(shade);
        PracticeUi.Box(shade, new Color(0, 0, 0, .75f));
        font = PracticeUi.DialogFont(font);
        var extra = _settings.Game == PracticeGame.LockPicking ? 24f : 0f;
        var panel = PracticeUi.Rect("Practice setup", transform, new Vector2(560, 360 + extra * 2), Vector2.zero);
        _panel = panel.gameObject;
        PracticeUi.Frame(panel);
        var header = PracticeUi.Rect("Caption Panel", panel, new Vector2(556, 28), new Vector2(0, 164 + extra));
        PracticeUi.Skin(header, "Caption Panel", new Color(.1f, .105f, .11f));
        LeftLabel(panel, _settings.Title.ToUpperInvariant() + " PRACTICE", font, new Vector2(490, 26), new Vector2(-11, 164 + extra), 20);
        PracticeUi.CloseButton(panel, font, new Vector2(259, 164 + extra), Close);
        LeftLabel(panel, "Practice only. No equipment required.\nNo XP, item costs, or world changes.", font,
            new Vector2(512, 46), new Vector2(0, 112 + extra), 17).color = PracticeUi.Muted;
        PracticeUi.Rule(panel, 82 + extra);
        if (_settings.Game != PracticeGame.Signals)
        {
            LeftLabel(panel, "Difficulty", font, new Vector2(260, 34), new Vector2(-126, 53 + extra), 18);
            _difficulty = PracticeUi.Label(panel, "", font, new Vector2(72, 32), new Vector2(164, 53 + extra), 18);
            _lessDifficulty = PracticeUi.Button(panel, "-", font, new Vector2(32, 30), new Vector2(108, 53 + extra), () => ChangeDifficulty(-1));
            _moreDifficulty = PracticeUi.Button(panel, "+", font, new Vector2(32, 30), new Vector2(220, 53 + extra), () => ChangeDifficulty(1));
        }
        else
        {
            LeftLabel(panel, "Scenario", font, new Vector2(150, 34), new Vector2(-181, 53 + extra), 18);
            var scenario = PracticeUi.Label(panel, "Receiver and pairing", font, new Vector2(332, 34), new Vector2(90, 53 + extra), 17);
            scenario.alignment = TextAlignmentOptions.MidlineRight;
        }
        _customize = PracticeUi.Checkbox(panel, "Customize skill level", font, new Vector2(0, 5 + extra), value =>
        {
            _settings.CustomizeLevel = value;
            Render();
        });
        LeftLabel(panel, "Skill level", font, new Vector2(260, 34), new Vector2(-126, -44 + extra), 18);
        _level = PracticeUi.Label(panel, "", font, new Vector2(72, 32), new Vector2(164, -44 + extra), 18);
        _lessLevel = PracticeUi.Button(panel, "-", font, new Vector2(32, 30), new Vector2(108, -44 + extra), () => ChangeLevel(-1));
        _moreLevel = PracticeUi.Button(panel, "+", font, new Vector2(32, 30), new Vector2(220, -44 + extra), () => ChangeLevel(1));
        LeftLabel(panel, "Uses your current level unless customized. Practice only.", font,
            new Vector2(512, 24), new Vector2(0, -78 + extra), 15).color = PracticeUi.Muted;
        if (_settings.Game == PracticeGame.LockPicking)
            _coaching = PracticeUi.Checkbox(panel, "Coaching hints and target guides", font, new Vector2(0, -92), value =>
            {
                _settings.ShowCoaching = value;
                Render();
            });
        _message = LeftLabel(panel, "", font, new Vector2(512, 34), new Vector2(0, -104 - extra), 14);
        _message.color = new Color(.85f, .55f, .4f);
        PracticeUi.Rule(panel, -125 - extra);
        PracticeUi.Button(panel, "BACK", font, new Vector2(150, 32), new Vector2(-181, -151 - extra), Close, 17);
        PracticeUi.Button(panel, "START PRACTICE", font, new Vector2(218, 32), new Vector2(147, -151 - extra), Launch, 17);
        Render();
    }

    private static TMP_Text LeftLabel(Transform parent, string text, TMP_FontAsset font,
        Vector2 size, Vector2 position, float fontSize)
    {
        var label = PracticeUi.Label(parent, text, font, size, position, fontSize);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }

    private void ChangeDifficulty(int delta) { _settings.SetDifficulty(_settings.Difficulty + delta); Render(); }
    private void ChangeLevel(int delta) { _settings.SetLevel(_settings.CustomLevel + delta); Render(); }

    private void Render()
    {
        if (_difficulty)
        {
            _difficulty.text = $"{_settings.Difficulty} / {_settings.MaxDifficulty}";
            _lessDifficulty.interactable = _settings.Difficulty > 1;
            _moreDifficulty.interactable = _settings.Difficulty < _settings.MaxDifficulty;
        }
        if (_coaching) _coaching.SetIsOnWithoutNotify(_settings.ShowCoaching);
        _customize.SetIsOnWithoutNotify(_settings.CustomizeLevel);
        _level.text = _settings.Level.ToString();
        _level.color = _settings.CustomizeLevel ? PracticeUi.Ink : PracticeUi.Muted;
        _lessLevel.interactable = _settings.CustomizeLevel && _settings.Level > 0;
        _moreLevel.interactable = _settings.CustomizeLevel && _settings.Level < 51;
    }

    private void Launch()
    {
        if (_session.Running)
            return;
        var seed = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
        // Keep the backdrop active under the game, but remove setup from hit testing.
        _panel.SetActive(false);
        try
        {
            if (_session.Start(Available(_skill), AnyGameOpen, () => StartGame(seed), GameAlive, CloseGame))
                return;
            _message.text = "Practice could not start. Check availability and installed assets.";
        }
        catch (Exception error)
        {
            SkillsExtendedPlugin.Log.LogError(error);
            _message.text = "Practice could not start. See the Skills Extended log.";
        }
        ShowSetup();
    }

    private void StartGame(int seed)
    {
        switch (_settings.Game)
        {
            case PracticeGame.LockPicking: LockPickingGame.Practice(_settings.Difficulty, _settings.Level, unchecked((uint)seed), _settings.ShowCoaching); break;
            case PracticeGame.Hacking: HackingView.Practice(_settings.Difficulty, _settings.Level, seed); break;
            case PracticeGame.Signals: SignalsView.Practice(_settings.Level, seed); break;
        }
        // The existing PDA prefab supplies its own canvas order. Put the practice instance
        // above our blocker without changing the prefab or any raid presentation.
        var view = GameObjectForPractice();
        if (view)
            view.GetComponent<Canvas>().sortingOrder = 31000;
    }

    private GameObject GameObjectForPractice() => _settings.Game switch
    {
        PracticeGame.LockPicking => LockPickingGame.Current ? LockPickingGame.Current.gameObject : null,
        PracticeGame.Hacking => HackingView.Current ? HackingView.Current.gameObject : null,
        PracticeGame.Signals => SignalsView.Current ? SignalsView.Current.gameObject : null,
        _ => null,
    };

    private bool GameAlive() => GameObjectForPractice();
    private void CloseGame()
    {
        switch (_settings.Game)
        {
            case PracticeGame.LockPicking: if (LockPickingGame.Current) LockPickingGame.Current.Close(); break;
            case PracticeGame.Hacking: if (HackingView.Current) HackingView.Current.Close(); break;
            case PracticeGame.Signals: if (SignalsView.Current) SignalsView.Current.Close(); break;
        }
    }

    private void ShowSetup()
    {
        _panel.SetActive(true);
        _ignoreEscapeThroughFrame = Time.frameCount;
        _input.Maintain();
    }

    private void Update()
    {
        if (_closed)
            return;
        if (!_screen || !_screen.isActiveAndEnabled || !Available(_skill))
        {
            Close();
            return;
        }
        if (_session.Returned())
        {
            _message.text = "";
            ShowSetup();
            return;
        }
        if (!_session.Running && Time.frameCount > _ignoreEscapeThroughFrame && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void LateUpdate()
    {
        if (_closed)
            return;
        BlockRaycasters();
        if (!_session.Running)
            _input.Maintain();
    }

    private void BlockRaycasters()
    {
        var practice = GameObjectForPractice();
        foreach (var raycaster in FindObjectsOfType<GraphicRaycaster>())
        {
            if (!raycaster.enabled || raycaster == _ownRaycaster
                || (practice && raycaster.transform.IsChildOf(practice.transform)))
                continue;
            if (!_raycasters.Contains(raycaster))
                _raycasters.Add(raycaster);
            raycaster.enabled = false;
        }
    }

    internal static void ScreenClosed(SkillsScreen screen)
    {
        if (Current && Current._screen == screen)
            Current.Close();
    }

    internal static void WorldStarting()
    {
        if (Current) Current.Close();
    }

    private void SceneChanged(Scene before, Scene after) => Close();
    private void SceneUnloaded(Scene scene) => Close();
    private void OnDisable() => Close();
    private void OnDestroy() => Close();

    private void Close()
    {
        if (_closed)
            return;
        _closed = true;
        SceneManager.activeSceneChanged -= SceneChanged;
        SceneManager.sceneUnloaded -= SceneUnloaded;
        try { _session.Close(); }
        finally
        {
            _input.Restore();
            foreach (var raycaster in _raycasters)
                if (raycaster) raycaster.enabled = true;
            _raycasters.Clear();
            _blockedThroughFrame = Time.frameCount;
            if (Current == this) Current = null;
            Destroy(gameObject);
        }
    }
}

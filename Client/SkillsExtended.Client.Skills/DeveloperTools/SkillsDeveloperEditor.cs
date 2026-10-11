using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFT;
using EFT.UI;
using Newtonsoft.Json;
using SkillsExtended.Config;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Hacking;
using SkillsExtended.Skills.LockPicking;
using SkillsExtended.Skills.Signals;
using SPT.Common.Http;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillsExtended.DeveloperTools;

[DefaultExecutionOrder(32000)]
public sealed class SkillsDeveloperEditor : MonoBehaviour
{
    public static SkillsDeveloperEditor Current { get; private set; }
    public bool IsOpen { get; private set; }
    public bool Looking => _cameraInput.Looking;
    private readonly DeveloperEditorCameraInput _cameraInput = new();
    private readonly IDeveloperEditorTool[] _tools = { new SignalCacheEditorTool(), new DoorEditorTool() };
    private readonly HackingInputState _input = new();
    private readonly HackingUiInputState _ui = new();
    private readonly List<(Renderer Renderer, bool Enabled)> _renderers = new();
    private DeveloperEditorView _view;
    private DeveloperEditorContext _context;
    private IDeveloperEditorTool _tool;
    private GameWorld _world;
    private Camera _camera;
    private Vector3 _savedPosition, _flyPosition;
    private Quaternion _savedRotation, _flyRotation;
    private CancellationTokenSource _operation;
    private bool _opening, _switching;
    private bool _checkingAuthorization;
    private float _nextAuthorizationCheck;
    private float _speed = 6;
    private bool OtherModal => SignalsView.Current || HackingView.IsOpen || LockPickingGame.Current
        || CommonUI.Instance?.InventoryScreen?.gameObject.activeInHierarchy == true
        || CommonUI.Instance?.MenuScreen?.gameObject.activeInHierarchy == true
        || CommonUI.Instance?.SettingsScreen?.gameObject.activeInHierarchy == true
        || CommonUI.Instance?.ChatScreen?.gameObject.activeInHierarchy == true
        || CommonUI.Instance?.HandbookScreen?.gameObject.activeInHierarchy == true
        || CommonUI.Instance?.WeaponModdingScreen?.gameObject.activeInHierarchy == true;

    private void Awake() { _world = GetComponent<GameWorld>(); Current = this; }
    public static void ToggleCurrent()
    {
        if (Current) Current.Toggle();
        else ElectronicsRuntime.Notify(LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.LoadAPmcRaidBeforeOpeningTheDeveloperEditor"));
    }
    private bool Eligible()
    {
        var p = _world ? _world.MainPlayer : null;
        return DeveloperEditorPolicy.Eligible(ConfigManager.DeveloperEditorEnabled?.Value == true,
            _world && !string.IsNullOrWhiteSpace(_world.LocationId),
            p && p.Side != EPlayerSide.Savage && p.HealthController.IsAlive,
            SkillsExtendedInfo.IsFikaHeadless);
    }
    private void Toggle()
    {
        if (IsOpen) { if (!_view.Typing) Close(); return; }
        if (!Eligible())
        { ElectronicsRuntime.Notify(LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.EnableDeveloperToolsThenLoadAPmcRaidFika")); return; }
        if (OtherModal || _opening) return;
        Open();
    }
    private async void Open()
    {
        _opening = true;
        try
        {
            Utils.GameUtils.HideConsole();
            var reply = await Post<DeveloperEditorReply>("/skills-extended/editor/session", new DeveloperEditorRequest { Map = _world.LocationId });
            if (!this || !Eligible() || OtherModal) return;
            if (!reply.Success) { ElectronicsRuntime.Notify(reply.Message); return; }
            _view ??= BuildView();
            _camera = Camera.main;
            if (!_camera) throw new InvalidOperationException(LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.RaidCameraIsNotReady"));
            _savedPosition = _camera.transform.position; _savedRotation = _camera.transform.rotation;
            _flyPosition = _world.MainPlayer.CameraPosition.position;
            _flyRotation = Quaternion.LookRotation(_world.MainPlayer.LookDirection);
            _operation = new CancellationTokenSource();
            _context = new DeveloperEditorContext
            {
                World = _world, Runner = this, View = _view, Raid = reply.Raid,
                Camera = () => _camera, IsOpen = () => IsOpen,
                Cancellation = () => _operation?.Token ?? new CancellationToken(true),
                Frame = target => { _flyPosition = target + new Vector3(0, 2, -4); _flyRotation = Quaternion.LookRotation(target - _flyPosition); },
            };
            IsOpen = true;
            Helpers.AutomaticCollection.SetEditorActive(true);
            _nextAuthorizationCheck = Time.realtimeSinceStartup + 5;
            _input.Capture(true); _ui.Capture();
            foreach (var renderer in _camera.GetComponentsInChildren<Renderer>(true))
            { _renderers.Add((renderer, renderer.enabled)); renderer.enabled = false; }
            Camera.onPreCull += CameraPose;
            _view.SetVisible(true);
            await SwitchTool(_tool ?? _tools.First(t => t.Supports(_world.LocationId)));
        }
        catch (Exception e) { Fail(e); }
        finally { _opening = false; }
    }
    private DeveloperEditorView BuildView()
    {
        var view = new DeveloperEditorView();
        view.Failed = Fail;
        view.Escape = Escape;
        view.Separator(view.Toolbar);
        foreach (var tool in _tools)
        {
            var button = view.Button(view.Toolbar, tool.Title, async () =>
            { try { await SwitchTool(tool); } catch (Exception e) { Fail(e); } });
            button.userData = tool;
            button.SetEnabled(tool.Supports(_world.LocationId));
        }
        view.Separator(view.Toolbar);
        var speed = view.Number(view.Toolbar, LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.Speed"), _speed, .25f, 96, n => _speed = n);
        speed.style.width = speed.style.minWidth = speed.style.maxWidth = 124;
        speed.style.height = speed.style.minHeight = speed.style.maxHeight = 28;
        speed.style.flexGrow = 0; speed.style.flexShrink = 0;
        speed.style.alignSelf = Align.Center;
        speed.style.marginTop = speed.style.marginBottom = 0;
        speed.labelElement.style.width = speed.labelElement.style.minWidth = speed.labelElement.style.maxWidth = 44;
        speed.labelElement.style.marginLeft = 0; speed.labelElement.style.marginRight = 6;
        speed.labelElement.style.paddingLeft = speed.labelElement.style.paddingRight = 0;
        speed.labelElement.style.alignSelf = Align.Center;
        var speedInput = speed.Q(className: "unity-base-text-field__input");
        speedInput.style.minWidth = 0; speedInput.style.flexGrow = 1; speedInput.style.flexShrink = 1;
        view.Button(view.Toolbar, LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.FrameF"), () => _tool?.Frame());
        view.Separator(view.Toolbar);
        view.Button(view.Toolbar, LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.Close"), Close);
        view.Search.RegisterValueChangedCallback(_ => _tool?.RefreshList());
        return view;
    }
    private async Task SwitchTool(IDeveloperEditorTool tool)
    {
        if (!IsOpen || _switching || _tool?.Busy == true || !tool.Supports(_world.LocationId)) return;
        _switching = true;
        try
        {
            _view.DismissMenu(); _view.BrowserControls.Clear();
            _tool?.Deactivate(); _view.Markers.Clear(); _view.Inspector.Clear(); _view.List.Clear(); _view.Actions.Clear();
            _tool = tool; _view.Search.SetValueWithoutNotify("");
            foreach (var tab in _view.Toolbar.Children().OfType<Button>())
                tab.EnableInClassList("editor-selected", ReferenceEquals(tab.userData, tool));
            _context.Status(LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.AiAndRaidTimeContinueRmbWasdQeFly", tool.Title));
            await _tool.Activate(_context);
            if (IsOpen) _tool.BuildActions(_view.Actions);
        }
        finally { _switching = false; }
    }
    private void Update()
    {
        try
        {
            if (ConfigManager.DeveloperEditorShortcut?.Value.IsDown() == true && _view?.Typing != true) Toggle();
            if (!IsOpen) return;
            if (!Eligible() || OtherModal) { Close(); return; }
            if (SkillsExtendedInfo.IsFikaPresent && !_checkingAuthorization
                && Time.realtimeSinceStartup >= _nextAuthorizationCheck)
                CheckAuthorization();
            _view.Tick();
            var dirty = false;
            for (var i = 0; i < _tools.Length; i++) dirty |= _tools[i].Dirty;
            _view.DraftState(dirty ? LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.UnsavedDrafts") : LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.DraftsSaved"));
            _view.Actions.SetEnabled(!_switching && !_tool.Busy);
            _view.Inspector.SetEnabled(!_switching && !_tool.Busy);
            if (!Application.isFocused) { _cameraInput.Reset(); _tool.Cancel(); _ui.Maintain(); return; }
            if (Input.GetKeyDown(KeyCode.Escape) && !_view.ConsumedEscape)
            { if (_view.MenuOpen) _view.DismissMenu(); else if (_view.Typing) _view.CancelTyping(); else Escape(); return; }
            if (_view.Typing || _view.Confirming || _view.MenuOpen || _view.MenuDismissed || _switching || _tool.Busy)
            { _cameraInput.Reset(); _ui.Maintain(); _tool.Tick(); return; }
            _cameraInput.Update(Input.GetMouseButton(1), _view.PointerOverPanel);
            _ui.Maintain(Looking);
            if (Looking)
            {
                var angles = _flyRotation.eulerAngles;
                angles.x -= Input.GetAxis("Mouse Y") * 2; angles.y += Input.GetAxis("Mouse X") * 2;
                _flyRotation = Quaternion.Euler(angles);
                var direction = _flyRotation * new Vector3((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0), 0,
                    (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
                direction.y += (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
                _flyPosition += Vector3.ClampMagnitude(direction, 1) * Time.unscaledDeltaTime * _speed
                    * (Input.GetKey(KeyCode.LeftShift) ? 4 : Input.GetKey(KeyCode.LeftControl) ? .25f : 1);
            }
            else
            {
                var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (ctrl && Input.GetKeyDown(KeyCode.Z)) _tool.Undo(false);
                if (ctrl && Input.GetKeyDown(KeyCode.Y)) _tool.Undo(true);
                if (Input.GetKeyDown(KeyCode.F)) _tool.Frame();
                _tool.WorldInput(_camera.ScreenPointToRay(Input.mousePosition));
            }
            _tool.Tick();
        }
        catch (Exception e) { Fail(e); }
    }
    private void LateUpdate() { if (IsOpen && _camera) _camera.transform.SetPositionAndRotation(_flyPosition, _flyRotation); }
    private async void CheckAuthorization()
    {
        _checkingAuthorization = true;
        _nextAuthorizationCheck = Time.realtimeSinceStartup + 5;
        var operation = _operation;
        try
        {
            var reply = await Post<DeveloperEditorReply>("/skills-extended/editor/session",
                new DeveloperEditorRequest { Map = _world.LocationId });
            if (this && IsOpen && operation == _operation && !reply.Success)
            {
                Close();
                ElectronicsRuntime.Notify(reply.Message);
            }
        }
        catch (Exception e)
        {
            if (this && IsOpen && operation == _operation) Fail(e);
        }
        finally { _checkingAuthorization = false; }
    }
    private void CameraPose(Camera camera) { if (IsOpen && camera == _camera) camera.transform.SetPositionAndRotation(_flyPosition, _flyRotation); }
    private void Escape() { if (_tool?.Cancel() != true) Close(); }
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false; _cameraInput.Reset();
        _operation?.Cancel();
        try { _tool?.Deactivate(); }
        finally
        {
            Camera.onPreCull -= CameraPose;
            if (_camera) _camera.transform.SetPositionAndRotation(_savedPosition, _savedRotation);
            foreach (var entry in _renderers) if (entry.Renderer) entry.Renderer.enabled = entry.Enabled;
            _renderers.Clear();
            _input.Restore(); _ui.Restore(); _view?.SetVisible(false);
            _camera = null; _operation?.Dispose(); _operation = null;
            Helpers.AutomaticCollection.SetEditorActive(false);
        }
    }
    private void Fail(Exception e)
    { SkillsExtendedPlugin.Log.LogError(LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.DeveloperEditor", e)); Close(); ElectronicsRuntime.Notify(LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.DeveloperEditor", e.Message)); }
    private void OnDestroy()
    {
        Close(); foreach (var tool in _tools) tool.Dispose(); _view?.Dispose();
        if (Current == this) Current = null;
    }
    internal static async Task<T> Post<T>(string path, object request) where T : class =>
        Helpers.ConfigurationJson.Deserialize<T>(await RequestHandler.PostJsonAsync(path, JsonConvert.SerializeObject(request)))
        ?? throw new InvalidOperationException(LocalizedText.Get("SkillsExtended.SkillsDeveloperEditor.EmptyDeveloperEditorResponse"));
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Console.Core;
using EFT.UI;
using SkillsExtended.Hacking;
using SkillsExtended.Hacking.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkillsExtended.Skills.Hacking;

/// <summary>Presentation only: every raid action is resolved by ElectronicsRuntime's authority.</summary>
public sealed class HackingView : MonoBehaviour
{
    private static AssetBundle _bundle;
    private static GameObject _prefab;
    public static HackingView Current { get; private set; }
    public static bool IsOpen => Current;
    public bool InRaid => !_practice && _player;

    private static readonly Dictionary<string, Sprite> Sprites = new();

    // Asset access is independent of either skill being enabled or owning the screen.
    internal static T PdaAsset<T>(string name)
        where T : UnityEngine.Object
    {
        if (!_bundle)
            _bundle = AssetBundle.LoadFromFile(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "bundles",
                    "electronics_ui.bundle"
                )
            );
        return _bundle ? _bundle.LoadAsset<T>(name) : null;
    }

    public static Sprite Icon(string name)
    {
        if (Sprites.TryGetValue(name, out var sprite) && sprite)
        {
            return sprite;
        }

        if (!Prepare())
        {
            return null;
        }

        sprite = _bundle.LoadAsset<Sprite>(name);
        if (sprite)
        {
            Sprites[name] = sprite;
        }

        return sprite;
    }

    private HackBoard _board;
    private HackReply _reply;
    private ElectronicsRuntime _runtime;
    private Player _player;
    private bool _practice;
    private bool _closing;
    private bool _waiting;
    private bool _captured;
    private readonly HackingInputState _inputState = new();
    private readonly HackingUiInputState _uiInputState = new();
    private static readonly Dictionary<string, AudioClip> Sounds = new();
    private int _selected = -1;
    private int _level;
    private int _hoveredNode = -1;
    private uint _seed;
    private Button[] _nodes;
    private Button[] _utilities;
    private readonly List<(int a, int b, Image image)> _edges = new();
    private TMP_Text _header;
    private TMP_Text _stats;
    private TMP_Text _message;
    private TMP_Text _tooltip;
    private RectTransform _boardRoot;
    private Material[] _gaugeMaterials;
    private HackingAudio _audio;
    private ElectronicsBoardFx _fx;
    private readonly HackingPresentation _presentation = new();
    private bool _visualBoot;
    private float _finishAt = -1;
    private int _soundTurn;
    private int _soundShield;
    private string _pendingCue = "reveal";
    private bool _abortSound;
    private static readonly Color Cyan = new(.24f, .87f, .9f);
    private static readonly Color Amber = new(1f, .58f, .2f);
    private static readonly Color Dark = new(.12f, .15f, .17f);

    public static bool Prepare()
    {
        if (Signals.SignalsView.Current || LockPicking.LockPickingGame.Current)
            return false;
        if (SkillsExtendedInfo.IsFikaHeadless)
        {
            return false;
        }

        try
        {
            if (!_bundle)
            {
                _bundle = AssetBundle.LoadFromFile(
                    Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "bundles",
                        "electronics_ui.bundle"
                    )
                );
            }

            if (!_prefab && _bundle)
            {
                _prefab = _bundle.LoadAsset<GameObject>("ElectronicsUi");
            }

            if (
                !_prefab
                || !_prefab.GetComponentsInChildren<TMP_Text>(true).All(t => t.font != null)
            )
            {
                return false;
            }

            foreach (var name in HackingAudio.Names)
            {
                if (!Sounds.TryGetValue(name, out var clip))
                {
                    clip = _bundle.LoadAsset<AudioClip>(name);
                    Sounds[name] = clip;
                }

                if (!clip || (clip.loadState != AudioDataLoadState.Loaded && !clip.LoadAudioData()))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception e)
        {
            SkillsExtendedPlugin.Log.LogError(e);
            return false;
        }
    }

    public static void ShowRaid(ElectronicsRuntime runtime, Player player, HackReply reply)
    {
        try
        {
            var view = Create(false, player);
            view._runtime = runtime;
            view._reply = reply;
            view._board = reply.Board;
            view.BuildBoard();
            view.Render();
        }
        catch (Exception e)
        {
            Current?.Close();
            SkillsExtendedPlugin.Log.LogError(e);
            runtime.Send(
                new HackRequest
                {
                    Raid = reply.Raid,
                    Actor = reply.Actor,
                    Door = reply.Door,
                    Attempt = reply.Attempt,
                    Sequence = reply.Sequence + 1,
                    Operation = "ui-error",
                }
            );
        }
    }

    public static void Practice(int difficulty, int level, int seed)
    {
        if (IsOpen || LockPicking.LockPickingGame.Current || Signals.SignalsView.Current)
        {
            return;
        }

        if (Utils.GameUtils.IsInRaid())
        {
            ElectronicsRuntime.Notify(LocalizedText.Get("SkillsExtended.HackingView.PracticeIsAvailableOutsideRaids"));
            return;
        }

        if (difficulty < 1 || difficulty > 3 || level < 0 || level > 51)
        {
            ElectronicsRuntime.Notify(LocalizedText.Get("SkillsExtended.HackingView.UsageHackingDifficulty13Level051Seed"));
            return;
        }

        if (!Prepare())
        {
            ElectronicsRuntime.Notify(LocalizedText.Get("SkillsExtended.HackingView.MissingOrInvalidElectronicsUiBundle"));
            return;
        }

        try
        {
            Utils.GameUtils.HideConsole();
            var view = Create(true);
            view._level = level;
            view._seed = unchecked((uint)seed);
            view._board = HackingEngine.StartAttempt(
                ElectronicsRuntime.Config,
                difficulty,
                level,
                view._seed
            );
            view.BuildBoard();
            view.Render();
        }
        catch (Exception e)
        {
            Current?.Close();
            SkillsExtendedPlugin.Log.LogError(e);
        }
    }

    private static HackingView Create(bool practice, Player player = null)
    {
        var go = Instantiate(_prefab);
        var view = go.AddComponent<HackingView>();
        Current = view;
        view._practice = practice;
        view._player = player;
        view._inputState.Capture(!practice && player);
        view._captured = true;
        view._uiInputState.Capture();
        view._header = view.Text("Header");
        view.BuildPdaFrame();
        view._stats = view.Text("Stats");
        view._message = view.Text("Message");
        view._tooltip = view.Text("Tooltip");
        view._tooltip.text = LocalizedText.Get("SkillsExtended.HackingView.HoverOverANodeForDetails");
        view._boardRoot = (RectTransform)view.transform.Find("Panel/Board");
        view._gaugeMaterials = ElectronicsUiVisuals.CreateGaugeMaterials(
            view.transform.Find("Panel")
        );
        view.transform.Find("Panel/Abort").GetComponent<Button>().onClick.AddListener(view.Abort);
        view.transform.Find("Panel/Retry").GetComponent<Button>().onClick.AddListener(view.Retry);
        var audioObject = new GameObject("Hacking audio");
        audioObject.transform.SetParent(go.transform, false);
        view._audio = audioObject.AddComponent<HackingAudio>();
        view._audio.Initialize(Sounds);
        go.SetActive(true);
        return view;
    }

    private TMP_Text Text(string name) => transform.Find("Panel/" + name).GetComponent<TMP_Text>();

    private void BuildPdaFrame()
    {
        var panel = (RectTransform)transform.Find("Panel");
        var frame = HackingPdaFrame.Build(transform, panel.sizeDelta, _header.font, Abort);
        frame.SetSiblingIndex(panel.GetSiblingIndex());
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
        panel.anchoredPosition = HackingPdaFrame.ScreenPosition;
        var outline = panel.GetComponent<Outline>();
        if (outline)
            outline.enabled = false;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = HackingPdaFrame.ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        transform.Find("Backdrop").GetComponent<Image>().color = new Color(0, 0, 0, .66f);
    }

    private void Sound(string name, bool terminal = false) => _audio.Play(name, terminal);

    private void ActionSound()
    {
        if (_board.Status != HackStatus.Active)
        {
            if (_board.Status == HackStatus.Won)
            {
                Sound("success", true);
            }
            else if (_board.Status == HackStatus.Lost)
            {
                Sound("failure", true);
            }
            else if (_board.Status == HackStatus.Aborted && !_abortSound)
            {
                Sound("abort", true);
            }
        }
        else if (_board.Turn > _soundTurn)
        {
            Sound(_pendingCue);
            if (_soundShield > 0 && _board.ShieldCharges == 0)
            {
                Sound("shield-off");
            }
        }

        _soundTurn = _board.Turn;
        _soundShield = _board.ShieldCharges;
    }

    private Vector2 Position(HackNode n)
    {
        var xs = _board.Nodes.Select(x => x.Q + x.R * .5f).ToArray();
        var ys = _board.Nodes.Select(x => x.R * ElectronicsUiVisuals.RowSpacing).ToArray();
        var scale = Math.Min(
            130,
            Math.Min(
                1080 / Math.Max(1, xs.Max() - xs.Min()),
                520 / Math.Max(1, ys.Max() - ys.Min())
            )
        );
        return new Vector2(
            (n.Q + n.R * .5f - (xs.Max() + xs.Min()) / 2) * scale,
            -(n.R * ElectronicsUiVisuals.RowSpacing - (ys.Max() + ys.Min()) / 2) * scale
        );
    }

    private void BuildBoard()
    {
        if (_fx)
        {
            _fx.enabled = false;
            Destroy(_fx);
        }

        foreach (Transform child in _boardRoot)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        _presentation.Reset();
        _visualBoot = true;
        _finishAt = -1;
        _hoveredNode = -1;
        _edges.Clear();
        var xs = _board.Nodes.Select(n => n.Q + n.R * .5f).ToArray();
        var ys = _board.Nodes.Select(n => n.R * ElectronicsUiVisuals.RowSpacing).ToArray();
        var scale = Math.Min(
            130,
            Math.Min(
                1080 / Math.Max(1, xs.Max() - xs.Min()),
                520 / Math.Max(1, ys.Max() - ys.Min())
            )
        );
        ElectronicsUiVisuals.AlignBackground(
            (RectTransform)transform.Find("Panel/NetworkBackgroundViewport/NetworkBackground"),
            scale,
            (xs.Max() + xs.Min()) / 2,
            (ys.Max() + ys.Min()) / 2,
            (int)Math.Round(_board.Nodes.Average(n => n.Q)),
            (int)Math.Round(_board.Nodes.Average(n => n.R))
        );
        var template = transform.Find("NodeTemplate").gameObject;
        foreach (var n in _board.Nodes)
        {
            foreach (var neighbor in n.Neighbors.Where(i => i > n.Id))
            {
                var edge = new GameObject("Connection", typeof(RectTransform), typeof(Image));
                edge.transform.SetParent(_boardRoot, false);
                var rect = (RectTransform)edge.transform;
                var a = Position(n);
                var b = Position(_board.Nodes[neighbor]);
                rect.anchoredPosition = (a + b) / 2;
                rect.sizeDelta = new Vector2(Math.Max(1, Vector2.Distance(a, b) - 42), 1.5f);
                rect.localRotation = Quaternion.Euler(
                    0,
                    0,
                    Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg
                );
                edge.GetComponent<Image>().color = new Color(.28f, .32f, .33f);
                edge.GetComponent<Image>().raycastTarget = false;
                ElectronicsUiVisuals.SmoothConnection(
                    edge.GetComponent<Image>(),
                    _bundle.LoadAsset<Material>("ElectronicsLine")
                );
                _edges.Add((n.Id, neighbor, edge.GetComponent<Image>()));
            }
        }

        _nodes = _board
            .Nodes.Select(n =>
            {
                var go = Instantiate(template, _boardRoot);
                go.name = "Node_" + n.Id;
                go.SetActive(true);
                ((RectTransform)go.transform).anchoredPosition = Position(n);
                var button = go.GetComponent<Button>();
                button.onClick.AddListener(() => Select(n.Id));
                var trigger = go.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entry.callback.AddListener(_ =>
                {
                    _hoveredNode = n.Id;
                    _tooltip.text = Description(_board.Nodes[n.Id]);
                    if (_fx)
                    {
                        _fx.Hover(n.Id, true);
                    }
                });
                trigger.triggers.Add(entry);
                var leave = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                leave.callback.AddListener(_ =>
                {
                    _hoveredNode = -1;
                    _tooltip.text = LocalizedText.Get("SkillsExtended.HackingView.HoverOverANodeForDetails");
                    if (_fx)
                    {
                        _fx.Hover(n.Id, false);
                    }
                });
                trigger.triggers.Add(leave);
                return button;
            })
            .ToArray();
        _fx = _boardRoot.gameObject.AddComponent<ElectronicsBoardFx>();
        _fx.Initialize(
            _boardRoot,
            _nodes.Select(n => n.gameObject).ToArray(),
            _edges.Select(e => new ElectronicsBoardFx.Connection(e.a, e.b, e.image)).ToArray(),
            Icon
        );
        _utilities = Enumerable
            .Range(0, 4)
            .Select(i =>
            {
                var b = transform.Find("Panel/Utility" + i).GetComponent<Button>();
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => Utility(i));
                return b;
            })
            .ToArray();
    }

    private string Description(HackNode n)
    {
        if (!n.Revealed)
        {
            return LocalizedText.Get("SkillsExtended.HackingView.UnknownNodeExploreAlongAnAccessibleConnection");
        }

        return n.Kind switch
        {
            NodeKind.Empty => n.Clue == 0
                ? LocalizedText.Get("SkillsExtended.HackingView.NetworkEntrance")
                : LocalizedText.Get("SkillsExtended.HackingView.DistanceLinksToTheNearestRemainingCoreUncollectedUtility", n.Clue, (n.Clue == 5 ? "+" : "")),
            NodeKind.Core => LocalizedText.Get("SkillsExtended.HackingView.SecurityCoreDestroyItToUnlockTheDoor"),
            NodeKind.Firewall => LocalizedText.Get("SkillsExtended.HackingView.FirewallBlocksNeighboringNodesAttackBeforeItRetaliates"),
            NodeKind.Antivirus => LocalizedText.Get("SkillsExtended.HackingView.AntivirusFragileButStrikesHardBlocksNeighboringNodes"),
            NodeKind.Restoration =>
                LocalizedText.Get("SkillsExtended.HackingView.RestorationRepairsAnotherRevealedDefenseBy10EachTurn"),
            NodeKind.Suppressor =>
                LocalizedText.Get("SkillsExtended.HackingView.SuppressorReducesYourStrengthBy5WhileActiveMinimum"),
            NodeKind.SelfRepair =>
                LocalizedText.Get("SkillsExtended.HackingView.SelfRepair8CoherenceEachTurnFor3Turns"),
            NodeKind.KernelRot =>
                LocalizedText.Get("SkillsExtended.HackingView.KernelRotHalveARevealedTargetSCurrentCoherence"),
            NodeKind.Shield => LocalizedText.Get("SkillsExtended.HackingView.PolymorphicShieldPreventsTheNext2Retaliations"),
            NodeKind.Vector =>
                LocalizedText.Get("SkillsExtended.HackingView.SecondaryVector20DamagePerTurnFor3Turns"),
            _ =>
                LocalizedText.Get("SkillsExtended.HackingView.DataCacheOpenToExposeAUtilityOrDefense"),
        };
    }

    private static string Name(NodeKind kind) =>
        kind switch
        {
            NodeKind.SelfRepair => LocalizedText.Get("SkillsExtended.HackingView.Repair"),
            NodeKind.KernelRot => LocalizedText.Get("SkillsExtended.HackingView.Rot"),
            NodeKind.Shield => LocalizedText.Get("SkillsExtended.HackingView.Shield"),
            NodeKind.Vector => LocalizedText.Get("SkillsExtended.HackingView.Vector"),
            _ => kind.ToString(),
        };

    private void Render()
    {
        var changes = _presentation.Observe(_board);
        if (_hoveredNode >= 0 && _hoveredNode < _board.Nodes.Count)
        {
            _tooltip.text = Description(_board.Nodes[_hoveredNode]);
        }

        transform.Find("Panel/Abort").GetComponentInChildren<TMP_Text>().text = LocalizedText.Get("SkillsExtended.HackingView.Abort");
        transform.Find("Panel/Retry").GetComponentInChildren<TMP_Text>().text = LocalizedText.Get("SkillsExtended.HackingView.Retry");
        Text("UtilityHeading").text = LocalizedText.Get("SkillsExtended.HackingView.UtilityHeading");
        Text("Rules").text = LocalizedText.Get("SkillsExtended.HackingView.Rules");
        _header.text = LocalizedText.Get("SkillsExtended.HackingView.TerragroupSecuritySystem");
        _stats.text = LocalizedText.Get("SkillsExtended.HackingView.Coherence", _board.Coherence, _board.MaximumCoherence);
        ElectronicsUiVisuals.SetCoherence(
            _gaugeMaterials,
            (float)_board.Coherence / _board.MaximumCoherence
        );
        Text("Strength").text = LocalizedText.Get("SkillsExtended.HackingView.Strength2", _board.Strength);
        _message.text =
            _board.Status == HackStatus.Active
                ? (
                    _selected >= 0
                        ? LocalizedText.Get("SkillsExtended.HackingView.SelectARevealedDefenseOrCoreAsTheUtility")
                        : (
                            _practice
                                ? LocalizedText.Get("SkillsExtended.HackingView.PracticeLevelNoXpTurn", _level, _board.Turn)
                                : LocalizedText.Get("SkillsExtended.HackingView.AttemptsRemainingTurnRaidIsLive", _runtime.Remaining(_reply.Door), _board.Turn)
                        )
                )
                : LocalizedText.Get("SkillsExtended.HackingView.Attempt", LocalizedText.Get("SkillsExtended.Hacking.Status." + _board.Status).ToUpperInvariant());
        foreach (var edge in _edges)
        {
            var a = _board.Nodes[edge.a];
            var b = _board.Nodes[edge.b];
            var color =
                a.Cleared && b.Cleared ? Amber
                : a.Cleared && _board.CanSelect(b.Id) || b.Cleared && _board.CanSelect(a.Id)
                    ? new Color(.16f, .4f, .4f)
                : new Color(.12f, .18f, .19f);
            _fx.SetConnectionColor(edge.a, edge.b, color);
        }

        foreach (var n in _board.Nodes)
        {
            var b = _nodes[n.Id];
            var legal = _board.CanSelect(n.Id);
            b.interactable = legal && !_waiting;
            ElectronicsUiVisuals.DrawNode(
                b.gameObject,
                (int)n.Kind,
                n.Revealed,
                n.Cleared,
                legal && !_waiting,
                n.Coherence,
                n.Strength,
                n.Clue,
                name =>
                    Icon(
                        name == "cpu"
                            ? _board.Difficulty == 1
                                ? "core-standard"
                                : _board.Difficulty == 2
                                    ? "core-secure"
                                    : "core-hardened"
                            : name
                    )
            );
            _fx.SetAvailable(n.Id, legal && !_waiting);
        }

        for (var i = 0; i < _utilities.Length; i++)
        {
            var b = _utilities[i];
            b.gameObject.SetActive(i < _board.Slots && _board.Status == HackStatus.Active);
            var utilityRect = (RectTransform)b.transform;
            utilityRect.anchoredPosition = new Vector2(
                (i - (_board.Slots - 1) * .5f) * 104,
                utilityRect.anchoredPosition.y
            );
            b.interactable =
                i < _board.Utilities.Count && _board.Status == HackStatus.Active && !_waiting;
            b.GetComponentInChildren<TMP_Text>().text =
                LocalizedText.Get("SkillsExtended.HackingView.UtilitySlot", i + 1, (i < _board.Utilities.Count ? Name(_board.Utilities[i]) : LocalizedText.Get("SkillsExtended.HackingView.Empty")));
            b.GetComponent<Image>().color = i == _selected ? Amber : Color.white;
            var icon = b.transform.Find("Icon").GetComponent<Image>();
            icon.enabled = i < _board.Utilities.Count;
            if (icon.enabled)
            {
                icon.sprite = Icon(ElectronicsUiVisuals.Icons[(int)_board.Utilities[i]]);
            }
        }

        transform
            .Find("Panel/Retry")
            .gameObject.SetActive(_practice && _board.Status != HackStatus.Active);
        var low =
            _board.Status == HackStatus.Active && _board.Coherence <= _board.MaximumCoherence / 4;
        _audio.LowCoherence(low);
        Animate(changes);
    }

    private void Animate(HackingVisualChanges changes)
    {
        _fx.Sample(Time.unscaledTime);
        if (_visualBoot)
        {
            _visualBoot = false;
            var entrance = _board.Nodes.FirstOrDefault(n => n.Cleared);
            if (entrance != null)
            {
                _fx.Reveal(entrance.Id);
                foreach (var neighbor in entrance.Neighbors.Where(_board.CanSelect))
                {
                    _fx.OpenPath(entrance.Id, neighbor, false);
                }
            }
        }

        foreach (var change in changes.Nodes)
        {
            switch (change.Motion)
            {
                case NodeMotion.Reveal:
                    _fx.Reveal(change.Node);
                    break;
                case NodeMotion.Destroy:
                    var kind = (int)_board.Nodes[change.Node].Kind;
                    _fx.DestroyNode(
                        change.Node,
                        kind > 0 ? Icon(ElectronicsUiVisuals.Icons[kind]) : null
                    );
                    break;
                case NodeMotion.Damage:
                    _fx.Damage(change.Node);
                    break;
                case NodeMotion.Repair:
                    _fx.Repair(change.Node);
                    break;
                case NodeMotion.Available:
                    _fx.Available(change.Node);
                    break;
                case NodeMotion.Blocked:
                    _fx.Blocked(change.Node);
                    break;
            }
        }

        foreach (var path in changes.Paths)
        {
            _fx.OpenPath(path.From, path.To, path.Claimed);
        }

        if (changes.Won || changes.Lost)
        {
            _fx.Finish(
                changes.Won,
                (
                    _board.Nodes.FirstOrDefault(n => n.Kind == NodeKind.Core)
                    ?? _board.Nodes.First(n => n.Revealed)
                ).Id
            );
        }
    }

    private void Select(int id)
    {
        if (_waiting || !_board.CanSelect(id))
        {
            return;
        }

        _fx.Click(id);
        if (_selected >= 0)
        {
            Command("utility", id, _selected);
        }
        else
        {
            Command("node", id, -1);
        }
    }

    private void Utility(int slot)
    {
        if (_waiting || slot >= _board.Utilities.Count)
        {
            return;
        }

        var kind = _board.Utilities[slot];
        if (kind == NodeKind.Vector || kind == NodeKind.KernelRot)
        {
            _selected = _selected == slot ? -1 : slot;
            Render();
        }
        else
        {
            Command("utility", -1, slot);
        }
    }

    private void Command(string operation, int node = -1, int slot = -1)
    {
        _pendingCue =
            operation == "utility"
                ? (
                    slot >= 0
                    && slot < _board.Utilities.Count
                    && _board.Utilities[slot] == NodeKind.Shield
                        ? "shield-on"
                        : "utility"
                )
            : node >= 0 && node < _board.Nodes.Count && _board.Nodes[node].Revealed
                ? (_board.Nodes[node].Kind == NodeKind.Cache ? "cache" : "attack")
            : "reveal";
        if (_practice)
        {
            var valid =
                operation == "node" ? _board.SelectNode(node) : _board.UseUtility(slot, node);
            if (valid)
            {
                _selected = -1;
                ActionSound();
            }

            Render();
            return;
        }

        _waiting = true;
        _runtime.Send(
            new HackRequest
            {
                Raid = _reply.Raid,
                Actor = _reply.Actor,
                Door = _reply.Door,
                Attempt = _reply.Attempt,
                Sequence = _reply.Sequence + 1,
                Operation = operation,
                Node = node,
                Slot = slot,
            }
        );
    }

    public void Receive(HackReply reply)
    {
        if (_reply == null || reply.Attempt != _reply.Attempt || reply.Sequence < _reply.Sequence)
        {
            return;
        }

        _waiting = false;
        if (reply.Board == null)
        {
            Render();
            return;
        }

        _reply = reply;
        _board = reply.Board;
        _selected = -1;
        ActionSound();
        Render();
        if (_board.Status != HackStatus.Active)
        {
            ElectronicsRuntime.Notify(LocalizedText.Get("SkillsExtended.HackingView.Pda", LocalizedText.Get("SkillsExtended.Hacking.Status." + _board.Status)));
            if (_board.Status == HackStatus.Won || _board.Status == HackStatus.Lost)
            {
                _finishAt = Time.unscaledTime + .95f;
            }
            else
            {
                Close();
            }
        }
    }

    private void Retry()
    {
        _board = HackingEngine.StartAttempt(
            ElectronicsRuntime.Config,
            _board.Difficulty,
            _level,
            ++_seed
        );
        _selected = -1;
        _soundTurn = _soundShield = 0;
        _audio.Restart();
        BuildBoard();
        Render();
    }

    public void Abort()
    {
        if (_closing)
        {
            return;
        }

        if (_board?.Status == HackStatus.Active)
        {
            _abortSound = true;
            Sound("abort", true);
        }

        if (!_practice && _board?.Status == HackStatus.Active)
        {
            _runtime.DismissAttempt(_reply.Attempt);
            Command("abort");
        }

        Close();
    }

    private void Update()
    {
        if (_closing || _board == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Abort();
            return;
        }

        if (_finishAt >= 0 && Time.unscaledTime >= _finishAt)
        {
            Close();
            return;
        }

        if (_board.Status != HackStatus.Active)
        {
            return;
        }

        for (var i = 0; i < _board.Slots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                Utility(i);
            }
        }
    }

    private void LateUpdate()
    {
        if (!_closing)
        {
            _uiInputState.Maintain();
        }
    }

    public void Close()
    {
        if (_closing)
        {
            return;
        }

        _closing = true;
        if (_audio)
        {
            _audio.Close();
        }

        Restore();
        if (Current == this)
        {
            Current = null;
        }

        Destroy(gameObject);
    }

    private void Restore()
    {
        if (!_captured)
        {
            return;
        }

        _captured = false;
        _inputState.Restore();
        _uiInputState.Restore();
    }

    private void OnDestroy()
    {
        Restore();
        if (Current == this)
        {
            Current = null;
        }

        if (_gaugeMaterials != null)
        {
            foreach (var material in _gaugeMaterials)
            {
                if (material)
                {
                    Destroy(material);
                }
            }
        }
    }
}

public class ElectronicsConsoleCommands
{
    [ConsoleCommand(
        "hacking",
        "",
        "PDA hacking practice: difficulty (1-3), Hacking level (0-51), seed"
    )]
    public static void Practice(
        [ConsoleArgument(2)] int difficulty,
        [ConsoleArgument(0)] int level,
        [ConsoleArgument(1)] int seed
    ) => HackingView.Practice(difficulty, level, seed);
}

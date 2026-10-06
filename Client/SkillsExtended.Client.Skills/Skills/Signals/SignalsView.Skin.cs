using System;
using System.Linq;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Hacking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.Signals;

public sealed partial class SignalsView
{
    private static readonly Color Ink = new(.82f, .88f, .86f);
    private static readonly Color Muted = new(.43f, .56f, .55f);
    private static readonly Color Accent = new(.36f, .88f, .74f);
    private static readonly Color Amber = new(1f, .64f, .25f);
    private TMP_Text _frequencyValue,
        _bearingValue,
        _phaseValue,
        _signalValue,
        _codeValue;
    private TMP_Text _recordLabel,
        _plotStatus,
        _mode,
        _spectrumTitle;
    private Button _recordButton;
    private RectTransform _bearingCard,
        _phaseCard;
    private Image _strengthFill;
    private readonly TMP_Text[] _stages = new TMP_Text[4];
    private readonly PdaSurface[] _stageFaces = new PdaSurface[4];
    private readonly TMP_Text[] _spectrumTicks = new TMP_Text[5];
    private Slider[] _controls;

    private PdaSurface Surface(
        Transform parent,
        string name,
        float x,
        float y,
        float w,
        float h,
        Color top,
        Color bottom,
        float corner = 8,
        float bevel = 1
    )
    {
        var g = Rect(name, parent, x, y, w, h).gameObject.AddComponent<PdaSurface>();
        g.Top = top;
        g.Bottom = bottom;
        g.Corner = corner;
        g.Bevel = bevel;
        g.Edge = new Color(.25f, .31f, .31f);
        g.raycastTarget = false;
        return g;
    }

    private Image Bar(
        Transform parent,
        string name,
        float x,
        float y,
        float w,
        float h,
        Color color
    )
    {
        var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text Label(
        Transform parent,
        string title,
        float x,
        float y,
        float w,
        float h,
        int size = 16,
        bool accent = false
    )
    {
        var label = Text(parent, title, x, y, w, h, size);
        label.color = accent ? Accent : Muted;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }

    private Button Control(
        Transform parent,
        string title,
        float x,
        float y,
        float w,
        float h,
        Action action,
        out TMP_Text label,
        bool primary = false
    )
    {
        var face = Surface(
            parent,
            title,
            x,
            y,
            w,
            h,
            primary ? new Color(.16f, .34f, .29f) : new Color(.16f, .2f, .21f),
            new Color(.045f, .07f, .075f),
            7,
            2
        );
        face.raycastTarget = true;
        var button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = button.colors;
        colors.normalColor = new Color(.82f, .88f, .85f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(.65f, .9f, .8f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.42f, .42f, .42f);
        colors.fadeDuration = .08f;
        button.colors = colors;
        button.onClick.AddListener(() => action());
        label = Label(face.transform, title, 8, 3, w - 16, h - 6, primary ? 18 : 15, primary);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private Slider ReceiverControl(
        Transform screen,
        string title,
        string keys,
        string left,
        string right,
        float y,
        float min,
        float max,
        float value,
        out TMP_Text readout,
        out RectTransform card
    )
    {
        card = (RectTransform)
            Surface(
                screen,
                title,
                24,
                y,
                374,
                116,
                new Color(.055f, .09f, .1f),
                new Color(.02f, .038f, .042f)
            ).transform;
        Label(card, title, 16, 8, 210, 22, 15);
        readout = Label(card, "", 160, 4, 196, 32, 24, true);
        readout.alignment = TextAlignmentOptions.MidlineRight;
        Label(card, left, 16, 85, 100, 20, 12);
        var end = Label(card, right, 260, 85, 96, 20, 12);
        end.alignment = TextAlignmentOptions.MidlineRight;
        var hint = Label(card, keys, 105, 85, 156, 20, 12);
        hint.alignment = TextAlignmentOptions.Center;
        var hit = Rect("Slider hit area", card, 16, 38, 340, 44);
        var background = hit.gameObject.AddComponent<Image>();
        background.color = Color.clear;
        var slider = hit.gameObject.AddComponent<Slider>();
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.wholeNumbers = false;
        Surface(hit, "Recessed track", 0, 17, 340, 10, Color.black, new Color(.12f, .17f, .17f), 3);
        for (var i = 0; i <= 20; i++)
            Bar(hit, "Dial tick", 8 + i * 16.2f, 3, 1, i % 5 == 0 ? 9 : 4, Muted * .65f);
        var fillArea = Rect("Fill area", hit, 8, 20, 324, 4);
        var fill = Bar(fillArea, "Tuned range", 0, 0, 324, 4, Accent * .65f);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        slider.fillRect = fill.rectTransform;
        // The handle moves inside its own inset track; its vertical anchors must not stretch.
        var handleArea = Rect("Handle travel", hit, 8, 22, 324, 0);
        var thumb = Surface(
            handleArea,
            "Knurled thumb",
            0,
            0,
            22,
            30,
            new Color(.52f, .61f, .58f),
            new Color(.19f, .27f, .25f),
            4,
            2
        );
        var thumbRect = (RectTransform)thumb.transform;
        thumbRect.pivot = new Vector2(.5f, .5f);
        thumb.raycastTarget = true;
        for (var i = 0; i < 3; i++)
            Bar(thumbRect, "Grip", 6 + i * 4, 8, 1, 14, new Color(.09f, .17f, .14f));
        slider.handleRect = thumbRect;
        slider.targetGraphic = thumb;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        return slider;
    }

    private void BuildScreen()
    {
        var template = HackingView.PdaAsset<GameObject>("ElectronicsUi");
        _font = template
            ? template.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font)?.font
            : null;
        if (!_font || !_font.atlasTexture)
            throw new InvalidOperationException("PDA font asset is unavailable.");
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();
        var shade = Bar(transform, "Backdrop", 0, 0, 0, 0, new Color(0, 0, 0, .66f));
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        shade.raycastTarget = true;
        var shell = Surface(
            transform,
            "PDA chassis",
            0,
            0,
            1420,
            920,
            new Color(.19f, .21f, .19f),
            new Color(.07f, .08f, .075f),
            42,
            7
        );
        var body = (RectTransform)shell.transform;
        body.anchorMin = body.anchorMax = body.pivot = new Vector2(.5f, .5f);
        body.anchoredPosition = Vector2.zero;
        Surface(
            body,
            "Inner housing",
            18,
            18,
            1384,
            884,
            new Color(.095f, .11f, .10f),
            new Color(.035f, .045f, .042f),
            30,
            3
        );
        foreach (var x in new[] { 28f, 1330f })
        {
            Surface(
                body,
                "Rubber grip",
                x,
                108,
                62,
                670,
                new Color(.036f, .042f, .038f),
                new Color(.016f, .02f, .018f),
                18,
                3
            );
            for (var i = 0; i < 21; i++)
                Surface(
                    body,
                    "Grip rib",
                    x + 9,
                    143 + i * 28,
                    44,
                    9,
                    new Color(.11f, .13f, .12f),
                    new Color(.025f, .03f, .026f),
                    3
                );
        }
        foreach (var x in new[] { 56f, 1344f })
        foreach (var y in new[] { 43f, 847f })
        {
            Surface(
                body,
                "Steel fastener",
                x,
                y,
                20,
                20,
                new Color(.39f, .42f, .38f),
                new Color(.09f, .11f, .10f),
                8,
                2
            );
            Bar(body, "Screw slot", x + 5, y + 9, 10, 2, Color.black);
        }
        Label(body, LocalizedText.Get("SkillsExtended.SignalsView.Skin.TerragroupFieldSystems"), 128, 32, 640, 28, 17);
        Label(body, LocalizedText.Get("SkillsExtended.SignalsView.Skin.ModifiedPdaRx201"), 884, 32, 416, 28, 15);
        Bar(body, "Power indicator", 1270, 43, 12, 5, Accent);
        Surface(
            body,
            "Screen gasket",
            108,
            74,
            1204,
            764,
            Color.black,
            new Color(.24f, .27f, .24f),
            13,
            5
        );
        var screen = (RectTransform)
            Surface(
                body,
                "Receiver screen",
                122,
                88,
                1176,
                736,
                new Color(.021f, .039f, .043f),
                new Color(.012f, .024f, .027f),
                4
            ).transform;
        screen.gameObject.AddComponent<RectMask2D>();
        Label(body, LocalizedText.Get("SkillsExtended.SignalsView.Skin.SignalsIntelligence"), 130, 851, 500, 28, 19);
        Label(body, LocalizedText.Get("SkillsExtended.SignalsView.Skin.ReusableFieldReceiver"), 930, 851, 364, 28, 13);
        for (var i = 0; i < 7; i++)
            Bar(body, "Speaker grille", 644 + i * 13, 857, 5, 23, Color.black);
        Control(body, LocalizedText.Get("SkillsExtended.SignalsView.Skin.ExitEsc"), 1330, 404, 62, 82, Close, out _);

        _heading = Label(screen, LocalizedText.Get("SkillsExtended.SignalsView.Skin.SignalsReceiver"), 24, 12, 770, 32, 25, true);
        _mode = Label(screen, "", 810, 14, 342, 28, 13);
        _mode.alignment = TextAlignmentOptions.MidlineRight;
        Bar(screen, "Header rule", 24, 58, 1128, 1, Muted * .45f);
        var stages = new[] { LocalizedText.Get("SkillsExtended.SignalsView.Skin.01Tune"), LocalizedText.Get("SkillsExtended.SignalsView.Skin.02Bearing"), LocalizedText.Get("SkillsExtended.SignalsView.Skin.03Fix"), LocalizedText.Get("SkillsExtended.SignalsView.Skin.04Pair") };
        for (var i = 0; i < stages.Length; i++)
        {
            _stageFaces[i] = Surface(
                screen,
                stages[i],
                24 + i * 282,
                72,
                270,
                30,
                new Color(.055f, .10f, .10f),
                new Color(.025f, .045f, .047f),
                4
            );
            _stages[i] = Label(_stageFaces[i].transform, stages[i], 12, 0, 246, 30, 13);
        }
        var spectrumCard = Surface(
            screen,
            "Receiver scope",
            24,
            116,
            374,
            170,
            new Color(.05f, .085f, .09f),
            new Color(.02f, .04f, .042f)
        );
        _spectrumTitle = Label(spectrumCard.transform, LocalizedText.Get("SkillsExtended.SignalsView.Skin.SpectrumMhz"), 16, 7, 342, 24, 13);
        _spectrum = Rect("Live spectrum", spectrumCard.transform, 16, 40, 342, 96)
            .gameObject.AddComponent<SignalGraphic>();
        _spectrum.View = this;
        _spectrum.Spectrum = true;
        _spectrum.raycastTarget = false;
        for (var i = 0; i < _spectrumTicks.Length; i++)
        {
            var tick = Label(
                spectrumCard.transform,
                (88 + i * 5).ToString(),
                16
                    + i * 85.5f
                    - (
                        i == 0 ? 0
                        : i == 4 ? 34
                        : 17
                    ),
                140,
                34,
                22,
                11
            );
            tick.alignment =
                i == 0 ? TextAlignmentOptions.MidlineLeft
                : i == 4 ? TextAlignmentOptions.MidlineRight
                : TextAlignmentOptions.Center;
            _spectrumTicks[i] = tick;
        }
        _frequency = ReceiverControl(
            screen,
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.Frequency"),
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.LeftRight"),
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.88Mhz"),
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.108Mhz"),
            298,
            88,
            108,
            94,
            out _frequencyValue,
            out _
        );
        _bearing = ReceiverControl(
            screen,
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.Bearing"),
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.UpDown"),
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.N000"),
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.360N"),
            426,
            0,
            360,
            0,
            out _bearingValue,
            out _bearingCard
        );
        _phase = ReceiverControl(
            screen,
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.PhaseAlignment"),
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.QE"),
            "0",
            "360",
            426,
            0,
            360,
            0,
            out _phaseValue,
            out _phaseCard
        );
        _controls = new[] { _frequency, _bearing, _phase };
        var plotCard = Surface(
            screen,
            "Navigation display",
            414,
            116,
            738,
            426,
            new Color(.045f, .075f, .08f),
            new Color(.016f, .03f, .034f)
        );
        Label(plotCard.transform, LocalizedText.Get("SkillsExtended.SignalsView.Skin.BearingPlot"), 18, 8, 400, 25, 14);
        Label(plotCard.transform, LocalizedText.Get("SkillsExtended.SignalsView.Skin.NNorthUp"), 552, 8, 168, 25, 12, true);
        _plot = Rect("Bearing plot", plotCard.transform, 18, 44, 702, 326)
            .gameObject.AddComponent<SignalGraphic>();
        _plot.View = this;
        _plot.raycastTarget = false;
        _plotStatus = Label(plotCard.transform, "", 18, 380, 700, 28, 13);
        var metrics = Surface(
            screen,
            "Telemetry",
            24,
            554,
            1128,
            62,
            new Color(.042f, .07f, .071f),
            new Color(.025f, .044f, .044f)
        );
        Label(metrics.transform, LocalizedText.Get("SkillsExtended.SignalsView.Skin.Reception"), 16, 5, 130, 20, 11);
        _signalValue = Label(metrics.transform, "", 16, 26, 90, 28, 22, true);
        Bar(metrics.transform, "Signal track", 110, 37, 166, 6, new Color(.11f, .17f, .17f));
        _strengthFill = Bar(metrics.transform, "Signal strength", 110, 37, 0, 6, Accent);
        _values = Label(metrics.transform, "", 304, 8, 535, 46, 15);
        Label(metrics.transform, LocalizedText.Get("SkillsExtended.SignalsView.Skin.RecoveredAccessCode"), 867, 5, 244, 20, 11);
        _codeValue = Label(metrics.transform, "------", 867, 27, 244, 28, 23, true);
        _status = Label(screen, "", 24, 632, 840, 52, 17);
        _status.enableWordWrapping = true;
        _recordButton = Control(
            screen,
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.RecordEnter"),
            886,
            632,
            266,
            52,
            ToggleRecord,
            out _recordLabel,
            true
        );
        if (!InRaid)
        {
            Control(
                screen,
                LocalizedText.Get("SkillsExtended.SignalsView.Skin.West"),
                24,
                697,
                100,
                28,
                () => Move(-Manifest.Config.MinimumSeparation, 0),
                out _
            );
            Control(
                screen,
                LocalizedText.Get("SkillsExtended.SignalsView.Skin.East"),
                132,
                697,
                100,
                28,
                () => Move(Manifest.Config.MinimumSeparation, 0),
                out _
            );
            Control(
                screen,
                LocalizedText.Get("SkillsExtended.SignalsView.Skin.North"),
                240,
                697,
                100,
                28,
                () => Move(0, Manifest.Config.MinimumSeparation),
                out _
            );
            Control(
                screen,
                LocalizedText.Get("SkillsExtended.SignalsView.Skin.PairingPractice"),
                348,
                697,
                196,
                28,
                () =>
                {
                    _recording = false;
                    Command("cancel");
                    Pairing = true;
                    _position = new SignalPoint { X = 160, Z = 210 };
                },
                out _
            );
            Label(screen, LocalizedText.Get("SkillsExtended.SignalsView.Skin.SimulatedMovementNoXpOrLoot"), 580, 697, 572, 28, 12);
        }
        else
            Label(
                screen,
                LocalizedText.Get("SkillsExtended.SignalsView.Skin.HoldPositionToRecordMoveMForANew", Manifest.Config.MinimumSeparation),
                24,
                697,
                1128,
                26,
                12
            );
        RenderScreen(0);
    }

    private void RenderScreen(float strength)
    {
        var offline = State.Unlocked;
        _bearingCard.gameObject.SetActive(!Pairing);
        _phaseCard.gameObject.SetActive(Pairing);
        _heading.text = Pairing ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.SignalsCachePairing") : LocalizedText.Get("SkillsExtended.SignalsView.Skin.SignalsReceiver");
        _mode.text = InRaid
            ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.LiveRaid", (
                    offline ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.BeaconOffline")
                    : SignalsModel.ProximityInterval(Manifest, State, Position) > 0
                        ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.ProximityBeacon")
                    : LocalizedText.Get("SkillsExtended.SignalsView.Skin.ReceiverOnline")
                ))
            : LocalizedText.Get("SkillsExtended.SignalsView.Skin.PracticeSimulatedSignal");
        _frequencyValue.text = LocalizedText.Get("SkillsExtended.SignalsView.Skin.Mhz", Frequency);
        _bearingValue.text = LocalizedText.Get("SkillsExtended.SignalsView.Skin.Deg", Bearing);
        _phaseValue.text = LocalizedText.Get("SkillsExtended.SignalsView.Skin.Deg", Phase);
        _spectrumTitle.text = Pairing ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.PairingMatchTheTwoWaveforms") : LocalizedText.Get("SkillsExtended.SignalsView.Skin.SpectrumMhz");
        foreach (var tick in _spectrumTicks)
            tick.gameObject.SetActive(!Pairing);
        _signalValue.text = offline ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.Off") : LocalizedText.Get("SkillsExtended.SignalsView.Skin.SignalStrength", strength * 100);
        _strengthFill.rectTransform.sizeDelta = new Vector2(
            offline ? 0 : 166 * Mathf.Clamp01(strength),
            6
        );
        _values.text =
            LocalizedText.Get("SkillsExtended.SignalsView.Skin.SkillPrecisionDegBearingsStoredMemorySlots", Level, SignalsModel.Uncertainty(Manifest.Config, Level), SignalsModel.PlottedReadings(State, Level).Count(), (Level >= 51 ? 6 : 4));
        _values.enableWordWrapping = true;
        _codeValue.text = State.HasFix ? State.AccessCode : "------";
        _plotStatus.text = State.HasFix
            ? Pairing || offline
                ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.SearchMWhiteYouCentered", State.Radius)
                : LocalizedText.Get("SkillsExtended.SignalsView.Skin.SearchMWhiteYouAmberArrowAntenna", State.Radius)
            : LocalizedText.Get("SkillsExtended.SignalsView.Skin.WhiteYouCenteredAmberAntennaGreenStored");
        var alignmentHint = Pairing
            ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.MatchTheWaveformsWithPhaseQEThenHold")
            : SignalsModel.ScanAlignmentHint(Manifest, Position, Level, Frequency, Bearing);
        _status.text =
            offline ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.CacheUnlockedBeaconOfflineLootTheCaseNormally")
            : _recording && !Pairing && alignmentHint != null ? alignmentHint
            : State.Message == "Align the signal and hold steady."
                ? alignmentHint ?? LocalizedText.Get("SkillsExtended.SignalsView.Skin.BearingAlignedHoldPositionToRecord")
            : State.Message
                ?? (
                    Pairing ? alignmentHint
                    : State.HasFix
                        ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.AccessCodeRecoveredSearchThePlottedAreaAndPair")
                    : State.Readings.Count >= 2
                        ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.NoCrossingFixYetMoveSidewaysRetuneTheBearing")
                    : LocalizedText.Get("SkillsExtended.SignalsView.Skin.TuneThePeakSweepForTheStrongestBearingThen")
                );
        _status.text = LocalizedText.Resolve(_status.text);
        _status.color =
            offline ? Accent
            : _recording ? Amber
            : Ink;
        _recordButton.interactable = !offline;
        _recordLabel.color = offline ? Muted : Accent;
        _recordLabel.text =
            offline ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.CacheUnlocked")
            : _recording ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.StopEnter")
            : Pairing ? LocalizedText.Get("SkillsExtended.SignalsView.Skin.PairEnter")
            : LocalizedText.Get("SkillsExtended.SignalsView.Skin.RecordEnter");
        foreach (var control in _controls)
            control.interactable = !offline;
        var stage =
            offline || Pairing ? 3
            : State.HasFix ? 2
            : State.Readings.Count > 0 || strength > .15f ? 1
            : 0;
        for (var i = 0; i < _stages.Length; i++)
        {
            _stages[i].color =
                i == stage ? Accent
                : i < stage ? Ink
                : Muted;
            var tint = i == stage ? new Color(.13f, .29f, .25f) : new Color(.055f, .10f, .10f);
            if (_stageFaces[i].Top != tint)
            {
                _stageFaces[i].Top = tint;
                _stageFaces[i].SetVerticesDirty();
            }
        }
    }
}

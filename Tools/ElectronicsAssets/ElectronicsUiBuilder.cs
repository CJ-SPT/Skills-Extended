// Authoring only. Copy this file into CJ-SDK/Assets/Mods/SkillsExtended.Assets/Editor.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkillsExtended.Electronics.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ElectronicsUiBuilder
{
    private const string Root = "Assets/Mods/SkillsExtended.Assets";
    private const string Generated = Root + "/Generated";
    private static TMP_FontAsset _font;
    private static Sprite _panel;
    private static Sprite _node;

    [MenuItem("SDK/Skills Extended/Build Electronics UI")]
    public static void Build()
    {
        if (Application.unityVersion != "2022.3.43f1")
        {
            throw new Exception("Use Unity 2022.3.43f1 for EFT bundles.");
        }

        Directory.CreateDirectory(Generated);
        AssetDatabase.Refresh();
        ElectronicsHudArtwork.Build(Generated);
        foreach (
            var spec in new[]
            {
                ("node-ring", false, false),
                ("defense-ring", true, false),
                ("hidden-node", false, true),
            }
        )
        {
            var path = Root + "/Inputs/" + spec.Item1 + ".png";
            if (!File.Exists(path))
            {
                var texture = ElectronicsUiVisuals.RingTexture(spec.Item2, spec.Item3);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        AssetDatabase.Refresh();
        foreach (var path in Directory.GetFiles(Root + "/Inputs", "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        foreach (var path in Directory.GetFiles(Root + "/Inputs", "*.wav"))
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }

        // Own a static ASCII atlas so no dynamic font dependency leaks from other SDK projects.
        var source = AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/TextMesh Pro/Fonts/LiberationSans.ttf"
        );
        if (!source)
        {
            throw new Exception("CJ-SDK TMP LiberationSans source font is missing.");
        }

        var fontPath = Generated + "/ElectronicsFontSharp.asset";
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (!_font)
        {
            _font = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                2048,
                2048
            );
            _font.name = "ElectronicsFont";
            if (
                !_font.TryAddCharacters(
                    string.Concat(Enumerable.Range(32, 95).Select(i => (char)i)) + "·"
                )
            )
            {
                throw new Exception("Font glyph generation failed.");
            }

            _font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(_font, fontPath);
            foreach (var atlas in _font.atlasTextures)
            {
                atlas.name = "ElectronicsFontAtlas";
                AssetDatabase.AddObjectToAsset(atlas, _font);
            }

            _font.material.name = "ElectronicsFontMaterial";
            AssetDatabase.AddObjectToAsset(_font.material, _font);
        }

        var lineShader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/ElectronicsLine.shader");
        if (!lineShader || ShaderUtil.ShaderHasError(lineShader))
        {
            throw new Exception("Electronics line shader failed compilation.");
        }

        var lineMaterialPath = Generated + "/ElectronicsLine.mat";
        var lineMaterial = AssetDatabase.LoadAssetAtPath<Material>(lineMaterialPath);
        if (!lineMaterial)
        {
            lineMaterial = new Material(lineShader) { name = "ElectronicsLine" };
            AssetDatabase.CreateAsset(lineMaterial, lineMaterialPath);
        }

        var go = new GameObject(
            "ElectronicsUi",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1;
        Image(
            "Backdrop",
            go.transform,
            new Vector2(10000, 10000),
            Vector2.zero,
            new Color(0, 0, 0, .35f)
        );
        var panel = Image(
            "Panel",
            go.transform,
            new Vector2(1440, 940),
            Vector2.zero,
            new Color(.008f, .011f, .012f, .98f)
        );
        var frame = panel.gameObject.AddComponent<Outline>();
        frame.effectDistance = new Vector2(1, -1);
        frame.effectColor = new Color(.2f, .26f, .27f);
        var viewport = Rect(
            "NetworkBackgroundViewport",
            panel.transform,
            new Vector2(1200, 640),
            new Vector2(0, 75)
        );
        viewport.gameObject.AddComponent<RectMask2D>();
        var background = Image(
            "NetworkBackground",
            viewport,
            new Vector2(896, 629),
            Vector2.zero,
            new Color(1, 1, 1, .15f)
        );
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            Root + "/Inputs/board-background.png"
        );
        background.raycastTarget = false;
        Image(
            "HeaderLine",
            panel.transform,
            new Vector2(1400, 1),
            new Vector2(0, 422),
            new Color(.17f, .22f, .23f)
        );
        var heading = Text(
            "Header",
            panel.transform,
            "TerraGroup Security System",
            13,
            new Vector2(1360, 28),
            new Vector2(0, 445)
        );
        heading.alignment = TextAlignmentOptions.Left;
        heading.color = new Color(.62f, .69f, .7f);
        Text(
            "Message",
            panel.transform,
            "Explore the network. Locate and defeat the system core.",
            14,
            new Vector2(1280, 30),
            new Vector2(0, 389)
        ).color = new Color(.62f, .72f, .72f);
        Rect("Board", panel.transform, new Vector2(1200, 580), new Vector2(0, 75));
        Text(
            "Tooltip",
            panel.transform,
            "Hover over a node for details.",
            13,
            new Vector2(1100, 38),
            new Vector2(0, -252)
        ).color = new Color(.58f, .72f, .72f);
        Text(
            "UtilityHeading",
            panel.transform,
            "UTILITY SUBSYSTEMS",
            17,
            new Vector2(540, 34),
            new Vector2(0, -303)
        ).color = ElectronicsUiVisuals.Amber;
        Image(
            "IntegrityBackground",
            panel.transform,
            new Vector2(184, 142),
            new Vector2(-570, -341),
            Color.white
        ).sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Generated + "/sharp-hud-background.png");
        Image(
            "IntegrityIcon",
            panel.transform,
            new Vector2(88, 66),
            new Vector2(-570, -341),
            Color.white
        ).sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Generated + "/sharp-hud-icon.png");
        foreach (var side in new[] { false, true })
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/ElectronicsGauge.shader");
            if (!shader || ShaderUtil.ShaderHasError(shader))
            {
                throw new Exception("Gauge shader failed compilation.");
            }

            var materialPath =
                Generated + (side ? "/ElectronicsGaugeRight.mat" : "/ElectronicsGaugeLeft.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.SetFloat("_Mirror", side ? 1 : 0);
            material.SetFloat("_Fill", 1);
            EditorUtility.SetDirty(material);
            var integrity = Image(
                side ? "IntegrityRight" : "IntegrityRing",
                panel.transform,
                new Vector2(40, 114),
                new Vector2(side ? -506 : -634, -341),
                ElectronicsUiVisuals.Amber
            );
            integrity.material = material;
            integrity.raycastTarget = false;
        }

        Text(
            "Stats",
            panel.transform,
            "COHERENCE 60 / 60",
            14,
            new Vector2(220, 28),
            new Vector2(-570, -254)
        );
        Text(
            "Strength",
            panel.transform,
            "STRENGTH 20",
            14,
            new Vector2(220, 28),
            new Vector2(-570, -428)
        );
        for (var i = 0; i < 4; i++)
        {
            var utility = Button(
                "Utility" + i,
                panel.transform,
                "[" + (i + 1) + "]",
                new Vector2(88, 88),
                new Vector2(-156 + i * 104, -366)
            );
            utility.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                Root + "/Inputs/utility-slot.png"
            );
            utility.GetComponent<Image>().color = Color.white;
            var label = utility.transform.Find("Label").GetComponent<TMP_Text>();
            label.rectTransform.anchoredPosition = new Vector2(0, -46);
            label.fontSize = label.fontSizeMax = 13;
            label.fontSizeMin = 11;
            var utilityIcon = Image(
                "Icon",
                utility.transform,
                new Vector2(64, 64),
                Vector2.zero,
                Color.white
            );
            utilityIcon.raycastTarget = false;
            utilityIcon.enabled = false;
        }

        Text(
            "Rules",
            panel.transform,
            "Explore adjacent nodes. Defenses block neighboring routes. Attack before retaliation.\nDistance clues point to the core, utilities or caches. Select a utility with 1-4. Esc aborts.",
            12,
            new Vector2(1030, 48),
            new Vector2(0, -433)
        ).color = new Color(.5f, .56f, .56f);
        var abort = Button(
            "Abort",
            panel.transform,
            "ABORT [ESC]",
            new Vector2(175, 42),
            new Vector2(570, -373)
        );
        abort.GetComponent<Image>().color = new Color(.08f, .04f, .02f);
        abort.gameObject.AddComponent<Outline>().effectColor = ElectronicsUiVisuals.Amber;
        var retry = Button(
            "Retry",
            panel.transform,
            "NEXT PRACTICE BOARD",
            new Vector2(270, 42),
            new Vector2(0, -357)
        );
        retry.gameObject.SetActive(false);
        var node = Button("NodeTemplate", go.transform, "", new Vector2(76, 86), Vector2.zero);
        node.GetComponent<Image>().color = Color.clear;
        var colors = node.colors;
        colors.disabledColor = Color.white;
        node.colors = colors;
        Image(
            "Ring",
            node.transform,
            new Vector2(30, 30),
            Vector2.zero,
            Color.white
        ).raycastTarget = false;
        Image(
            "Icon",
            node.transform,
            new Vector2(64, 64),
            Vector2.zero,
            Color.white
        ).raycastTarget = false;
        var nodeLabel = node.transform.Find("Label").GetComponent<TMP_Text>();
        nodeLabel.rectTransform.sizeDelta = new Vector2(100, 26);
        nodeLabel.rectTransform.anchoredPosition = new Vector2(0, -43);
        nodeLabel.fontSize = nodeLabel.fontSizeMax = 11;
        nodeLabel.fontSizeMin = 10;
        Text("Health", node.transform, "", 11, new Vector2(66, 22), new Vector2(0, 34));
        Text("Power", node.transform, "", 10, new Vector2(60, 20), new Vector2(0, -23));
        Text("Center", node.transform, "", 11, new Vector2(38, 24), Vector2.zero);
        node.gameObject.SetActive(false);
        var prefab = Generated + "/ElectronicsUi.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefab);
        UnityEngine.Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        var audio = Directory
            .GetFiles(Root + "/Inputs", "*.wav")
            .Concat(
                Directory
                    .GetFiles(Root + "/Inputs", "*.ogg")
                    .Where(p => !File.Exists(Path.ChangeExtension(p, ".wav")))
            );
        var assets = new[] { prefab, lineMaterialPath }
            .Concat(Directory.GetFiles(Root + "/Inputs", "*.png"))
            .Concat(audio)
            .Select(p => p.Replace('\\', '/'))
            .ToArray();
        var forbidden = AssetDatabase
            .GetDependencies(assets, true)
            .Where(p =>
                !p.StartsWith(Root + "/")
                && !p.StartsWith("Packages/")
                && !p.StartsWith("Assets/TextMesh Pro/")
                && !p.StartsWith("Resources/")
            )
            .ToArray();
        if (forbidden.Length != 0)
        {
            throw new Exception("Unrelated SDK dependencies: " + string.Join(", ", forbidden));
        }

        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "-electronicsOutput");
        var output =
            index >= 0
                ? args[index + 1]
                : Path.GetFullPath("../Skills-Extended/artifacts/electronics-ui");
        Directory.CreateDirectory(output);
        var manifest = BuildPipeline.BuildAssetBundles(
            output,
            new[]
            {
                new AssetBundleBuild
                {
                    assetBundleName = "electronics_ui.bundle",
                    assetNames = assets,
                },
            },
            BuildAssetBundleOptions.ChunkBasedCompression
                | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            BuildTarget.StandaloneWindows64
        );
        if (!manifest || manifest.GetAllDependencies("electronics_ui.bundle").Length != 0)
        {
            throw new Exception("Bundle build failed or external bundles required.");
        }

        var loaded = AssetBundle.LoadFromFile(Path.Combine(output, "electronics_ui.bundle"));
        if (!loaded)
        {
            throw new Exception("Could not reload bundle.");
        }

        var built = loaded.LoadAsset<GameObject>("ElectronicsUi");
        if (
            !built
            || built
                .GetComponentsInChildren<TMP_Text>(true)
                .Any(t => !t.font || !t.font.atlasTexture)
        )
        {
            throw new Exception("Missing font or prefab after bundle reload.");
        }

        foreach (
            var clip in new[]
            {
                "startup",
                "reveal",
                "attack",
                "cache",
                "utility",
                "shield-on",
                "shield-off",
                "success",
                "failure",
                "abort",
                "ambient",
                "low-coherence",
            }
        )
        {
            var sound = loaded.LoadAsset<AudioClip>(clip);
            if (!sound || sound.samples == 0 || sound.length <= 0)
            {
                throw new Exception("Missing/empty audio: " + clip);
            }
        }

        var builtLine = loaded.LoadAsset<Material>("ElectronicsLine");
        if (!builtLine || !builtLine.shader || !builtLine.shader.isSupported)
        {
            throw new Exception("Line material unavailable after reload.");
        }

        if (
            built.transform.Find("Panel/IntegrityIcon").GetComponent<Image>().sprite.texture.width
            < 352
        )
        {
            throw new Exception("Low resolution gauge icon.");
        }

        ValidateAudio(loaded, output);
        foreach (
            var sprite in new[]
            {
                "cpu",
                "shield",
                "bug",
                "wrench",
                "radio",
                "heart-pulse",
                "scissors",
                "shield-check",
                "zap",
                "database",
            }
        )
        {
            if (!loaded.LoadAsset<Sprite>(sprite))
            {
                throw new Exception("Missing sprite: " + sprite);
            }
        }

        if (args.Contains("-electronicsPreview"))
        {
            Preview(loaded, output);
        }

        ValidateSharpness(loaded, output);
        File.WriteAllText(
            Path.Combine(output, "validation.txt"),
            "Unity "
                + Application.unityVersion
                + "\nReload: passed\nFonts/sprites/audio: passed\nExternal bundle dependencies: none\n"
                + string.Join("\n", AssetDatabase.GetDependencies(assets, true))
        );
        loaded.Unload(true);
        Debug.Log("Electronics bundle validated: " + output);
    }

    private static void ValidateSharpness(AssetBundle bundle, string output)
    {
        var canvasGo = new GameObject(
            "LineCoverageValidation",
            typeof(RectTransform),
            typeof(Canvas)
        );
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(128, 128);
        var cameraGo = new GameObject("LineCoverageCamera");
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 64;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 30;
        canvas.worldCamera = camera;
        var line = Image(
            "Line",
            canvasGo.transform,
            new Vector2(90, 4.5f),
            Vector2.zero,
            Color.white
        );
        line.material = bundle.LoadAsset<Material>("ElectronicsLine");
        line.transform.localRotation = Quaternion.Euler(0, 0, 35);
        var target = new RenderTexture(128, 128, 24) { antiAliasing = 1 };
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(128, 128, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
        image.Apply();
        var pixels = image.GetPixels32();
        var fractional = pixels.Count(p => p.r > 10 && p.r < 245);
        if (
            fractional < 50
            || pixels.Count(p => p.r >= 245) < 20
            || pixels.Count(p => p.r == 0) < 15000
        )
        {
            throw new Exception(
                "Analytical line coverage failed (no multisampling): " + fractional
            );
        }

        File.WriteAllBytes(Path.Combine(output, "line-coverage.png"), image.EncodeToPNG());
        File.WriteAllText(
            Path.Combine(output, "sharpness-validation.txt"),
            $"Line shader reloaded and supported; {fractional} fractional edge pixels at 35 degrees without MSAA.\nGauge icon 352x264; procedural shader bars; background 736x568. Uncompressed, original NPOT dimensions preserved.\nFont atlas 2048x2048 at 90-point sampling.\n"
        );
        camera.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(canvasGo);
        UnityEngine.Object.DestroyImmediate(cameraGo);
    }

    private static void ValidateAudio(AssetBundle bundle, string output)
    {
        var report = new System.Text.StringBuilder("PCM source-to-bundle validation\n");
        foreach (var path in Directory.GetFiles(Root + "/Inputs", "*.wav"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var clip = bundle.LoadAsset<AudioClip>(name);
            if (!clip || !clip.LoadAudioData())
            {
                throw new Exception("Audio preload failed: " + name);
            }

            var samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0))
            {
                throw new Exception("Cannot read decoded audio: " + name);
            }

            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                if (new string(reader.ReadChars(4)) != "RIFF")
                {
                    throw new Exception("Expected PCM WAV: " + name);
                }

                reader.ReadInt32();
                reader.ReadChars(4);
                short format = 0,
                    channels = 0,
                    bits = 0;
                int rate = 0;
                byte[] data = null;
                while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
                {
                    var id = new string(reader.ReadChars(4));
                    var length = reader.ReadInt32();
                    var next = reader.BaseStream.Position + length + (length & 1);
                    if (id == "fmt ")
                    {
                        format = reader.ReadInt16();
                        channels = reader.ReadInt16();
                        rate = reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt16();
                        bits = reader.ReadInt16();
                    }

                    if (id == "data")
                    {
                        data = reader.ReadBytes(length);
                    }

                    reader.BaseStream.Position = next;
                }

                if (
                    format != 1
                    || bits != 16
                    || data == null
                    || data.Length / 2 != samples.Length
                    || channels != clip.channels
                    || rate != clip.frequency
                )
                {
                    throw new Exception("Audio format/sample count changed in bundle: " + name);
                }

                float error = 0,
                    peak = 0;
                for (var i = 0; i < samples.Length; i++)
                {
                    if (float.IsNaN(samples[i]) || float.IsInfinity(samples[i]))
                    {
                        throw new Exception("Invalid PCM sample: " + name);
                    }

                    error = Mathf.Max(
                        error,
                        Mathf.Abs(samples[i] - BitConverter.ToInt16(data, i * 2) / 32768f)
                    );
                    peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
                }

                if (error > .0001f)
                {
                    throw new Exception("PCM source/bundle mismatch: " + name + " / " + error);
                }

                report.AppendLine(
                    $"{name}: {rate} Hz, {channels} channels, {clip.samples} frames, peak {peak:F6}, maximum sample error {error:F8}"
                );
            }
        }

        File.WriteAllText(Path.Combine(output, "audio-validation.txt"), report.ToString());
    }

    [Serializable]
    private class PreviewBoard
    {
        public PreviewNode[] Nodes;
        public int Slots;
        public int Coherence;
        public int MaximumCoherence;
        public int Strength;
        public int Difficulty;
        public int[] Utilities;
    }

    [Serializable]
    private class PreviewNode
    {
        public int Id;
        public int Q;
        public int R;
        public int Kind;
        public int Coherence;
        public int Strength;
        public int Clue;
        public bool Revealed;
        public bool Cleared;
        public int[] Neighbors;
    }

    [Serializable]
    private class MotionPreview
    {
        public PreviewBoard Before;
        public PreviewBoard After;
        public int Clicked;
        public int[] BeforeAvailable;
        public int[] AfterAvailable;
        public MotionNode[] Nodes;
        public MotionPath[] Paths;
    }

    [Serializable]
    private class MotionNode
    {
        public int Node;
        public int Motion;
    }

    [Serializable]
    private class MotionPath
    {
        public int From;
        public int To;
        public bool Claimed;
    }

    private static void Preview(AssetBundle bundle, string output)
    {
        var board = JsonUtility.FromJson<PreviewBoard>(
            File.ReadAllText(Path.Combine(output, "preview-board.json"))
        );
        foreach (
            var resolution in new[]
            {
                new Vector2Int(1920, 1080),
                new Vector2Int(2560, 1440),
                new Vector2Int(3440, 1440),
            }
        )
        {
            var go = UnityEngine.Object.Instantiate(bundle.LoadAsset<GameObject>("ElectronicsUi"));
            var gaugeMaterials = ElectronicsUiVisuals.CreateGaugeMaterials(
                go.transform.Find("Panel")
            );
            ElectronicsUiVisuals.SetCoherence(
                gaugeMaterials,
                (float)board.Coherence / board.MaximumCoherence
            );
            var cameraGo = new GameObject("ElectronicsPreviewCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .04f, .045f);
            camera.orthographic = true;
            camera.orthographicSize = 540;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 30;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.enabled = false;
            ((RectTransform)go.transform).sizeDelta = new Vector2(
                resolution.x * 1080f / resolution.y,
                1080
            );
            canvas.worldCamera = camera;
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.transform.Find("Panel/Header").GetComponent<TMP_Text>().text =
                "TerraGroup Security System";
            go.transform.Find("Panel/Stats").GetComponent<TMP_Text>().text =
                $"COHERENCE {board.Coherence} / {board.MaximumCoherence}";
            go.transform.Find("Panel/Strength").GetComponent<TMP_Text>().text =
                "STRENGTH " + board.Strength;
            for (var i = 0; i < 4; i++)
            {
                var utility = go.transform.Find("Panel/Utility" + i);
                utility.gameObject.SetActive(i < board.Slots);
                ((RectTransform)utility).anchoredPosition = new Vector2(
                    (i - (board.Slots - 1) * .5f) * 104,
                    -366
                );
                var icon = utility.Find("Icon").GetComponent<Image>();
                icon.enabled = i < board.Utilities.Length;
                if (icon.enabled)
                {
                    icon.sprite = bundle.LoadAsset<Sprite>(
                        ElectronicsUiVisuals.Icons[board.Utilities[i]]
                    );
                }
            }

            var boardRoot = go.transform.Find("Panel/Board");
            var template = go.transform.Find("NodeTemplate").gameObject;
            var xs = board.Nodes.Select(n => n.Q + n.R * .5f).ToArray();
            var ys = board.Nodes.Select(n => n.R * ElectronicsUiVisuals.RowSpacing).ToArray();
            var scale = Math.Min(
                130,
                Math.Min(
                    1080 / Math.Max(1, xs.Max() - xs.Min()),
                    520 / Math.Max(1, ys.Max() - ys.Min())
                )
            );
            Vector2 Pos(PreviewNode n) =>
                new Vector2(
                    (n.Q + n.R * .5f - (xs.Max() + xs.Min()) / 2) * scale,
                    -(n.R * ElectronicsUiVisuals.RowSpacing - (ys.Max() + ys.Min()) / 2) * scale
                );
            ElectronicsUiVisuals.AlignBackground(
                (RectTransform)
                    go.transform.Find("Panel/NetworkBackgroundViewport/NetworkBackground"),
                scale,
                (xs.Max() + xs.Min()) / 2,
                (ys.Max() + ys.Min()) / 2,
                (int)Math.Round(board.Nodes.Average(n => n.Q)),
                (int)Math.Round(board.Nodes.Average(n => n.R))
            );
            var renderedNodes = new GameObject[board.Nodes.Length];
            var connections = new List<ElectronicsBoardFx.Connection>();
            foreach (var n in board.Nodes)
            {
                foreach (var adjacent in n.Neighbors.Where(i => i > n.Id))
                {
                    var a = Pos(n);
                    var b = Pos(board.Nodes[adjacent]);
                    var edge = Image(
                        "Edge",
                        boardRoot,
                        new Vector2(Math.Max(1, Vector2.Distance(a, b) - 42), 1.5f),
                        (a + b) / 2,
                        new Color(.65f, .37f, .15f)
                    );
                    ElectronicsUiVisuals.SmoothConnection(
                        edge,
                        bundle.LoadAsset<Material>("ElectronicsLine")
                    );
                    edge.transform.localRotation = Quaternion.Euler(
                        0,
                        0,
                        Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg
                    );
                    connections.Add(new ElectronicsBoardFx.Connection(n.Id, adjacent, edge));
                }
            }

            foreach (var n in board.Nodes)
            {
                var node = UnityEngine.Object.Instantiate(template, boardRoot);
                node.SetActive(true);
                ((RectTransform)node.transform).anchoredPosition = Pos(n);
                renderedNodes[n.Id] = node;
                ElectronicsUiVisuals.DrawNode(
                    node,
                    n.Kind,
                    n.Revealed,
                    n.Cleared,
                    !n.Cleared,
                    n.Coherence,
                    n.Strength,
                    n.Clue,
                    name =>
                        bundle.LoadAsset<Sprite>(
                            name == "cpu"
                                ? board.Difficulty == 1
                                    ? "core-standard"
                                    : board.Difficulty == 2
                                        ? "core-secure"
                                        : "core-hardened"
                                : name
                        )
                );
            }

            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(resolution.x, resolution.y, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
            image.Apply();
            File.WriteAllBytes(
                Path.Combine(output, $"layout-{resolution.x}x{resolution.y}.png"),
                image.EncodeToPNG()
            );
            if (resolution.x == 1920)
            {
                PreviewMotion(
                    bundle,
                    output,
                    (RectTransform)boardRoot,
                    renderedNodes,
                    connections.ToArray(),
                    camera,
                    image
                );
            }

            PreviewGauge(go, gaugeMaterials, camera, image, output);
            foreach (var material in gaugeMaterials)
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(cameraGo);
        }
    }

    private static void PreviewGauge(
        GameObject go,
        Material[] materials,
        Camera camera,
        Texture2D image,
        string output
    )
    {
        foreach (var material in materials)
        {
            if (!material.shader.isSupported || !material.HasProperty("_Fill"))
            {
                throw new Exception("Missing gauge shader after reload.");
            }
        }

        var scale = image.height / 1080f;
        var size = Mathf.RoundToInt(220 * scale);
        var x = Mathf.RoundToInt(image.width * .5f - 680 * scale);
        var y = Mathf.RoundToInt(image.height * .5f - 451 * scale);
        var previousCount = -1;
        foreach (var fill in new[] { 0f, .01f, .25f, .5f, .75f, 1f })
        {
            ElectronicsUiVisuals.SetCoherence(materials, fill);
            go.transform.Find("Panel/Stats").GetComponent<TMP_Text>().text =
                $"COHERENCE {Mathf.RoundToInt(fill * 100)} / 100";
            Canvas.ForceUpdateCanvases();
            camera.Render();
            image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
            image.Apply();
            var crop = new Texture2D(size, size, TextureFormat.RGB24, false);
            crop.SetPixels(image.GetPixels(x, y, size, size));
            crop.Apply();
            var pixels = crop.GetPixels32();
            var count = pixels.Count(p => p.r > 180 && p.g > 70 && p.b < 100);
            if (count < previousCount || fill == 0 && count != 0 || fill == 1 && count < 500)
            {
                throw new Exception("Gauge fill is not monotonic or empty/full bounds are wrong.");
            }

            previousCount = count;
            File.WriteAllBytes(
                Path.Combine(output, $"gauge-{image.width}x{image.height}-{fill * 100:000}.png"),
                crop.EncodeToPNG()
            );
            UnityEngine.Object.DestroyImmediate(crop);
        }

        File.AppendAllText(
            Path.Combine(output, "gauge-validation.txt"),
            $"{image.width}x{image.height}: 0, 1, 25, 50, 75, 100 percent rendered; monotonic fill; empty/full bounds passed.\n"
        );
    }

    private static void PreviewMotion(
        AssetBundle bundle,
        string output,
        RectTransform root,
        GameObject[] nodes,
        ElectronicsBoardFx.Connection[] connections,
        Camera camera,
        Texture2D image
    )
    {
        var fixture = JsonUtility.FromJson<MotionPreview>(
            File.ReadAllText(Path.Combine(output, "animation-preview.json"))
        );
        Sprite Sprite(string name) =>
            bundle.LoadAsset<Sprite>(name == "cpu" ? "core-hardened" : name);
        foreach (
            var name in new[]
            {
                "fx-ring",
                "fx-glow",
                "fx-infected",
                "fx-trace-1",
                "fx-trace-2",
                "fx-trace-3",
                "fx-trace-4",
            }
        )
        {
            if (!Sprite(name))
            {
                throw new Exception("Missing animation texture: " + name);
            }
        }

        var fx = root.gameObject.AddComponent<ElectronicsBoardFx>();
        fx.Initialize(root, nodes, connections, Sprite);
        fx.Sample(0);
        var positions = nodes.Select(n => ((RectTransform)n.transform).anchoredPosition).ToArray();
        void Draw(PreviewBoard board, int[] available)
        {
            root.parent.Find("Strength").GetComponent<TMP_Text>().text =
                "STRENGTH " + board.Strength;
            root.parent.Find("Stats").GetComponent<TMP_Text>().text =
                $"COHERENCE {board.Coherence} / {board.MaximumCoherence}";
            for (var slot = 0; slot < 4; slot++)
            {
                var utility = root.parent.Find("Utility" + slot);
                utility.gameObject.SetActive(slot < board.Slots);
                utility.Find("Icon").GetComponent<Image>().enabled = slot < board.Utilities.Length;
            }

            foreach (var n in board.Nodes)
            {
                ElectronicsUiVisuals.DrawNode(
                    nodes[n.Id],
                    n.Kind,
                    n.Revealed,
                    n.Cleared,
                    available.Contains(n.Id),
                    n.Coherence,
                    n.Strength,
                    n.Clue,
                    Sprite
                );
                fx.SetAvailable(n.Id, available.Contains(n.Id));
            }

            foreach (var c in connections)
            {
                var a = board.Nodes[c.A];
                var b = board.Nodes[c.B];
                fx.SetConnectionColor(
                    c.A,
                    c.B,
                    a.Cleared && b.Cleared ? ElectronicsUiVisuals.Amber
                        : a.Cleared && available.Contains(c.B)
                        || b.Cleared && available.Contains(c.A)
                            ? new Color(.2f, .42f, .4f)
                        : new Color(.12f, .15f, .15f)
                );
            }
        }

        Draw(fixture.Before, fixture.BeforeAvailable);
        var directory = Path.Combine(output, "motion");
        Directory.CreateDirectory(directory);
        var hashes = new HashSet<string>();
        for (var frame = 0; frame < 32; frame++)
        {
            fx.Sample(frame / 20f);
            if (frame == 3)
            {
                fx.Hover(fixture.Clicked, true);
                fx.Click(fixture.Clicked);
            }

            if (frame == 4)
            {
                Draw(fixture.After, fixture.AfterAvailable);
                foreach (var n in fixture.Nodes)
                {
                    switch (n.Motion)
                    {
                        case 0:
                            fx.Reveal(n.Node);
                            break;
                        case 1:
                            fx.DestroyNode(n.Node, null);
                            break;
                        case 2:
                            fx.Damage(n.Node);
                            break;
                        case 3:
                            fx.Repair(n.Node);
                            break;
                        case 4:
                            fx.Available(n.Node);
                            break;
                        case 5:
                            fx.Blocked(n.Node);
                            break;
                    }
                }

                foreach (var p in fixture.Paths)
                {
                    fx.OpenPath(p.From, p.To, p.Claimed);
                }
            }

            fx.Sample(frame / 20f);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
            image.Apply();
            var png = image.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(directory, $"frame-{frame:000}.png"), png);
            hashes.Add(
                Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.Create().ComputeHash(png)
                )
            );
        }

        fx.Sample(2);
        if (fx.ActiveEffectCount != 0 || hashes.Count < 20)
        {
            throw new Exception("Animation did not render or settle.");
        }

        // Exercise overlapping feedback, replacement of a running path, and pool saturation.
        var edge = connections[0];
        fx.OpenPath(edge.A, edge.B, true);
        fx.OpenPath(edge.B, edge.A, false);
        if (fx.ActiveEffectCount != 1)
        {
            throw new Exception("Rapid path replacement failed.");
        }

        for (var i = 0; i < 220; i++)
        {
            fx.Click(i % nodes.Length);
            fx.Damage(i % nodes.Length);
        }

        fx.Repair(0);
        fx.DestroyNode(0, Sprite("shield"));
        fx.Finish(true, 0);
        fx.Finish(false, 0);
        fx.SetConnectionColor(edge.A, edge.B, Color.red);
        fx.Sample(5);
        if (fx.ActiveEffectCount != 0 || edge.Image.color != Color.red)
        {
            throw new Exception("Rapid effects left stale state.");
        }

        for (var i = 0; i < nodes.Length; i++)
        {
            if (
                ((RectTransform)nodes[i].transform).anchoredPosition != positions[i]
                || nodes[i].transform.Find("Ring").localScale != Vector3.one
            )
            {
                throw new Exception("Animation moved a node off its grid or did not settle.");
            }
        }

        if (
            root.Find("Network effects")
                .GetComponentsInChildren<Image>(true)
                .Any(i => i.raycastTarget)
        )
        {
            throw new Exception("Effects intercept input.");
        }

        File.WriteAllText(
            Path.Combine(output, "motion-validation.txt"),
            $"Rendered 32 frames at 20 fps; {hashes.Count} distinct frames.\nGrid positions preserved; transforms settle; transient effects cleaned up; rapid effects bounded; current path color restored; effects do not intercept input.\n"
        );
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static Image Image(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        Color color
    )
    {
        var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text Text(
        string name,
        Transform parent,
        string value,
        int size,
        Vector2 dimensions,
        Vector2 position
    )
    {
        var text = Rect(name, parent, dimensions, position)
            .gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.text = value;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = size - 4;
        text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.9f, .94f, .93f);
        text.raycastTarget = false;
        return text;
    }

    private static Button Button(
        string name,
        Transform parent,
        string label,
        Vector2 size,
        Vector2 position
    )
    {
        var image = Image(name, parent, size, position, new Color(.15f, .22f, .25f));
        var b = image.gameObject.AddComponent<Button>();
        b.targetGraphic = image;
        Text("Label", image.transform, label, 14, size - new Vector2(6, 6), Vector2.zero);
        return b;
    }
}

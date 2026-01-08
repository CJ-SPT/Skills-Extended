using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.LockPicking.Alternative;

public class AlternativeLockpickUI : MonoBehaviour
{
    private static AlternativeLockpickUI _instance;
    
    private GameObject _ringRootGo;
    private CanvasGroup _group;
    private Canvas _canvas;
    private Image _fill;
    private TextMeshProUGUI _secondsText;
    private TextMeshProUGUI _labelText;
    
    private Coroutine _blinkRoutine;
    private Coroutine _resultRoutine;
    
    private const float RootSizePx = 64f;
    private const float RingThicknessPx = 4f;
    private const float FillInsetPx = 0f;
    
    private static readonly Color32 ColorProgress = new Color32(0xB5, 0xC1, 0xAD, 0xFF);
    private static readonly Color32 ColorText = new Color32(0xDA, 0xDA, 0xBC, 0xFF);
    private static readonly Color32 ColorSuccess = new Color32(0x9A, 0xD3, 0x23, 0xFF);
    private static readonly Color32 ColorFailure = new Color32(0xEC, 0x4D, 0x31, 0xFF);
    
    public static AlternativeLockpickUI EnsureInstance()
    {
        if (_instance != null)
        {
            return _instance;
        }

        var go = new GameObject("SkillsExtended.AltLockpickUi");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<AlternativeLockpickUI>();
        _instance.Build();
        _instance.Hide();
        return _instance;
    }

    private void Build()
    {
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 1f;
        
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5000;
        
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        gameObject.AddComponent<GraphicRaycaster>();
        
        const int spriteSize = 256;
        
        float thicknessInSpritePx = (RingThicknessPx / RootSizePx) * spriteSize;

        var ringSprite = MakeRingSprite(size: spriteSize, thickness: thicknessInSpritePx, feather: 2.0f);
        
        var root = new GameObject("Root", typeof(RectTransform));
        root.transform.SetParent(_canvas.transform, false);

        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.16f);
        rootRt.anchorMax = new Vector2(0.5f, 0.16f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(RootSizePx, RootSizePx + 26f);
        
        _ringRootGo = new GameObject("RingRoot", typeof(RectTransform));
        _ringRootGo.transform.SetParent(root.transform, false);

        var ringRt = _ringRootGo.GetComponent<RectTransform>();
        ringRt.anchorMin = new Vector2(0.5f, 1f);
        ringRt.anchorMax = new Vector2(0.5f, 1f);
        ringRt.pivot = new Vector2(0.5f, 1f);
        ringRt.anchoredPosition = Vector2.zero;
        ringRt.sizeDelta = new Vector2(RootSizePx, RootSizePx);
        
        var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(_ringRootGo.transform, false);

        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var bgImg = bgGo.GetComponent<Image>();
        bgImg.sprite = ringSprite;
        bgImg.type = Image.Type.Simple;
        bgImg.raycastTarget = false;
        bgImg.color = new Color(1f, 1f, 1f, 0.15f);
        
        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(_ringRootGo.transform, false);

        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(FillInsetPx, FillInsetPx);
        fillRt.offsetMax = new Vector2(-FillInsetPx, -FillInsetPx);

        _fill = fillGo.GetComponent<Image>();
        _fill.sprite = ringSprite;
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Radial360;
        _fill.fillOrigin = (int)Image.Origin360.Top;
        _fill.fillClockwise = true;
        _fill.fillAmount = 0f;
        _fill.raycastTarget = false;
        _fill.color = ColorProgress;

        var font = TryGetDefaultTmpFont();
        
        var secondsGo = new GameObject("Seconds", typeof(RectTransform), typeof(TextMeshProUGUI));
        secondsGo.transform.SetParent(_ringRootGo.transform, false);

        var secondsRt = secondsGo.GetComponent<RectTransform>();
        secondsRt.anchorMin = new Vector2(0.5f, 0.5f);
        secondsRt.anchorMax = new Vector2(0.5f, 0.5f);
        secondsRt.pivot = new Vector2(0.5f, 0.5f);
        secondsRt.sizeDelta = new Vector2(RootSizePx, RootSizePx);
        secondsRt.anchoredPosition = Vector2.zero;

        _secondsText = secondsGo.GetComponent<TextMeshProUGUI>();
        _secondsText.font = font;
        _secondsText.fontSize = 16;
        _secondsText.alignment = TextAlignmentOptions.Center;
        _secondsText.color = ColorText;
        _secondsText.raycastTarget = false;
        _secondsText.enableWordWrapping = false;
        _secondsText.text = "";
        
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(root.transform, false);

        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0f);
        labelRt.anchorMax = new Vector2(0.5f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, -4f);
        labelRt.sizeDelta = new Vector2(220f, 22f);

        _labelText = labelGo.GetComponent<TextMeshProUGUI>();
        _labelText.font = font;
        _labelText.fontSize = 16;
        _labelText.alignment = TextAlignmentOptions.Center;
        _labelText.color = new Color(218f/255f, 218f/255f, 188f/255f, 1f);
        _labelText.raycastTarget = false;
        _labelText.enableWordWrapping = false;
        _labelText.text = "";
    }

    public void Show(float initialSeconds)
    {
        if (_ringRootGo != null)
        {
            _ringRootGo.SetActive(true);
        }

        if (_secondsText != null)
        {
            _secondsText.enabled = true;
        }

        if (_canvas == null)
        {
            return;
        }

        _fill.fillAmount = 0f;
        
        SetSeconds(initialSeconds);
        
        _canvas.enabled = true;

        if (_resultRoutine != null)
        {
            StopCoroutine(_resultRoutine); 
            _resultRoutine = null;
        }
        
        _group.alpha = 1f;

        _labelText.text = "LOCKPICKING";
        _labelText.color = ColorText;
        
        StartBlink();
    }

    public void SetProgress(float p)
    {
        if (_fill != null)
        {
            _fill.fillAmount = Mathf.Clamp01(p);
        }
    }

    public void SetSeconds(float seconds)
    {
        if (_secondsText == null)
        {
            return;
        }

        if (seconds < 0f) seconds = 0f;
        
        _secondsText.text = $"{seconds:0.0}s";
    }

    public void Hide()
    {
        StopBlink();
        
        if (_resultRoutine != null)
        {
            StopCoroutine(_resultRoutine); 
            _resultRoutine = null;
        }

        if (_group != null)
        {
            _group.alpha = 1f;
        }
        
        _canvas.enabled = false;
    }

    private void StartBlink()
    {
        StopBlink();
        _blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlink()
    {
        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }

        if (_labelText != null)
        {
            var c = _labelText.color;
            c.a = 0.75f;
            _labelText.color = c;
        }
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.unscaledTime * 2.0f, 1f);
            float a = Mathf.Lerp(0.25f, 0.85f, t);

            if (_labelText != null)
            {
                var c = _labelText.color;
                c.a = a;
                _labelText.color = c;
            }

            yield return null;
        }
    }
    
    private static Sprite MakeRingSprite(int size, float thickness, float feather)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;

        float outerR = Mathf.Min(cx, cy) - 1f;
        float innerR = Mathf.Max(outerR - thickness, 0f);

        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            float dy = y - cy;
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                float aOuter = SmoothStep(outerR + feather, outerR - feather, dist);
                float aInner = SmoothStep(innerR - feather, innerR + feather, dist);
                float a = Mathf.Clamp01(aOuter * aInner);

                byte ab = (byte)Mathf.RoundToInt(255f * a);
                pixels[y * size + x] = new Color32(255, 255, 255, ab);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Mathf.InverseLerp(edge0, edge1, x);
        return Mathf.Clamp01(t);
    }
    
    public void ShowResult(bool success)
    {
        StopBlink();

        if (_resultRoutine != null)
        {
            StopCoroutine(_resultRoutine);
            _resultRoutine = null;
        }

        if (_ringRootGo != null)
        {
            _ringRootGo.SetActive(false);
        }
        
        if (_secondsText != null)
        {
            _secondsText.text = "";
            _secondsText.enabled = false;
        }

        if (_labelText != null)
        {
            _labelText.text = success ? "SUCCESS" : "FAILURE";
            _labelText.color = success ? ColorSuccess : ColorFailure;

            var c = _labelText.color;
            c.a = 1f;
            _labelText.color = c;
        }

        _resultRoutine = StartCoroutine(ResultRoutine());
    }

    private IEnumerator ResultRoutine()
    {
        float hold = 1.0f;
        while (hold > 0f)
        {
            hold -= Time.unscaledDeltaTime;
            yield return null;
        }
        
        float fade = 0.45f;
        float t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / fade));
            if (_group != null) _group.alpha = a;
            yield return null;
        }

        Hide();
    }
    
    private static TMP_FontAsset TryGetDefaultTmpFont()
    {
        try
        {
            return TMP_Settings.defaultFontAsset;
        }
        catch
        {
            return null;
        }
    }
}
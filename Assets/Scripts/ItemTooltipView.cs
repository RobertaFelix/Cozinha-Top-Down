using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemTooltipView : MonoBehaviour
{
    private static ItemTooltipView instance;
    public static bool IsDragging { get; private set; }
    private ItemTooltipTarget owner;
    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform panel;
    private Image icon;
    private TextMeshProUGUI title;
    private TextMeshProUGUI body;
    private Vector2 pointerPosition;
    private float showAt;
    private string previousTitle;
    private string previousBody;
    private Sprite previousIcon;
    private float previousWidth;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; IsDragging = false; }

    public static void Show(ItemTooltipTarget target, Vector2 position)
    {
        if (IsDragging || target == null) return;
        if (instance == null)
        {
            var root = new GameObject("ItemTooltipCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            instance = root.AddComponent<ItemTooltipView>();
        }
        instance.owner = target;
        instance.pointerPosition = position;
        instance.showAt = Time.unscaledTime + .2f;
        instance.previousWidth = -1;
        instance.panel.gameObject.SetActive(false);
    }

    public static void Hide(ItemTooltipTarget target)
    {
        if (instance != null && instance.owner == target) HideAll();
    }

    public static void HideAll()
    {
        if (instance == null) return;
        instance.owner = null;
        instance.panel.gameObject.SetActive(false);
    }

    public static void BeginDrag() { IsDragging = true; HideAll(); }
    public static void EndDrag() { IsDragging = false; HideAll(); }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        gameObject.layer = 5;
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        canvas.pixelPerfect = true;
        canvasRect = GetComponent<RectTransform>();
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        var frame = CreateImage("Tooltip", transform, new Color32(94, 58, 35, 255));
        panel = frame.rectTransform;
        panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
        panel.pivot = new Vector2(0, 1);
        var fill = CreateImage("Paper", panel, new Color32(242, 217, 174, 255));
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.one * 3;
        fill.rectTransform.offsetMax = Vector2.one * -3;
        icon = CreateImage("Icon", panel, Color.white);
        icon.preserveAspect = true;
        SetRect(icon.rectTransform, new Vector2(16, -14), new Vector2(40, 40));

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        foreach (var candidate in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (candidate.name.StartsWith("Jersey10")) { font = candidate; break; }
        title = CreateText("Title", panel, font, 28);
        body = CreateText("Body", panel, font, 24);
        title.fontStyle = FontStyles.Bold;
        panel.gameObject.SetActive(false);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, int size)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.font = font;
        text.fontSize = size;
        text.color = new Color32(69, 43, 29, 255);
        text.raycastTarget = false;
        text.richText = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void LateUpdate()
    {
        if (owner == null || !owner.isActiveAndEnabled || IsDragging)
        {
            HideAll();
            return;
        }
        var content = owner.GetContent();
        if (content.IsEmpty || Time.unscaledTime < showAt)
        {
            panel.gameObject.SetActive(false);
            return;
        }
        float width = Mathf.Min(360, canvasRect.rect.width - 16);
        if (previousTitle != content.Title || previousBody != content.Body || previousIcon != content.Icon || previousWidth != width)
        {
            previousTitle = title.text = content.Title;
            previousBody = body.text = content.Body ?? "";
            previousIcon = icon.sprite = content.Icon;
            previousWidth = width;
            icon.gameObject.SetActive(content.Icon != null);
            float titleX = content.Icon != null ? 68 : 16;
            float headingHeight = Mathf.Max(40, title.GetPreferredValues(content.Title, width - titleX - 16, 0).y);
            float bodyHeight = string.IsNullOrEmpty(content.Body) ? 0 : body.GetPreferredValues(content.Body, width - 32, 0).y;
            float height = Mathf.Min(canvasRect.rect.height - 16, 28 + headingHeight + (bodyHeight > 0 ? bodyHeight + 8 : 0));
            panel.sizeDelta = new Vector2(width, height);
            SetRect(title.rectTransform, new Vector2(titleX, -14), new Vector2(width - titleX - 16, headingHeight));
            SetRect(body.rectTransform, new Vector2(16, -22 - headingHeight), new Vector2(width - 32, Mathf.Max(0, height - headingHeight - 36)));
        }
        if (Mouse.current != null) pointerPosition = Mouse.current.position.ReadValue();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerPosition + new Vector2(18, -18) * canvas.scaleFactor, null, out var position);
        panel.anchoredPosition = ClampPosition(position, panel.sizeDelta, canvasRect.rect);
        panel.gameObject.SetActive(true);
    }

    public static Vector2 ClampPosition(Vector2 position, Vector2 size, Rect bounds)
    {
        return new Vector2(Mathf.Clamp(position.x, bounds.xMin + 8, bounds.xMax - size.x - 8),
            Mathf.Clamp(position.y, bounds.yMin + size.y + 8, bounds.yMax - 8));
    }

    private void OnApplicationFocus(bool focused) { if (!focused) HideAll(); }
    private void OnDestroy() { if (instance == this) instance = null; }
}

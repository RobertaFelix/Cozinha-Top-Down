using TMPro;
using UnityEngine;
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
    private readonly Vector3[] corners = new Vector3[4];
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
        var slotBounds = BoundsInTooltipCanvas((RectTransform)owner.transform);
        var inventoryBounds = FindInventoryBounds(owner.transform);
        panel.anchoredPosition = SlotPosition(slotBounds, inventoryBounds, panel.sizeDelta, canvasRect.rect);
        panel.gameObject.SetActive(true);
    }

    private Rect FindInventoryBounds(Transform target)
    {
        Rect? gridBounds = null;
        for (var parent = target.parent; parent != null; parent = parent.parent)
        {
            if (!(parent is RectTransform rect)) continue;
            var image = parent.GetComponent<Image>();
            var rawImage = parent.GetComponent<RawImage>();
            if ((image != null && image.sprite != null && image.color.a > .01f) ||
                (rawImage != null && rawImage.texture != null && rawImage.color.a > .01f))
                return BoundsInTooltipCanvas(rect);
            if (!gridBounds.HasValue && parent.GetComponent<LayoutGroup>() != null)
            {
                // Some existing shop grids are only 100px wide while their cells overflow it.
                // Use the actual cells instead of that placeholder rect or a transparent full-screen page.
                foreach (Transform child in parent)
                {
                    if (!(child is RectTransform cell) || child.GetComponent<ItemTooltipTarget>() == null) continue;
                    var bounds = BoundsInTooltipCanvas(cell);
                    if (!gridBounds.HasValue) gridBounds = bounds;
                    else
                    {
                        var previous = gridBounds.Value;
                        gridBounds = Rect.MinMaxRect(Mathf.Min(previous.xMin, bounds.xMin), Mathf.Min(previous.yMin, bounds.yMin),
                            Mathf.Max(previous.xMax, bounds.xMax), Mathf.Max(previous.yMax, bounds.yMax));
                    }
                }
            }
            var ancestorCanvas = parent.GetComponent<Canvas>();
            if (ancestorCanvas != null && ancestorCanvas.isRootCanvas) break;
        }
        return gridBounds ?? canvasRect.rect;
    }

    private Rect BoundsInTooltipCanvas(RectTransform rect)
    {
        if (rect == canvasRect) return canvasRect.rect;
        rect.GetWorldCorners(corners);
        var sourceCanvas = rect.GetComponentInParent<Canvas>();
        var camera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? sourceCanvas.worldCamera : null;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var corner in corners)
        {
            var screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // The top-left box pivot grows down/right beside the slot. Mouse motion inside a slot is irrelevant.
    public static Vector2 SlotPosition(Rect slot, Rect inventory, Vector2 size, Rect screen)
    {
        const float gap = 20f;
        float right = slot.xMax + gap;
        float left = slot.xMin - gap - size.x;
        bool fitsRight = right + size.x <= Mathf.Min(inventory.xMax, screen.xMax - 8f);
        float x = fitsRight ? right : left;
        if (x < screen.xMin + 8f && right + size.x <= screen.xMax - 8f) x = right;
        float y = slot.yMax + gap;
        // Extremely narrow screens may not fit either side: place above/below without covering the item.
        if (x < screen.xMin + 8f || x + size.x > screen.xMax - 8f)
        {
            x = Mathf.Clamp(slot.center.x - size.x * .5f, screen.xMin + 8f, screen.xMax - size.x - 8f);
            y = slot.yMax + gap + size.y;
            if (y > screen.yMax - 8f) y = slot.yMin - gap;
        }
        return ClampPosition(new Vector2(x, y), size, screen);
    }

    public static Vector2 ClampPosition(Vector2 position, Vector2 size, Rect bounds)
    {
        return new Vector2(Mathf.Clamp(position.x, bounds.xMin + 8, bounds.xMax - size.x - 8),
            Mathf.Clamp(position.y, bounds.yMin + size.y + 8, bounds.yMax - 8));
    }

    private void OnApplicationFocus(bool focused) { if (!focused) HideAll(); }
    private void OnDestroy() { if (instance == this) instance = null; }
}

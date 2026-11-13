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
    private Image headerFrame;
    private Image bodyFrame;
    private ItemTooltipStyle style;
    private float minimumPanelHeight = 128f;
    private float panelWidth = 360f;
    private float minimumPanelWidth = 220f;
    private float contentLeft = 20f;
    private float contentRight = 20f;
    private float titleTop = 16f;
    private float headerBottomPadding = 16f;
    private float bodyTopPadding = 8f;
    private float bottomPadding = 20f;
    private readonly System.Collections.Generic.List<Material> textMaterials = new System.Collections.Generic.List<Material>();
    private TextMeshProUGUI title;
    private TextMeshProUGUI body;
    private readonly Vector3[] corners = new Vector3[4];
    private float showAt;
    private string previousTitle;
    private string previousBody;
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

        panel = new GameObject("Tooltip", typeof(RectTransform)).GetComponent<RectTransform>();
        panel.SetParent(transform, false);
        panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
        panel.pivot = new Vector2(0, 1);
        style = Resources.Load<ItemTooltipStyle>("ItemTooltipStyle");
        headerFrame = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(TooltipFrameImage)).GetComponent<TooltipFrameImage>();
        headerFrame.transform.SetParent(panel, false);
        headerFrame.raycastTarget = false;
        ((TooltipFrameImage)headerFrame).repairTopCorners = true;
        bodyFrame = CreateImage("DescriptionPanel", panel, new Color32(220, 185, 138, 255));
        if (style != null)
        {
            float scale = Mathf.Max(1f, style.pixelScale);
            panelWidth = Mathf.Max(100f, style.width);
            minimumPanelWidth = Mathf.Min(panelWidth, Mathf.Max(100f, style.minimumWidth));
            minimumPanelHeight = Mathf.Max(0f, style.minimumHeight);
            ConfigureFrame(headerFrame, style.headerBackground, scale);
            ConfigureFrame(bodyFrame, style.bodyBackground, scale);
            if (style.headerBackground != null)
            {
                contentLeft = style.headerBackground.border.x * scale + 8f;
                contentRight = style.headerBackground.border.z * scale + 8f;
                titleTop = style.headerBackground.border.w * scale + 6f;
                headerBottomPadding = style.headerBackground.border.y * scale + 6f;
            }
            if (style.bodyBackground != null) bottomPadding = style.bodyBackground.border.y * scale + 8f;
        }

        TMP_FontAsset font = style != null && style.font != null ? style.font : TMP_Settings.defaultFontAsset;
        title = CreateText("Title", panel, font, style != null ? style.titleFontSize : 56);
        body = CreateText("Body", panel, font, style != null ? style.bodyFontSize : 28);
        title.fontStyle = FontStyles.Normal;
        title.color = style != null ? style.titleColor : new Color32(69, 43, 29, 255);
        title.extraPadding = false;
        title.UpdateMeshPadding();
        body.color = style != null ? style.descriptionColor : Color.black;
        body.richText = true;
        panel.gameObject.SetActive(false);
    }

    private void ConfigureFrame(Image image, Sprite sprite, float scale)
    {
        if (sprite == null) return;
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Tiled;
        image.pixelsPerUnitMultiplier = canvas.referencePixelsPerUnit / (sprite.pixelsPerUnit * scale);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float size)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.font = font;
        // Keep tooltip text clean without changing shared font materials elsewhere in the UI.
        if (font != null && font.material != null)
        {
            var material = new Material(font.material) { name = "Tooltip Text - No Outline" };
            foreach (var property in new[] { "_OutlineWidth", "_OutlineSoftness", "_FaceDilate", "_WeightBold" })
                if (material.HasProperty(property)) material.SetFloat(property, 0f);
            if (material.HasProperty("_UnderlayColor")) material.SetColor("_UnderlayColor", Color.clear);
            foreach (var keyword in new[] { "OUTLINE_ON", "UNDERLAY_ON", "UNDERLAY_INNER", "GLOW_ON", "BEVEL_ON" })
                material.DisableKeyword(keyword);
            textMaterials.Add(material);
            text.fontSharedMaterial = material;
        }
        text.fontStyle = FontStyles.Normal;
        text.fontSize = size;
        text.color = new Color32(69, 43, 29, 255);
        text.raycastTarget = false;
        text.richText = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
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
        float maximumWidth = Mathf.Min(panelWidth, canvasRect.rect.width - 16);
        if (previousTitle != content.Title || previousBody != content.Body || previousWidth != maximumWidth)
        {
            previousTitle = title.text = content.Title;
            previousBody = content.Body ?? "";
            body.text = ItemTooltipFormatting.FormatBody(previousBody, style);
            previousWidth = maximumWidth;
            float naturalWidth = Mathf.Max(title.GetPreferredValues(title.text).x, body.GetPreferredValues(body.text).x)
                + contentLeft + contentRight;
            float width = Mathf.Clamp(Mathf.Ceil(naturalWidth), Mathf.Min(minimumPanelWidth, maximumWidth), maximumWidth);
            float textWidth = Mathf.Max(1, width - contentLeft - contentRight);
            float titleHeight = Mathf.Ceil(title.GetPreferredValues(title.text, textWidth, 0).y);
            float bodyHeight = string.IsNullOrEmpty(previousBody) ? 0 : Mathf.Ceil(body.GetPreferredValues(body.text, textWidth, 0).y);
            float headerHeight = Mathf.Ceil(titleTop + titleHeight + headerBottomPadding);
            float bodyTop = headerHeight + bodyTopPadding;
            float height = Mathf.Ceil(Mathf.Max(minimumPanelHeight, bodyTop + bodyHeight + bottomPadding));
            panel.sizeDelta = new Vector2(width, height);
            SetRect(headerFrame.rectTransform, Vector2.zero, new Vector2(width, headerHeight));
            SetRect(bodyFrame.rectTransform, new Vector2(0, -headerHeight), new Vector2(width, height - headerHeight));
            SetRect(title.rectTransform, new Vector2(contentLeft, -titleTop), new Vector2(textWidth, titleHeight));
            SetRect(body.rectTransform, new Vector2(contentLeft, -bodyTop), new Vector2(textWidth, bodyHeight));
        }
        var slotBounds = BoundsInTooltipCanvas(owner.TooltipAnchor != null ? owner.TooltipAnchor : (RectTransform)owner.transform);
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
    private void OnDestroy()
    {
        if (instance == this) instance = null;
        foreach (var material in textMaterials) if (material != null) Destroy(material);
    }
}

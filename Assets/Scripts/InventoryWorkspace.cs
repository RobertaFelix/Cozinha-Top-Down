using UnityEngine;
using UnityEngine.UI;

// Move the existing panels so the hotbar keeps its physical slots and save identity.
[DefaultExecutionOrder(-10)]
public sealed class InventoryWorkspace : MonoBehaviour
{
    private GridLayoutGroup bagGrid, barGrid;
    private RectOffset bagPadding, barPadding;
    private Vector2 barSize, barCell, barSpacing;
    private bool barImageEnabled;
    [SerializeField] private Color hotbarHighlight = new Color(.78f, .45f, .15f, .26f);
    private Color[] hudSlotColors;
    private Image[] hotbarCells;
    [SerializeField] private Vector2 backpackPosition = new Vector2(-400, -55);
    [SerializeField] private Vector2 storagePosition = new Vector2(465, 0);
    [SerializeField] private RectTransform recipePage;
    [SerializeField] private RectTransform tabs;
    [SerializeField] private RectTransform dishesPage;
    [SerializeField, Min(.05f)] private float hudMotionSeconds = .18f;
    private CanvasGroup hudGroup;
    private Vector3 hudScale;
    private Vector2 hudPosition;
    private bool hudWanted = true;
    private float hudProgress = 1f;
    private RectTransform departingHud;
    private CanvasGroup departingGroup;
    private float departure;
    private Vector2 departurePosition;
    private Vector3 departureScale;
    private static InventoryWorkspace current;
    internal static bool HotbarInBackpack => current && current.docked;
    internal static bool HotbarAcceptsInput => !current || current.docked || current.hudWanted;
    private RectTransform bag, bar, storage;
    private Transform bagParent, barParent;
    private int bagIndex, barIndex;
    private Vector2 bagPosition, barPosition, storagePreviousPosition;
    private Vector2 barAnchorMin, barAnchorMax, barPivot;
    private Vector3 barScale;
    private bool bagActive, docked;

    private void Awake()
    {
        current = this;
        bag = (RectTransform)GetComponent<InventoryController>().inventoryPanel.transform;
        bar = (RectTransform)GetComponent<HotbarController>().hotbarPanel.transform;
        hudScale = bar.localScale;
        hudPosition = bar.anchoredPosition;
        hudGroup = bar.GetComponent<CanvasGroup>();
        if (!hudGroup) hudGroup = bar.gameObject.AddComponent<CanvasGroup>();
        bagGrid = bag.GetComponent<GridLayoutGroup>();
        barGrid = bar.GetComponent<GridLayoutGroup>();
    }

    public static void OpenStorage(GameObject panel)
    {
        if (!current) return;
        current.AttachStorage((RectTransform)panel.transform);
    }

    private void AttachStorage(RectTransform panel)
    {
        if (storage == panel) return;
        storage = panel;
        storagePreviousPosition = panel.anchoredPosition;
        PositionOnScreen(panel, storagePosition);
        bagParent = bag.parent;
        bagIndex = bag.GetSiblingIndex();
        bagPosition = bag.anchoredPosition;
        bagActive = bag.gameObject.activeSelf;
        bag.SetParent(panel.parent, false);
        PositionOnScreen(bag, backpackPosition);
        bag.gameObject.SetActive(true);
        Dock();
    }

    public static void CloseStorage(GameObject panel)
    {
        if (!current || !panel || current.storage != panel.transform) return;
        current.DetachStorage();
    }

    public static void RestoreHud()
    {
        if (current) current.Undock();
    }

    public static void SetShopHotbarVisible(bool visible)
    {
        if (current) current.SetHudVisible(visible);
    }

    private void SetHudVisible(bool visible)
    {
        hudWanted = visible;
        hudGroup.blocksRaycasts = hudGroup.interactable = visible || docked;
        if (visible && !bar.gameObject.activeSelf) bar.gameObject.SetActive(true);
    }

    // Only the artwork stays behind while the real slots move to the backpack.
    private void CaptureHud()
    {
        if (departingHud) Destroy(departingHud.gameObject);
        departingHud = new GameObject("Hotbar transition", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        departingHud.SetParent(bar.parent, false);
        departingHud.anchorMin = bar.anchorMin;
        departingHud.anchorMax = bar.anchorMax;
        departingHud.pivot = bar.pivot;
        departingHud.sizeDelta = bar.sizeDelta;
        departingHud.anchoredPosition = departurePosition = bar.anchoredPosition;
        departingHud.localScale = departureScale = bar.localScale;
        departingHud.SetSiblingIndex(bar.GetSiblingIndex());
        departingGroup = departingHud.GetComponent<CanvasGroup>();
        departingGroup.blocksRaycasts = departingGroup.interactable = false;
        departure = hudGroup.alpha;
        foreach (var graphic in bar.GetComponentsInChildren<MaskableGraphic>())
        {
            if (!graphic.isActiveAndEnabled || graphic.color.a <= 0) continue;
            // Bake the current UI geometry, including quantity glyphs, without duplicating slot logic.
            var mesh = graphic.canvasRenderer.GetMesh();
            if (!mesh || mesh.vertexCount == 0) continue;
            var copy = new GameObject("Graphic", typeof(RectTransform), typeof(CanvasRenderer));
            var rect = (RectTransform)copy.transform;
            rect.SetParent(departingHud, false);
            rect.position = graphic.transform.position;
            rect.rotation = graphic.transform.rotation;
            var scale = graphic.transform.lossyScale;
            var parentScale = departingHud.lossyScale;
            rect.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, 1);
            var renderer = copy.GetComponent<CanvasRenderer>();
            renderer.SetMaterial(graphic.materialForRendering, graphic.mainTexture);
            renderer.SetMesh(mesh);
        }
    }

    private void AnimateDeparture()
    {
        if (!departingHud) return;
        departure = Mathf.MoveTowards(departure, 0f, Time.unscaledDeltaTime / hudMotionSeconds);
        float eased = Mathf.SmoothStep(0f, 1f, departure);
        departingGroup.alpha = eased;
        departingHud.localScale = Vector3.Scale(departureScale, new Vector3(.93f + .07f * eased, .25f + .75f * eased, 1));
        departingHud.anchoredPosition = departurePosition + Vector2.down * (1-eased) * (barSize.y * .375f + 18f);
        if (departure == 0f) Destroy(departingHud.gameObject);
    }

    private void AnimateHud()
    {
        if (!bar.gameObject.activeSelf && !hudWanted) return;
        hudProgress = Mathf.MoveTowards(hudProgress, hudWanted ? 1f : 0f, Time.unscaledDeltaTime / hudMotionSeconds);
        float eased = Mathf.SmoothStep(0, 1, hudProgress);
        float lift = Mathf.Sin(Mathf.PI * hudProgress) * hudProgress;
        float height = Mathf.Lerp(.25f, 1f, eased) + .12f * lift;
        bar.localScale = Vector3.Scale(hudScale, new Vector3(.93f + .07f * eased, height, 1));
        bar.anchoredPosition = hudPosition + Vector2.down * ((1f-height) * bar.rect.height * bar.pivot.y + (1f-eased)*18f);
        hudGroup.alpha = eased;
        if (hudProgress == 0 && !hudWanted) bar.gameObject.SetActive(false);
    }

    private void DetachStorage()
    {
        Undock();
        storage.anchoredPosition = storagePreviousPosition;
        storage = null;
        bag.SetParent(bagParent, false);
        bag.SetSiblingIndex(bagIndex);
        bag.anchoredPosition = bagPosition;
        bag.gameObject.SetActive(bagActive);
    }

    private void LateUpdate()
    {
        AnimateDeparture();
        if (bag.gameObject.activeInHierarchy)
        {
            PositionOnScreen(bag, backpackPosition);
            if (storage) PositionOnScreen(storage, storagePosition);
            Dock();
        }
        else
        {
            Undock();
            AnimateHud();
        }
        if (tabs && tabs.gameObject.activeInHierarchy)
        {
            PositionOnScreen(recipePage, backpackPosition);
            SetHudVisible(!recipePage.gameObject.activeInHierarchy);
            // Attach the tab strip to the upper-left of both identically sized pages.
            var size = bag.rect.size;
            PositionOnScreen(tabs, backpackPosition + new Vector2((tabs.rect.width-size.x)*.5f,
                (size.y+tabs.rect.height)*.5f-6f));
            if (dishesPage.gameObject.activeInHierarchy) PositionOnScreen(dishesPage, storagePosition);
        }
    }

    // Offsets are measured in the same 1920-wide reference space as the inventory artwork.
    private static void PositionOnScreen(RectTransform panel, Vector2 offset)
    {
        var canvas = panel.GetComponentInParent<Canvas>();
        if (!canvas) return;
        var viewport = canvas.rootCanvas.pixelRect;
        var point = viewport.center + offset * (viewport.width / 1920f);
        var camera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)panel.parent, point, camera, out var world))
            panel.position = world;
    }

    internal static RectTransform FirstHotbarSlot()
    {
        if (!current || !current.docked || !current.bag.gameObject.activeInHierarchy) return null;
        foreach (Transform child in current.bar)
            if (child.TryGetComponent<Slot>(out _)) return (RectTransform)child;
        return null;
    }

    private void Dock()
    {
        if (!docked)
        {
            if (bar.gameObject.activeInHierarchy && hudGroup.alpha > .01f) CaptureHud();
            barParent = bar.parent;
            barIndex = bar.GetSiblingIndex();
            barPosition = hudPosition;
            barAnchorMin = bar.anchorMin; barAnchorMax = bar.anchorMax; barPivot = bar.pivot;
            barScale = hudScale;
            hudGroup.alpha = 1;
            hudGroup.blocksRaycasts = hudGroup.interactable = true;
            bagPadding = bagGrid.padding;
            barPadding = barGrid.padding;
            barSize = bar.sizeDelta;
            barCell = barGrid.cellSize;
            barSpacing = barGrid.spacing;
            var slots = bar.GetComponentsInChildren<Slot>(true);
            hotbarCells = new Image[slots.Length];
            hudSlotColors = new Color[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                hotbarCells[i] = slots[i].GetComponent<Image>();
                hudSlotColors[i] = hotbarCells[i].color;
                hotbarCells[i].color = hotbarHighlight;
            }
            barImageEnabled = bar.GetComponent<Image>().enabled;
            bar.GetComponent<Image>().enabled = false;
            bagGrid.padding = new RectOffset(bagPadding.left, bagPadding.right,
                bagPadding.top + Mathf.RoundToInt(bagGrid.cellSize.y + bagGrid.spacing.y), bagPadding.bottom);
            barGrid.cellSize = bagGrid.cellSize;
            barGrid.spacing = bagGrid.spacing;
            barGrid.padding = new RectOffset(bagPadding.left, bagPadding.right, 0, 0);
            docked = true;
        }
        // Siblings keep backpack-only searches and inventory saves independent.
        if (bar.parent != bag.parent) bar.SetParent(bag.parent, false);
        bar.localScale = bag.localScale;
        bar.anchorMin = bag.anchorMin; bar.anchorMax = bag.anchorMax;
        bar.pivot = new Vector2(.5f, 1);
        bar.sizeDelta = new Vector2(bag.rect.width, bagGrid.cellSize.y);
        bar.anchoredPosition = bag.anchoredPosition + Vector2.up * (bag.rect.height * (1 - bag.pivot.y) - bagPadding.top);
        if (bar.GetSiblingIndex() < bag.GetSiblingIndex()) bar.SetAsLastSibling();
        bar.gameObject.SetActive(true);
    }

    private void Undock()
    {
        if (!docked) return;
        if (departingHud) { Destroy(departingHud.gameObject); departingHud = null; }
        docked = false;
        hudProgress = 0f;
        SetHudVisible(hudWanted);
        hudGroup.alpha = 0f;
        for (int i = 0; i < hotbarCells.Length; i++)
            if (hotbarCells[i]) hotbarCells[i].color = hudSlotColors[i];
        bar.SetParent(barParent, false);
        bar.SetSiblingIndex(barIndex);
        bar.anchorMin = barAnchorMin; bar.anchorMax = barAnchorMax; bar.pivot = barPivot;
        bar.anchoredPosition = barPosition;
        bar.localScale = barScale;
        bar.sizeDelta = barSize;
        barGrid.padding = barPadding;
        barGrid.cellSize = barCell;
        barGrid.spacing = barSpacing;
        bagGrid.padding = bagPadding;
        bar.GetComponent<Image>().enabled = barImageEnabled;
    }

    public static bool IsNavigationTarget(Transform target)
    {
        var panel = InventoryPanelSession.CurrentPanel;
        if (!panel || !target.gameObject.activeInHierarchy) return false;
        return target.IsChildOf(panel) || (current && current.storage == panel &&
            (target.IsChildOf(current.bag) || target.IsChildOf(current.bar)));
    }

    public static void QuickMove(Slot slot)
    {
        if (!current || !current.bag.gameObject.activeInHierarchy) return;
        var source = slot.transform.parent;
        if (source == current.storage)
        {
            InventoryStacking.Transfer(slot, current.bag, true);
            InventoryStacking.Transfer(slot, current.bar, true);
            InventoryStacking.Transfer(slot, current.bag);
            InventoryStacking.Transfer(slot, current.bar);
        }
        else if (source == current.bag || source == current.bar)
            InventoryStacking.Transfer(slot, current.storage ? current.storage : source == current.bag ? current.bar : current.bag);
    }
}

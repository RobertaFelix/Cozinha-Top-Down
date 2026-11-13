using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Reservations stay in the inventory until a transfer commits, so cancellation and saves are lossless.
public sealed class InventoryStackCursor : MonoBehaviour
{
    private sealed class Reservation
    {
        public Slot slot;
        public InventoryItem item;
        public GameObject visual;
        public CanvasGroup group;
        public float alpha;
        public int amount;
    }
    private sealed class DistributionPreview
    {
        public Slot slot;
        public GameObject visual;
        public CanvasGroup originalGroup;
        public float originalAlpha;
        public int finalAmount;
    }
    private readonly List<DistributionPreview> previews = new List<DistributionPreview>();
    public static int? PreviewAmount(Slot slot)
    {
        if (instance == null) return null;
        var preview = instance.previews.Find(x => x.slot == slot);
        return preview != null ? preview.finalAmount : (int?)null;
    }

    private enum RightAction { None, Collect, Deposit }
    private const float RepeatDelay = .35f;
    private const float RepeatInterval = .08f;
    private static InventoryStackCursor instance;
    private readonly List<Reservation> reservations = new List<Reservation>();
    private GameObject prefab;
    private int amount;
    private bool allowSwap;
    private bool originExhausted;
    private RightAction repeatAction;
    private float repeatAt;
    private int handledLeftFrame = -1;
    private int handledRightFrame = -1;
    private bool waitingForRelease;
    private bool leftDragged;
    private bool distributing;
    private readonly List<Slot> distributionSlots = new List<Slot>();
    private Vector2 pressPosition;
    private RectTransform cursor;
    private Image icon;
    private TMP_Text count;
    // Both click-held items and legacy drag handlers share the same screen overlay.
    public static Canvas DragCanvas => GetCursor().GetComponent<Canvas>();

    public static bool IsHolding => instance != null && instance.amount > 0;
    public static int HeldAmount => IsHolding ? instance.amount : 0;
    public static int ReservedFrom(Slot slot)
    {
        if (!IsHolding) return 0;
        var r = instance.reservations.Find(x => x.slot == slot);
        return r != null ? r.amount : 0;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; }

    private static bool Selectable(Slot slot) => slot != null && slot.isActiveAndEnabled &&
        slot.inventoryItem?.itemPrefab != null && slot.inventoryItem.amount > 0 &&
        slot.currentItem != null && slot.currentItem.transform.parent == slot.transform;

    private static InventoryStackCursor GetCursor()
    {
        if (instance == null)
        {
            var root = new GameObject("InventoryStackCursor", typeof(RectTransform), typeof(Canvas));
            instance = root.AddComponent<InventoryStackCursor>();
        }
        return instance;
    }

    public static bool Pick(Slot slot, bool half = false)
    {
        if (!Selectable(slot) || Time.timeScale <= 0 || (!IsHolding && ItemTooltipView.IsDragging)) return false;
        var held = GetCursor();
        if (IsHolding && held.prefab != slot.inventoryItem.itemPrefab) return false;
        if (!IsHolding) { held.Begin(slot); held.allowSwap = false; }
        int available = slot.inventoryItem.amount - ReservedFrom(slot);
        bool picked = held.Take(slot, half ? Mathf.CeilToInt(available / 2f) : 1);
        held.repeatAction = half ? RightAction.None : RightAction.Collect;
        held.repeatAt = Time.unscaledTime + RepeatDelay;
        return picked;
    }

    private static RectTransform keyboardAnchor;
    public static void AnchorTo(RectTransform anchor) => keyboardAnchor = anchor;

    public static void ActivateSlot(Slot slot)
    {
        if (IsHolding) { TryPlace(slot); return; }
        if (!Selectable(slot) || ItemTooltipView.IsDragging || Time.timeScale <= 0) return;
        var held = GetCursor();
        held.Begin(slot);
        held.allowSwap = true;
        held.Take(slot, slot.inventoryItem.amount);
    }

    public static void PointerDown(Slot slot, PointerEventData data)
    {
        if (Time.timeScale <= 0) return;
        if (data.button == PointerEventData.InputButton.Right)
        {
            if (instance != null && instance.handledRightFrame == Time.frameCount) return;
            if (IsHolding) instance.RightDown(slot);
            else Pick(slot, ShiftHeld);
            if (instance != null) instance.handledRightFrame = Time.frameCount;
            return;
        }
        if (data.button != PointerEventData.InputButton.Left) return;
        if (!IsHolding && ShiftHeld)
        {
            InventoryWorkspace.QuickMove(slot);
            return;
        }
        if (instance != null && instance.handledLeftFrame == Time.frameCount) return;
        if (IsHolding)
        {
            instance.handledLeftFrame = Time.frameCount;
            instance.BeginDistribution(slot);
            return;
        }
        if (!Selectable(slot) || ItemTooltipView.IsDragging) return;
        var held = GetCursor();
        held.Begin(slot);
        held.allowSwap = true;
        held.Take(slot, slot.inventoryItem.amount);
        held.leftDragged = false;
        held.waitingForRelease = true;
        held.pressPosition = data.position;
        held.handledLeftFrame = Time.frameCount;
    }

    private static bool ShiftHeld => Keyboard.current != null &&
        (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

    private void Awake()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32761;
        var visual = new GameObject("HeldStack", typeof(RectTransform), typeof(Image));
        visual.transform.SetParent(transform, false);
        cursor = visual.GetComponent<RectTransform>();
        icon = visual.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.color = new Color(1, 1, 1, .6f);
        icon.raycastTarget = false;
        var label = new GameObject("Quantity", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(cursor, false);
        count = label.GetComponent<TextMeshProUGUI>();
        count.font = TMP_Settings.defaultFontAsset;
        count.fontSize = 22;
        count.color = Color.white;
        count.alignment = TextAlignmentOptions.TopRight;
        count.raycastTarget = false;
        count.rectTransform.anchorMin = count.rectTransform.anchorMax = new Vector2(1, 1);
        count.rectTransform.pivot = new Vector2(1, 1);
        count.rectTransform.anchoredPosition = new Vector2(-2, -2);
        count.rectTransform.sizeDelta = new Vector2(180, 55);
        cursor.gameObject.SetActive(false);
    }

    private void Begin(Slot slot)
    {
        reservations.Clear();
        originExhausted = false;
        prefab = slot.inventoryItem.itemPrefab;
        var image = slot.currentItem.GetComponentInChildren<Image>();
        var sprite = slot.currentItem.GetComponentInChildren<SpriteRenderer>();
        icon.sprite = image != null ? image.sprite : sprite != null ? sprite.sprite : null;
        var rect = image != null ? image.rectTransform : slot.currentItem.GetComponent<RectTransform>();
        if (rect != null)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = slot.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var bottom = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var top = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            cursor.sizeDelta = new Vector2(Mathf.Abs(top.x - bottom.x), Mathf.Abs(top.y - bottom.y));
        }
        else cursor.sizeDelta = new Vector2(64, 64);
        MatchQuantityStyle(slot.QuantityLabel);
        cursor.gameObject.SetActive(true);
        if (Mouse.current != null) cursor.position = Mouse.current.position.ReadValue();
        ItemTooltipView.BeginDrag();
    }

    private void MatchQuantityStyle(TMP_Text template)
    {
        if (template == null) return;
        count.gameObject.SetActive(false);
        Destroy(count.gameObject);
        count = Instantiate(template, cursor, false);
        count.name = "Quantity";
        count.gameObject.SetActive(true);
        count.enabled = true;
        count.raycastTarget = false;
        count.alignment = TextAlignmentOptions.TopRight;
        count.margin = Vector4.zero; // Slot-specific margins must not offset the cursor label.
        var rect = count.rectTransform;
        var originalRect = template.rectTransform;
        var corners = new Vector3[4];
        originalRect.GetWorldCorners(corners);
        var canvas = template.canvas;
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var bottom = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        var top = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        rect.localScale = new Vector3(
            Mathf.Abs(top.x - bottom.x) / Mathf.Max(.001f, originalRect.rect.width),
            Mathf.Abs(top.y - bottom.y) / Mathf.Max(.001f, originalRect.rect.height), 1);
        rect.anchorMin = rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.sizeDelta = originalRect.rect.size;
        rect.anchoredPosition = new Vector2(-2, -2);
        // Only the image is translucent. The cloned label keeps the slot's font and material.
        var color = template.color;
        color.a = 1f;
        count.color = color;
    }

    private bool SourcesValid()
    {
        foreach (var r in reservations)
            if (r.amount > 0 && (!Selectable(r.slot) || r.slot.inventoryItem != r.item ||
                r.slot.currentItem != r.visual || r.item.amount < r.amount)) return false;
        return true;
    }

    private bool Take(Slot slot, int requested)
    {
        if (!Selectable(slot) || slot.inventoryItem.itemPrefab != prefab) return false;
        int taken = Mathf.Max(0, Mathf.Min(requested, slot.inventoryItem.amount - ReservedFrom(slot), InventoryStacking.MaxStackSize - amount));
        if (taken == 0) return false;
        var r = reservations.Find(x => x.slot == slot && x.item == slot.inventoryItem);
        if (r == null)
        {
            var group = slot.currentItem.GetComponent<CanvasGroup>();
            if (group == null) group = slot.currentItem.AddComponent<CanvasGroup>();
            r = new Reservation { slot = slot, item = slot.inventoryItem, visual = slot.currentItem, group = group, alpha = group.alpha };
            reservations.Add(r);
        }
        r.amount += taken;
        amount += taken;
        if (reservations[0].item.amount == reservations[0].amount) originExhausted = true;
        Refresh();
        return true;
    }

    private void Refresh()
    {
        count.text = amount.ToString();
        foreach (var r in reservations)
        {
            if (r.group != null) r.group.alpha = r.amount > 0 && r.item.amount == r.amount ? 0f : r.alpha;
            if (r.slot != null) r.slot.UpdateQuantityDisplay();
        }
    }

    private void RightDown(Slot slot)
    {
        // Returning to the original stack withdraws more; other slots receive items.
        bool atOrigin = !originExhausted && reservations.Count > 0 && reservations[0].slot == slot &&
            Selectable(slot) && slot.inventoryItem.amount > ReservedFrom(slot);
        repeatAction = atOrigin ? RightAction.Collect : RightAction.Deposit;
        if (ShiftHeld)
        {
            if (Selectable(slot) && slot.inventoryItem.itemPrefab == prefab) Take(slot, Mathf.CeilToInt((slot.inventoryItem.amount - ReservedFrom(slot)) / 2f));
            repeatAction = RightAction.None;
        }
        else RightStep(slot);
        repeatAt = Time.unscaledTime + RepeatDelay;
    }
    private void RightStep(Slot slot)
    {
        if (repeatAction == RightAction.Collect)
        {
            if (!Take(slot, 1) || (Selectable(slot) && slot.inventoryItem.amount == ReservedFrom(slot)))
                repeatAction = RightAction.None;
        }
        else if (repeatAction == RightAction.Deposit) TryPlace(slot, 1);
    }

    private void Update()
    {
        if (!IsHolding) return;
        if (!SourcesValid() || Time.timeScale <= 0 || Mouse.current == null) { Cancel(); return; }
        var mouse = Mouse.current;
        var position = keyboardAnchor ? (Vector2)keyboardAnchor.position : mouse.position.ReadValue();
        cursor.position = position;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Cancel(); return; }
        var slot = SlotAt(position);
        if (waitingForRelease && mouse.leftButton.isPressed &&
            Vector2.Distance(position, pressPosition) >= (EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 5))
            leftDragged = true;
        if (mouse.leftButton.wasPressedThisFrame && handledLeftFrame != Time.frameCount)
        {
            handledLeftFrame = Time.frameCount;
            BeginDistribution(slot);
        }
        if (!IsHolding) return;
        if (distributing)
        {
            if (mouse.leftButton.isPressed) VisitDistributionSlot(slot);
            RefreshPreviews();
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                ClearPreviews();
                distributing = false;
                var visited = distributionSlots.ToArray();
                distributionSlots.Clear();
                if (visited.Length > 0) Distribute(visited);
                else if (!TryPlace(slot) && !TryServe(position)) Cancel();
            }
            return;
        }
        if (waitingForRelease && mouse.leftButton.wasReleasedThisFrame)
        {
            bool dragged = leftDragged || Vector2.Distance(position, pressPosition) >= (EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 5);
            waitingForRelease = false;
            if (dragged)
            {
                if (!TryPlace(slot)) TryServe(position);
                Cancel(); // Any remainder goes back to its original stacks on a drag release.
                return;
            }
        }
        if (mouse.rightButton.wasPressedThisFrame && handledRightFrame != Time.frameCount)
        {
            handledRightFrame = Time.frameCount;
            RightDown(slot);
        }
        if (!IsHolding) return;
        if (!mouse.rightButton.isPressed) repeatAction = RightAction.None;
        if (repeatAction != RightAction.None && Time.unscaledTime >= repeatAt)
        {
            RightStep(slot);
            repeatAt = Time.unscaledTime + RepeatInterval;
        }
    }

    private void BeginDistribution(Slot slot)
    {
        distributing = true;
        waitingForRelease = false;
        repeatAction = RightAction.None;
        distributionSlots.Clear();
        VisitDistributionSlot(slot);
        RefreshPreviews();
    }

    private int Capacity(Slot slot)
    {
        if (slot == null || !slot.isActiveAndEnabled) return 0;
        var item = slot.inventoryItem;
        if (item != null && item.itemPrefab != prefab) return 0;
        if (item == null && slot.currentItem != null) return 0;
        return Mathf.Max(0, InventoryStacking.MaxStackSize - (item != null ? item.amount - ReservedFrom(slot) : 0));
    }

    private void VisitDistributionSlot(Slot slot)
    {
        if (Capacity(slot) > 0 && !distributionSlots.Contains(slot)) distributionSlots.Add(slot);
    }

    // Round-robin allocation gives equal shares; earlier visited slots receive remainders.
    public static int Distribute(IEnumerable<Slot> visited)
    {
        if (!IsHolding || visited == null) return 0;
        var held = instance;
        if (!held.SourcesValid()) { Cancel(); return 0; }
        var allocations = held.PlanDistribution(visited, out var slots);
        int placed = 0;
        for (int i = 0; i < slots.Count; i++)
            if (allocations[i] > 0 && TryPlace(slots[i], allocations[i])) placed += allocations[i];
        return placed;
    }

    private int[] PlanDistribution(IEnumerable<Slot> visited, out List<Slot> slots)
    {
        slots = new List<Slot>();
        foreach (var slot in visited)
            if (Capacity(slot) > 0 && !slots.Contains(slot)) slots.Add(slot);
        var capacities = new int[slots.Count];
        var allocations = new int[slots.Count];
        for (int i = 0; i < slots.Count; i++) capacities[i] = Capacity(slots[i]);
        int remaining = amount;
        bool progressed;
        do
        {
            progressed = false;
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (allocations[i] >= capacities[i]) continue;
                allocations[i]++;
                remaining--;
                progressed = true;
            }
        } while (remaining > 0 && progressed);
        return allocations;
    }

    private void RefreshPreviews()
    {
        var allocations = PlanDistribution(distributionSlots, out var slots);
        for (int i = previews.Count - 1; i >= 0; i--)
        {
            int index = slots.IndexOf(previews[i].slot);
            if (index >= 0 && allocations[index] > 0) continue;
            var preview = previews[i];
            previews.RemoveAt(i);
            RemovePreview(preview);
        }
        for (int i = 0; i < slots.Count; i++)
        {
            if (allocations[i] <= 0) continue;
            var slot = slots[i];
            var preview = previews.Find(x => x.slot == slot);
            if (preview == null)
            {
                preview = new DistributionPreview { slot = slot };
                if (slot.currentItem != null)
                {
                    preview.originalGroup = slot.currentItem.GetComponent<CanvasGroup>();
                    if (preview.originalGroup == null) preview.originalGroup = slot.currentItem.AddComponent<CanvasGroup>();
                    preview.originalAlpha = preview.originalGroup.alpha;
                }
                preview.visual = Instantiate(prefab, slot.transform);
                preview.visual.name = "DistributionPreview";
                if (preview.visual.TryGetComponent<RectTransform>(out var rect)) rect.anchoredPosition = Vector2.zero;
                foreach (var script in preview.visual.GetComponentsInChildren<MonoBehaviour>(true))
                    if (!(script is Graphic)) script.enabled = false;
                foreach (var graphic in preview.visual.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                foreach (var collider in preview.visual.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
                var group = preview.visual.GetComponent<CanvasGroup>();
                if (group == null) group = preview.visual.AddComponent<CanvasGroup>();
                group.alpha = .5f;
                group.blocksRaycasts = false;
                group.interactable = false;
                previews.Add(preview);
            }
            preview.finalAmount = (slot.inventoryItem != null ? slot.inventoryItem.amount : 0) - ReservedFrom(slot) + allocations[i];
            if (preview.originalGroup != null) preview.originalGroup.alpha = 0f;
            slot.UpdateQuantityDisplay();
        }
    }

    private void RemovePreview(DistributionPreview preview)
    {
        if (preview.originalGroup != null) preview.originalGroup.alpha = preview.originalAlpha;
        if (preview.visual != null) { preview.visual.SetActive(false); Destroy(preview.visual); }
        if (preview.slot != null) preview.slot.UpdateQuantityDisplay();
    }

    private void ClearPreviews()
    {
        var previous = previews.ToArray();
        previews.Clear();
        foreach (var preview in previous) RemovePreview(preview);
    }

    private static Slot SlotAt(Vector2 position)
    {
        if (EventSystem.current == null) return null;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, results);
        foreach (var hit in results)
            if (hit.gameObject != null) return hit.gameObject.GetComponentInParent<Slot>();
        return null;
    }

    public static bool TryPlace(Slot target, int requested = int.MaxValue)
    {
        if (!IsHolding) return false;
        var held = instance;
        if (!held.SourcesValid()) { Cancel(); return false; }
        if (target == null || !target.isActiveAndEnabled) return false;
        var item = target.inventoryItem;
        if (item != null && item.itemPrefab != held.prefab)
        {
            // Preserve whole-stack swapping, but never swap a partial selection.
            var origin = held.reservations.Find(x => x.amount > 0);
            if (requested != int.MaxValue || !held.allowSwap || origin == null ||
                origin.amount != held.amount || origin.item.amount != held.amount || target.currentItem == null) return false;
            var from = origin.slot;
            var movingVisual = origin.visual;
            var movingItem = origin.item;
            var targetVisual = target.currentItem;
            Cancel();
            from.currentItem = targetVisual;
            from.inventoryItem = item;
            targetVisual.transform.SetParent(from.transform, false);
            if (targetVisual.TryGetComponent<RectTransform>(out var oldRect)) oldRect.anchoredPosition = Vector2.zero;
            target.currentItem = movingVisual;
            target.inventoryItem = movingItem;
            movingVisual.transform.SetParent(target.transform, false);
            if (movingVisual.TryGetComponent<RectTransform>(out var newRect)) newRect.anchoredPosition = Vector2.zero;
            from.UpdateQuantityDisplay();
            target.UpdateQuantityDisplay();
            return true;
        }
        if (item == null && target.currentItem != null) return false;
        int visible = item != null ? item.amount - ReservedFrom(target) : 0;
        int moved = Mathf.Min(held.amount, requested, InventoryStacking.MaxStackSize - visible);
        if (moved <= 0) return false;
        // Consume the target's reservation first: putting an item back is a release, not a duplicate.
        held.Consume(moved, target);
        if (target.inventoryItem == null)
        {
            target.currentItem = Instantiate(held.prefab, target.transform);
            if (target.currentItem.TryGetComponent<RectTransform>(out var rect)) rect.anchoredPosition = Vector2.zero;
            target.inventoryItem = new InventoryItem { itemPrefab = held.prefab, amount = moved };
        }
        else target.inventoryItem.amount += moved;
        held.ClearEmptySources();
        target.UpdateQuantityDisplay();
        if (held.amount == 0) Cancel();
        else held.Refresh();
        return true;
    }

    private void Consume(int quantity, Slot preferred = null)
    {
        var first = reservations.Find(x => x.slot == preferred && x.amount > 0);
        if (first != null) quantity -= ConsumeFrom(first, quantity);
        foreach (var r in reservations)
        {
            if (quantity == 0) break;
            quantity -= ConsumeFrom(r, quantity);
        }
    }
    private int ConsumeFrom(Reservation r, int quantity)
    {
        int moved = Mathf.Min(quantity, r.amount);
        r.amount -= moved;
        r.item.amount -= moved;
        amount -= moved;
        return moved;
    }
    private void ClearEmptySources()
    {
        foreach (var r in reservations)
        {
            if (r.item.amount > 0 || r.slot == null || r.slot.inventoryItem != r.item) continue;
            r.slot.inventoryItem = null;
            r.slot.currentItem = null;
            if (r.visual != null) { r.visual.SetActive(false); Destroy(r.visual); }
            r.slot.UpdateQuantityDisplay();
        }
    }

    private bool TryServe(Vector2 position)
    {
        if (Camera.main == null || SlotAt(position) != null) return false;
        var world = Camera.main.ScreenToWorldPoint(new Vector3(position.x, position.y, -Camera.main.transform.position.z));
        foreach (var hit in Physics2D.OverlapPointAll(world))
        {
            var customer = hit.GetComponentInParent<CustomerOrderController>();
            if (customer == null) continue;
            if (!customer.TryServe(new InventoryItem { itemPrefab = prefab, amount = 1 })) return false;
            Consume(1);
            ClearEmptySources();
            if (amount == 0) Cancel(); else Refresh();
            return true;
        }
        return false;
    }

    public static void CancelIfSource(Slot slot)
    {
        if (IsHolding && ReservedFrom(slot) > 0) Cancel();
    }
    public static void Cancel()
    {
        if (instance == null) return;
        var held = instance;
        held.ClearPreviews();
        held.amount = 0;
        held.repeatAction = RightAction.None;
        held.waitingForRelease = false;
        held.distributing = false;
        held.distributionSlots.Clear();
        held.cursor.gameObject.SetActive(false);
        foreach (var r in held.reservations)
        {
            if (r.group != null) r.group.alpha = r.alpha;
            if (r.slot != null) r.slot.UpdateQuantityDisplay();
        }
        held.reservations.Clear();
        ItemTooltipView.EndDrag();
    }
    private void OnDisable() { if (instance == this) Cancel(); }
}

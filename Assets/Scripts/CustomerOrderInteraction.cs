using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomerOrderInteraction : MonoBehaviour
{
    private static readonly List<CustomerOrderInteraction> seated = new();
    private static int handledFrame = -1;
    private CustomerOrderController order;
    private CustomerNPC guest;
    private HotbarController hotbar;
    private Collider2D interactionArea;
    private SpriteRenderer guestArt;
    private Collider2D player;

    private void Awake()
    {
        order = GetComponent<CustomerOrderController>();
        guest = GetComponent<CustomerNPC>();
        guestArt = GetComponent<SpriteRenderer>();
        interactionArea = GetComponent<Collider2D>();
    }

    private void OnEnable() => seated.Add(this);
    private void OnDisable()
    {
        seated.Remove(this);
        player = null;
        InteractionPromptManager.Instance?.Hide(transform);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || other.isTrigger) return;
        if (player != other && !order.OrderCompleted && !order.OrderRevealed)
            InteractionPromptManager.Instance?.Show(transform);
        player = other;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != player) return;
        player = null;
        InteractionPromptManager.Instance?.Hide(transform);
    }

    private void Update()
    {
        if (handledFrame == Time.frameCount || Time.timeScale <= 0 || InventoryPanelSession.HasOpenPanel ||
            RestaurantFurnitureEditor.Editing || InventoryStackCursor.IsHolding || ItemTooltipView.IsDragging) return;
        bool interact = CookwareCounter.InteractPressed;
        bool click = !interact && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                     Camera.main && Stove.CanClickWorld();
        if (!interact && !click) return;
        if (!HotbarController.Resolve(ref hotbar)) return;
        var slot = hotbar.SelectedSlot;
        var held = slot?.inventoryItem;
        if (click)
        {
            Vector2 point = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (TryServeAt(point, held)) Stove.ConsumeOne(slot);
            return;
        }

        CustomerOrderInteraction target = null;
        float best = float.PositiveInfinity;
        int bestPriority = int.MaxValue;
        foreach (var candidate in seated)
        {
            if (!candidate.CanTakeOrder || !candidate.player || !candidate.interactionArea.IsTouching(candidate.player)) continue;
            bool matches = candidate.Wants(held);
            if (!matches && candidate.order.OrderRevealed) continue;
            int priority = matches ? 0 : 1;
            float distance = ((Vector2)(candidate.transform.position - candidate.player.bounds.center)).sqrMagnitude;
            if (priority > bestPriority || (priority == bestPriority && distance >= best)) continue;
            target = candidate;
            bestPriority = priority;
            best = distance;
        }
        if (!target) return;
        handledFrame = Time.frameCount;
        if (!target.Reveal()) return;
        if (target.Wants(held) && target.order.TryServe(held)) Stove.ConsumeOne(slot);
    }

    private bool CanTakeOrder => guest.IsSeated && !order.OrderCompleted && !order.OrderExpired && order.RequestedRecipe != null;
    private bool Wants(InventoryItem item) => item != null && item.amount > 0 && item.itemPrefab == order.RequestedRecipe.result.itemPrefab;

    private bool Reveal()
    {
        if (!order.OrderRevealed && !order.RequestOrder()) return false;
        InteractionPromptManager.Instance?.Hide(transform);
        return true;
    }

    internal static bool TryServeAt(Vector2 point, InventoryItem item)
    {
        if (item?.itemPrefab == null || item.amount <= 0 || handledFrame == Time.frameCount) return false;
        CustomerOrderInteraction target = null;
        int bestPriority = int.MaxValue;
        float best = float.PositiveInfinity;
        foreach (var candidate in seated)
        {
            if (!candidate.CanTakeOrder) continue;
            int priority;
            if (candidate.guestArt && candidate.guestArt.bounds.Contains(new Vector3(point.x, point.y, candidate.guestArt.bounds.center.z))) priority = 0;
            else if (!candidate.Wants(item)) continue;
            else if (candidate.guest.AtTable(point)) priority = 1;
            else if (candidate.interactionArea.OverlapPoint(point)) priority = 2;
            else continue;

            float distance = ((Vector2)candidate.transform.position - point).sqrMagnitude;
            if (priority > bestPriority || (priority == bestPriority && distance >= best)) continue;
            target = candidate;
            bestPriority = priority;
            best = distance;
        }
        if (!target || !target.Wants(item) || !target.Reveal() || !target.order.TryServe(item)) return false;
        handledFrame = Time.frameCount;
        return true;
    }
}

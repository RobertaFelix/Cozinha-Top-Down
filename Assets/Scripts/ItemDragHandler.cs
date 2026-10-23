
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private Transform originalParent;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        transform.SetParent(transform.root);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        Slot sourceSlot = originalParent != null
            ? originalParent.GetComponent<Slot>()
            : null;

        if (sourceSlot == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        if (TryDeliverToNPC(eventData, sourceSlot))
            return;

        Slot targetSlot = FindDropSlot(eventData);

        if (targetSlot == null || targetSlot == sourceSlot)
        {
            ReturnToOriginalPosition();
            return;
        }

        InventoryItem sourceItem = sourceSlot.inventoryItem;

        if (sourceItem == null || sourceItem.itemPrefab == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        Transform sourceContainer =
            FindInventoryContainer(sourceSlot.transform);

        Transform targetContainer =
            FindInventoryContainer(targetSlot.transform);

        if (sourceContainer == null || targetContainer == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        if (sourceContainer != targetContainer)
        {
            TransferBetweenContainers(
                sourceSlot,
                targetSlot,
                sourceItem
            );

            return;
        }

        HandleSameContainer(sourceSlot, targetSlot, sourceItem);
    }

    private void HandleSameContainer(
        Slot sourceSlot,
        Slot targetSlot,
        InventoryItem sourceItem)
    {
        InventoryItem targetItem = targetSlot.inventoryItem;

        // Agrupa itens iguais.
        if (targetItem != null &&
            targetItem.itemPrefab == sourceItem.itemPrefab)
        {
            int space =
                InventoryStacking.MaxStackSize - targetItem.amount;

            if (space <= 0)
            {
                ReturnToOriginalPosition();
                return;
            }

            int moved = Mathf.Min(space, sourceItem.amount);

            targetItem.amount += moved;
            sourceItem.amount -= moved;

            targetSlot.UpdateQuantityDisplay();

            if (sourceItem.amount <= 0)
            {
                sourceSlot.currentItem = null;
                sourceSlot.inventoryItem = null;

                Destroy(gameObject);
            }

            sourceSlot.UpdateQuantityDisplay();

            if (sourceItem.amount > 0)
                ReturnToOriginalPosition();

            return;
        }

        // Troca itens diferentes entre slots do mesmo contêiner.
        GameObject targetVisual = targetSlot.currentItem;

        if (targetItem != null && targetVisual == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        if (targetItem != null)
        {
            targetVisual.transform.SetParent(sourceSlot.transform, false);
            ResetVisualTransform(targetVisual);

            sourceSlot.currentItem = targetVisual;
            sourceSlot.inventoryItem = targetItem;
        }
        else
        {
            sourceSlot.currentItem = null;
            sourceSlot.inventoryItem = null;
        }

        transform.SetParent(targetSlot.transform, false);
        ResetVisualTransform(gameObject);

        targetSlot.currentItem = gameObject;
        targetSlot.inventoryItem = sourceItem;

        sourceSlot.UpdateQuantityDisplay();
        targetSlot.UpdateQuantityDisplay();
    }

    
    private void TransferBetweenContainers(
        Slot sourceSlot,
        Slot targetSlot,
        InventoryItem sourceItem)
    {
        Debug.Log("[TRANSFERÊNCIA] Método iniciado.");

        if (sourceSlot == null || targetSlot == null)
        {
            Debug.LogError("[TRANSFERÊNCIA] Slot nulo.");
            ReturnToOriginalPosition();
            return;
        }

        InventoryItem targetItem = targetSlot.inventoryItem;
        GameObject targetVisual = targetSlot.currentItem;

        // Verifica se o destino realmente possui um objeto visual válido.
        bool targetHasVisual =
            targetVisual != null &&
            targetVisual.transform.parent == targetSlot.transform;

        if (!targetHasVisual)
        {
            targetSlot.currentItem = null;
            targetSlot.inventoryItem = null;
            targetSlot.UpdateQuantityDisplay();

            targetItem = null;
        }

        // Transfere para um slot vazio.
        if (targetItem == null)
        {
            InventoryItem itemToMove = sourceSlot.inventoryItem;
            GameObject visualToMove = gameObject;

            if (itemToMove == null ||
                itemToMove.itemPrefab == null ||
                visualToMove == null)
            {
                Debug.LogError("[TRANSFERÊNCIA] Item ou visual inválido.");
                ReturnToOriginalPosition();
                return;
            }

            sourceSlot.currentItem = null;
            sourceSlot.inventoryItem = null;
            sourceSlot.UpdateQuantityDisplay();

            visualToMove.transform.SetParent(targetSlot.transform, false);

            RectTransform rect = visualToMove.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            targetSlot.currentItem = visualToMove;
            targetSlot.inventoryItem = itemToMove;
            targetSlot.UpdateQuantityDisplay();

            Debug.Log("[TRANSFERÊNCIA] Item movido para a hotbar: " +
                itemToMove.itemPrefab.name);

            return;
        }

        // Agrupa itens iguais.
        if (targetItem.itemPrefab == sourceItem.itemPrefab)
        {
            int space = InventoryStacking.MaxStackSize - targetItem.amount;

            if (space <= 0)
            {
                ReturnToOriginalPosition();
                return;
            }

            int moved = Mathf.Min(space, sourceItem.amount);

            targetItem.amount += moved;
            sourceItem.amount -= moved;

            targetSlot.UpdateQuantityDisplay();

            if (sourceItem.amount <= 0)
            {
                sourceSlot.currentItem = null;
                sourceSlot.inventoryItem = null;
                Destroy(gameObject);
            }
            else
            {
                ReturnToOriginalPosition();
            }

            sourceSlot.UpdateQuantityDisplay();
            return;
        }

        Debug.Log("[TRANSFERÊNCIA] Destino possui outro item válido.");
        ReturnToOriginalPosition();
    }



    private void ResetVisualTransform(GameObject visual)
    {
        RectTransform rect = visual.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }

    private Transform FindInventoryContainer(Transform slotTransform)
    {
        Transform current = slotTransform != null
            ? slotTransform.parent
            : null;

        while (current != null)
        {
            if (current.GetComponent<HotbarController>() != null ||
                current.GetComponent<InventoryController>() != null ||
                current.GetComponent<InventoryRefrigeratorController>() != null)
            {
                return current;
            }

            current = current.parent;
        }

        return slotTransform != null ? slotTransform.parent : null;
    }

    private bool TryDeliverToNPC(
        PointerEventData eventData,
        Slot sourceSlot)
    {
        if (sourceSlot == null ||
            sourceSlot.inventoryItem == null ||
            sourceSlot.inventoryItem.itemPrefab == null)
        {
            return false;
        }

        Camera cam = Camera.main;

        if (cam == null)
            return false;

        Vector3 screenPosition = new Vector3(
            eventData.position.x,
            eventData.position.y,
            -cam.transform.position.z
        );

        Vector3 worldPosition =
            cam.ScreenToWorldPoint(screenPosition);

        foreach (Collider2D hit in
                 Physics2D.OverlapPointAll(worldPosition))
        {
            if (hit == null)
                continue;

            CustomerOrderController customer =
                hit.GetComponentInParent<CustomerOrderController>();

            if (customer == null)
                continue;

            if (!customer.TryServe(sourceSlot.inventoryItem))
            {
                ReturnToOriginalPosition();
                return true;
            }

            sourceSlot.currentItem = null;
            sourceSlot.inventoryItem = null;
            sourceSlot.UpdateQuantityDisplay();

            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private Slot FindDropSlot(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null)
            return null;

        Slot slot = eventData.pointerEnter.GetComponent<Slot>();

        return slot != null
            ? slot
            : eventData.pointerEnter.GetComponentInParent<Slot>();
    }

    private void ReturnToOriginalPosition()
    {
        if (originalParent == null)
            return;

        transform.SetParent(originalParent, false);
        ResetVisualTransform(gameObject);
    }
}

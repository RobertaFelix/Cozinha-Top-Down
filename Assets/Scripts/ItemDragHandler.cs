
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private bool isDragging;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || InventoryStackCursor.IsHolding || Time.timeScale <= 0) return;
        originalParent = transform.parent;

        Canvas parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            Debug.LogError(
                "ItemDragHandler: Canvas não encontrado!"
            );
            return;
        }

        rootCanvas = InventoryStackCursor.DragCanvas;

        isDragging = true;
        ItemTooltipView.BeginDrag();

        // Shared overlay keeps the item above every inventory Canvas while dragging.
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || rootCanvas == null)
            return;

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;
        ItemTooltipView.EndDrag();

        canvasGroup.alpha = 1f;

        // Impede que o próprio item seja identificado como destino.
        canvasGroup.blocksRaycasts = false;

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

        // Procura o slot sob o cursor.
        Slot targetSlot = FindDropSlot(eventData);

        canvasGroup.blocksRaycasts = true;

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

        Debug.Log(
            "[DRAG] Origem: " + sourceContainer.name +
            " | Destino: " + targetContainer.name +
            " | Slot: " + targetSlot.name
        );

        // Transferência entre contêineres diferentes.
        if (sourceContainer != targetContainer)
        {
            TransferBetweenContainers(
                sourceSlot,
                targetSlot,
                sourceItem
            );

            return;
        }

        // Movimentação dentro do mesmo contêiner.
        HandleSameContainer(
            sourceSlot,
            targetSlot,
            sourceItem
        );
    }

    private void HandleSameContainer(
    Slot sourceSlot,
    Slot targetSlot,
    InventoryItem sourceItem)
{
    InventoryItem targetItem = targetSlot.inventoryItem;

    Debug.Log("[INVENTÁRIO] Iniciando movimentação.");

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
        else
        {
            ReturnToOriginalPosition();
        }

        sourceSlot.UpdateQuantityDisplay();

        Debug.Log("[INVENTÁRIO] Agrupamento concluído.");
        return;
    }

    GameObject targetVisual = targetSlot.currentItem;

    // Se o destino tem item, troca os objetos visuais.
    if (targetItem != null)
    {
        if (targetVisual == null)
        {
            Debug.LogError(
                "[INVENTÁRIO] Destino possui item, mas não possui visual."
            );

            ReturnToOriginalPosition();
            return;
        }

        targetVisual.transform.SetParent(
            sourceSlot.transform,
            false
        );

        ResetVisualTransform(targetVisual);

        sourceSlot.currentItem = targetVisual;
        sourceSlot.inventoryItem = targetItem;
    }
    else
    {
        sourceSlot.currentItem = null;
        sourceSlot.inventoryItem = null;
    }

    // Coloca o item arrastado no destino.
    transform.SetParent(targetSlot.transform, false);

    ResetVisualTransform(gameObject);

    targetSlot.currentItem = gameObject;
    targetSlot.inventoryItem = sourceItem;

    sourceSlot.UpdateQuantityDisplay();
    targetSlot.UpdateQuantityDisplay();

    Debug.Log(
        "[INVENTÁRIO] Movimentação concluída. " +
        "Novo pai: " + transform.parent.name
    );
}

    private void TransferBetweenContainers(
        Slot sourceSlot,
        Slot targetSlot,
        InventoryItem sourceItem)
    {
        if (sourceSlot == null || targetSlot == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        InventoryItem targetItem = targetSlot.inventoryItem;
        GameObject targetVisual = targetSlot.currentItem;

        // Confere se o visual do destino realmente pertence ao slot.
        bool targetHasVisual =
            targetVisual != null &&
            targetVisual.transform.parent == targetSlot.transform;

        if (!targetHasVisual)
        {
            targetSlot.currentItem = null;
            targetSlot.inventoryItem = null;

            targetItem = null;
        }

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
            else
            {
                ReturnToOriginalPosition();
            }

            sourceSlot.UpdateQuantityDisplay();
            return;
        }

        // Se o destino está vazio, apenas transfere.
        if (targetItem == null)
        {
            sourceSlot.currentItem = null;
            sourceSlot.inventoryItem = null;

            transform.SetParent(targetSlot.transform, false);
            ResetVisualTransform(gameObject);

            targetSlot.currentItem = gameObject;
            targetSlot.inventoryItem = sourceItem;

            sourceSlot.UpdateQuantityDisplay();
            targetSlot.UpdateQuantityDisplay();

            return;
        }

        // Se os itens são diferentes, realiza a troca.
        if (targetVisual == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        targetVisual.transform.SetParent(
            sourceSlot.transform,
            false
        );

        ResetVisualTransform(targetVisual);

        transform.SetParent(targetSlot.transform, false);
        ResetVisualTransform(gameObject);

        sourceSlot.currentItem = targetVisual;
        sourceSlot.inventoryItem = targetItem;

        targetSlot.currentItem = gameObject;
        targetSlot.inventoryItem = sourceItem;

        sourceSlot.UpdateQuantityDisplay();
        targetSlot.UpdateQuantityDisplay();

        Debug.Log(
            "[TRANSFERÊNCIA] Itens trocados entre contêineres."
        );
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
        if (slotTransform == null)
            return null;

        return slotTransform.parent;
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

    public static void CancelAllDrags()
    {
        foreach (var drag in FindObjectsByType<ItemDragHandler>(FindObjectsInactive.Include))
        {
            if (!drag.isDragging) continue;
            drag.isDragging = false;
            drag.ReturnToOriginalPosition();
            ItemTooltipView.EndDrag();
        }
    }

    private void OnDisable()
    {
        if (!isDragging) return;
        isDragging = false;
        ItemTooltipView.EndDrag();
    }

    private Slot FindDropSlot(PointerEventData eventData)
    {
        if (EventSystem.current == null)
        {
            Debug.LogError(
                "[DRAG] EventSystem não encontrado!"
            );

            return null;
        }

        List<RaycastResult> results =
            new List<RaycastResult>();

        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == null)
                continue;

            Slot slot =
                result.gameObject.GetComponentInParent<Slot>();

            if (slot != null)
            {
                Debug.Log(
                    "[DRAG] Slot encontrado: " + slot.name
                );

                return slot;
            }
        }

        Debug.LogWarning(
            "[DRAG] Nenhum slot encontrado sob o cursor."
        );

        return null;
    }

    private void ReturnToOriginalPosition()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (originalParent == null)
            return;

        transform.SetParent(originalParent, false);
        ResetVisualTransform(gameObject);
        originalParent.GetComponent<Slot>()?.UpdateQuantityDisplay();
    } 
}

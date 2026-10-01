using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private Transform originalParent;

    private CanvasGroup canvasGroup;

    private void Start()
    {
        canvasGroup =
            GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        originalParent =
            transform.parent;

        transform.SetParent(
            transform.root
        );

        canvasGroup.blocksRaycasts =
            false;

        canvasGroup.alpha =
            0.6f;
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        transform.position =
            eventData.position;
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts =
            true;

        canvasGroup.alpha =
            1f;

        Slot originalSlot =
            originalParent.GetComponent<Slot>();

        if (originalSlot == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        Slot dropSlot = FindDropSlot(
            eventData
        );

        if (dropSlot == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        if (dropSlot == originalSlot)
        {
            ReturnToOriginalPosition();
            return;
        }

        InventoryItem draggedInventoryItem =
            originalSlot.inventoryItem;

        // ==========================================
        // DESTINO POSSUI ITEM
        // ==========================================

        if (dropSlot.currentItem != null)
        {
            GameObject droppedItem =
                dropSlot.currentItem;

            InventoryItem droppedInventoryItem =
                dropSlot.inventoryItem;

            droppedItem.transform.SetParent(
                originalSlot.transform
            );

            RectTransform droppedRect =
                droppedItem.GetComponent<RectTransform>();

            if (droppedRect != null)
            {
                droppedRect.anchoredPosition =
                    Vector2.zero;
            }

            originalSlot.currentItem =
                droppedItem;

            originalSlot.inventoryItem =
                droppedInventoryItem;
        }
        else
        {
            // ==========================================
            // DESTINO VAZIO
            // ==========================================

            originalSlot.currentItem =
                null;

            originalSlot.inventoryItem =
                null;
        }

        // ==========================================
        // MOVER ITEM PARA O NOVO SLOT
        // ==========================================

        transform.SetParent(
            dropSlot.transform
        );

        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchoredPosition =
                Vector2.zero;
        }

        dropSlot.currentItem =
            gameObject;

        dropSlot.inventoryItem =
            draggedInventoryItem;
    }

private Slot FindDropSlot(
    PointerEventData eventData)
{
    if (eventData.pointerEnter == null)
    {
        Debug.Log("DROP: pointerEnter = NULL");
        return null;
    }

    Debug.Log(
        "DROP: pointerEnter = " +
        eventData.pointerEnter.name
    );

    Slot slot =
        eventData.pointerEnter
            .GetComponent<Slot>();

    if (slot != null)
    {
        Debug.Log(
            "DROP: SLOT ENCONTRADO = " +
            slot.name
        );

        return slot;
    }

    slot =
        eventData.pointerEnter
            .GetComponentInParent<Slot>();

    if (slot != null)
    {
        Debug.Log(
            "DROP: SLOT PAI ENCONTRADO = " +
            slot.name
        );
    }
    else
    {
        Debug.Log(
            "DROP: NENHUM SLOT ENCONTRADO"
        );
    }

    return slot;
}

    private void ReturnToOriginalPosition()
    {
        transform.SetParent(
            originalParent
        );

        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchoredPosition =
                Vector2.zero;
        }
    }
}
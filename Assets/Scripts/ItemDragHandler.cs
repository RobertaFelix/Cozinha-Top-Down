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
        canvasGroup = GetComponent<CanvasGroup>();
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

        Slot originalSlot =
            originalParent.GetComponent<Slot>();

        Slot dropSlot =
            eventData.pointerEnter?.GetComponent<Slot>();

        if (dropSlot == null)
        {
            GameObject item =
                eventData.pointerEnter;

            if (item != null)
            {
                dropSlot =
                    item.GetComponentInParent<Slot>();
            }
        }


        // ==========================================
        // SOLTOU FORA DE UM SLOT
        // ==========================================

        if (dropSlot == null)
        {
            transform.SetParent(originalParent);

            GetComponent<RectTransform>()
                .anchoredPosition = Vector2.zero;

            return;
        }


        // ==========================================
        // MESMO SLOT
        // ==========================================

        if (dropSlot == originalSlot)
        {
            transform.SetParent(originalSlot.transform);

            GetComponent<RectTransform>()
                .anchoredPosition = Vector2.zero;

            return;
        }


        // Guarda o InventoryItem do item arrastado
        InventoryItem draggedInventoryItem =
            originalSlot.inventoryItem;


        // ==========================================
        // SLOT DE DESTINO JÁ TEM ITEM
        // ==========================================

        if (dropSlot.currentItem != null)
        {
            GameObject droppedItem =
                dropSlot.currentItem;

            InventoryItem droppedInventoryItem =
                dropSlot.inventoryItem;


            // Move o item antigo para o slot original
            droppedItem.transform.SetParent(
                originalSlot.transform
            );

            droppedItem.GetComponent<RectTransform>()
                .anchoredPosition = Vector2.zero;


            originalSlot.currentItem =
                droppedItem;

            originalSlot.inventoryItem =
                droppedInventoryItem;
        }
        else
        {
            // Slot original fica vazio
            originalSlot.currentItem = null;
            originalSlot.inventoryItem = null;
        }


        // ==========================================
        // MOVE ITEM ARRASTADO
        // ==========================================

        transform.SetParent(
            dropSlot.transform
        );

        GetComponent<RectTransform>()
            .anchoredPosition = Vector2.zero;


        dropSlot.currentItem =
            gameObject;

        dropSlot.inventoryItem =
            draggedInventoryItem;
    }
}
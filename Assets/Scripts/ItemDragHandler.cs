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

        Slot originalSlot = originalParent.GetComponent<Slot>();

        if (originalSlot == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        // Primeiro, tenta entregar o prato a um NPC.
        if (TryDeliverToNPC(eventData, originalSlot))
            return;

        // Se não foi uma entrega, mantém o comportamento normal
        // de arrastar e soltar entre slots.
        Slot dropSlot = FindDropSlot(eventData);

        if (dropSlot == null || dropSlot == originalSlot)
        {
            ReturnToOriginalPosition();
            return;
        }

        InventoryItem draggedInventoryItem =
            originalSlot.inventoryItem;

        if (draggedInventoryItem == null)
        {
            ReturnToOriginalPosition();
            return;
        }

        if (dropSlot.currentItem != null)
        {
            GameObject droppedItem = dropSlot.currentItem;

            InventoryItem droppedInventoryItem =
                dropSlot.inventoryItem;

            droppedItem.transform.SetParent(originalSlot.transform);

            RectTransform droppedRect =
                droppedItem.GetComponent<RectTransform>();

            if (droppedRect != null)
                droppedRect.anchoredPosition = Vector2.zero;

            originalSlot.currentItem = droppedItem;
            originalSlot.inventoryItem = droppedInventoryItem;
        }
        else
        {
            originalSlot.currentItem = null;
            originalSlot.inventoryItem = null;
        }

        transform.SetParent(dropSlot.transform);

        RectTransform rect = GetComponent<RectTransform>();

        if (rect != null)
            rect.anchoredPosition = Vector2.zero;

        dropSlot.currentItem = gameObject;
        dropSlot.inventoryItem = draggedInventoryItem;
    }

    private bool TryDeliverToNPC(
        PointerEventData eventData,
        Slot originalSlot)
    {
        if (originalSlot == null ||
            originalSlot.inventoryItem == null ||
            originalSlot.inventoryItem.itemPrefab == null)
        {
            return false;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError(
                "ItemDragHandler: nenhuma câmera com a tag MainCamera foi encontrada."
            );

            return false;
        }

        // Converte a posição do cursor na tela para o mundo 2D.
        float distance = -cam.transform.position.z;

        Vector3 screenPosition = new Vector3(
            eventData.position.x,
            eventData.position.y,
            distance
        );

        Vector3 worldPosition =
            cam.ScreenToWorldPoint(screenPosition);

        // Detecta colliders na posição em que o item foi solto.
        Collider2D[] hits =
            Physics2D.OverlapPointAll(worldPosition);

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            CustomerOrderController customerOrder =
                hit.GetComponentInParent<CustomerOrderController>();

            if (customerOrder == null)
                continue;

            InventoryItem offeredItem =
                originalSlot.inventoryItem;

            // TryServe retorna true somente se o prato for correto.
            if (!customerOrder.TryServe(offeredItem))
            {
                Debug.Log(
                    "Prato incorreto ou pedido já concluído."
                );

                ReturnToOriginalPosition();
                return true;
            }

            // Pedido aceito: remove o item da hotbar.
            originalSlot.currentItem = null;
            originalSlot.inventoryItem = null;

            Destroy(gameObject);

            Debug.Log(
                "Prato entregue ao NPC com sucesso!"
            );

            return true;
        }

        // Nenhum NPC foi encontrado: continua o fluxo normal.
        return false;
    }

    private Slot FindDropSlot(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null)
        {
            Debug.Log("DROP: pointerEnter = NULL");
            return null;
        }

        Slot slot =
            eventData.pointerEnter.GetComponent<Slot>();

        if (slot != null)
            return slot;

        slot =
            eventData.pointerEnter.GetComponentInParent<Slot>();

        if (slot == null)
        {
            Debug.Log(
                "DROP: nenhum Slot encontrado no destino."
            );
        }

        return slot;
    }

    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);

        RectTransform rect = GetComponent<RectTransform>();

        if (rect != null)
            rect.anchoredPosition = Vector2.zero;
    }
}
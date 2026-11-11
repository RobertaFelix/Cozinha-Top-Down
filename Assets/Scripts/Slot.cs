
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour, IPointerDownHandler
{
    public GameObject currentItem;
    public InventoryItem inventoryItem;

    [Header("Quantidade do item")]
    [SerializeField] private TextMeshProUGUI amountText;

    public TMP_Text QuantityLabel => amountText;

    private void Awake()
    {
        ItemTooltipTarget.Bind(gameObject, GetTooltipContent);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        InventoryStackCursor.PointerDown(this, eventData);
    }

    private void OnDisable()
    {
        InventoryStackCursor.CancelIfSource(this);
    }

    private ItemTooltipContent GetTooltipContent()
    {
        // Dados residuais ou o visual de outro slot não representam um item visível aqui.
        if (inventoryItem?.itemPrefab == null || inventoryItem.amount <= 0 ||
            currentItem == null || currentItem.transform.parent != transform ||
            !currentItem.activeInHierarchy)
            return default;

        var image = currentItem.GetComponentInChildren<Image>();
        bool hasImage = image != null && image.isActiveAndEnabled && image.sprite != null && image.color.a > 0;
        var rawImage = currentItem.GetComponentInChildren<RawImage>();
        bool hasRawImage = rawImage != null && rawImage.isActiveAndEnabled && rawImage.texture != null && rawImage.color.a > 0;
        var renderer = currentItem.GetComponentInChildren<SpriteRenderer>();
        bool hasSprite = renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sprite != null && renderer.color.a > 0;

        return hasImage || hasRawImage || hasSprite
            ? ItemTooltipCatalog.ForItem(inventoryItem.itemPrefab, inventoryItem.amount)
            : default;
    }

    public void UpdateQuantityDisplay()
    {
        if (amountText == null)
            return;

        var preview = InventoryStackCursor.PreviewAmount(this);
        if (preview.HasValue)
        {
            amountText.text = preview.Value.ToString();
            amountText.transform.SetAsLastSibling();
            return;
        }

        if (inventoryItem == null ||
            inventoryItem.itemPrefab == null)
        {
            amountText.text = "";
            return;
        }

        int visibleAmount = inventoryItem.amount - InventoryStackCursor.ReservedFrom(this);
        amountText.text = visibleAmount > 0 ? visibleAmount.ToString() : "";
        amountText.transform.SetAsLastSibling();
    }
}

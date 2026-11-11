
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public GameObject currentItem;
    public InventoryItem inventoryItem;

    [Header("Quantidade do item")]
    [SerializeField] private TextMeshProUGUI amountText;

    private void Awake()
    {
        ItemTooltipTarget.Bind(gameObject, GetTooltipContent);
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

        if (inventoryItem == null ||
            inventoryItem.itemPrefab == null)
        {
            amountText.text = "";
            return;
        }

        amountText.text = "" + Mathf.Max(1, inventoryItem.amount);
        amountText.transform.SetAsLastSibling();
    }
}

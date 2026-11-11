
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketCartSlot : MonoBehaviour
{
    [Header("Ingrediente")]
    public GameObject ingredientPrefab;

    [Header("Visual")]
    public GameObject visualItem;
    public TMP_Text amountText;

    [Header("Dados")]
    public int price;
    public int amount;

    private MarketUIController market;
    private UnityEngine.UI.Button slotButton;

    public bool IsEmpty => ingredientPrefab == null;

    private void Awake()
    {
        ItemTooltipTarget.Bind(gameObject, () => ItemTooltipCatalog.ForItem(ingredientPrefab, amount,
            market != null ? market.GetPurchasePrice(price) : Mathf.Max(0, price)));

        slotButton = GetComponent<UnityEngine.UI.Button>();

        if (slotButton != null)
            slotButton.onClick.AddListener(RemoveOne);
    }

    public void SetMarket(MarketUIController controller)
    {
        market = controller;
    }

    public void SetItem(GameObject prefab, int itemPrice)
    {
        Clear();

        ingredientPrefab = prefab;
        price = itemPrice;
        amount = 1;

        CreateVisual();
        UpdateQuantityDisplay();
    }

    public void AddAmount(int quantity = 1)
    {
        amount += quantity;
        UpdateQuantityDisplay();
    }

    private void CreateVisual()
    {
        if (ingredientPrefab == null)
            return;

        visualItem = Instantiate(ingredientPrefab, transform);
        visualItem.name = ingredientPrefab.name + "_CartVisual";

        RectTransform rect = visualItem.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
        else
        {
            visualItem.transform.localPosition = Vector3.zero;
            visualItem.transform.localScale = Vector3.one;
        }

        foreach (Graphic graphic in
                 visualItem.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        foreach (Collider2D col in
                 visualItem.GetComponentsInChildren<Collider2D>(true))
        {
            col.enabled = false;
        }

        // Mantém a quantidade à frente da imagem.
        if (amountText != null)
            amountText.transform.SetAsLastSibling();
    }

    private void RemoveOne()
    {
        if (IsEmpty || amount <= 0)
            return;

        amount--;

        if (amount <= 0)
        {
            Clear();
        }
        else
        {
            UpdateQuantityDisplay();
        }

        if (market != null)
            market.RefreshTotal();
    }

    private void UpdateQuantityDisplay()
    {
        if (amountText != null)
        {
            amountText.text =
                IsEmpty || amount < 1 ? "" : amount.ToString();

            amountText.transform.SetAsLastSibling();
        }
    }

    public void Clear()
    {
        if (visualItem != null)
            Destroy(visualItem);

        visualItem = null;
        ingredientPrefab = null;
        price = 0;
        amount = 0;

        UpdateQuantityDisplay();
    }
}

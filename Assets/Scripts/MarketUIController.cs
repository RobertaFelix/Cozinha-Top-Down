
using TMPro;
using UnityEngine;

public class MarketUIController : MonoBehaviour
{
    [Header("Painel")]
    [SerializeField] private GameObject marketPanel;

    [Header("Slots da loja")]
    [SerializeField] private MarketIngredientSlot[] ingredientSlots;

    [Header("Carrinho")]
    [SerializeField] private MarketCartSlot[] cartSlots;
    [SerializeField] private TMP_Text totalText;

    [Header("Sistemas existentes")]
    [SerializeField] private HotbarController hotbar;
    [SerializeField] private InventoryController inventory;

    public bool IsOpen =>
        marketPanel != null && marketPanel.activeSelf;

    [Header("Upgrade de desconto")]
    [SerializeField, Range(0f, 1f)] private float upgradedDiscount = 0.2f;

    public int GetPurchasePrice(int basePrice)
    {
        if (basePrice <= 0) return 0;
        return UpgradePurchases.Has(UpgradePurchases.Wallet)
            ? Mathf.Max(1, Mathf.CeilToInt(basePrice * (1f - Mathf.Clamp01(upgradedDiscount))))
            : basePrice;
    }

    private void OnEnable()
    {
        UpgradePurchases.Changed += RefreshPrices;
        RefreshPrices();
    }

    private void OnDisable()
    {
        UpgradePurchases.Changed -= RefreshPrices;
    }

    private void RefreshPrices()
    {
        if (ingredientSlots != null)
            foreach (MarketIngredientSlot slot in ingredientSlots)
                if (slot != null) slot.RefreshPrice();
        UpdateTotal();
    }

    private void Awake()
    {
        if (marketPanel != null)
            marketPanel.SetActive(false);

        foreach (MarketIngredientSlot slot in ingredientSlots)
        {
            if (slot != null)
                slot.Initialize(this);
        }

        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot != null)
                slot.SetMarket(this);
        }

        UpdateTotal();
    }

    private void Start()
    {
        FindInventoryControllers();
    }

    private void FindInventoryControllers()
    {
        GameObject gameControllerObject =
            GameObject.Find("GameController");

        if (gameControllerObject == null)
        {
            Debug.LogError(
                "MarketUIController: GameObject GameController não encontrado!"
            );

            hotbar = null;
            inventory = null;
            return;
        }

        hotbar =
            gameControllerObject.GetComponent<HotbarController>();

        inventory =
            gameControllerObject.GetComponent<InventoryController>();

        if (hotbar != null)
        {
            Debug.Log(
                "MarketUIController: HotbarController recuperado com sucesso!"
            );
        }
        else
        {
            Debug.LogError(
                "MarketUIController: HotbarController não encontrado no GameController!"
            );
        }

        if (inventory != null)
        {
            Debug.Log(
                "MarketUIController: InventoryController recuperado com sucesso!"
            );
        }
        else
        {
            Debug.LogError(
                "MarketUIController: InventoryController não encontrado no GameController!"
            );
        }
    }

    public void OpenMarket()
    {
        if (marketPanel == null)
            return;

        FindInventoryControllers();

        marketPanel.SetActive(true);
        UpdateTotal();
    }

    public void CloseMarket()
    {
        ClearCart();

        if (marketPanel != null)
            marketPanel.SetActive(false);
    }

    public void AddToCart(GameObject prefab, int price)
    {
        if (!IsOpen || prefab == null || price < 0)
            return;

        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot == null || slot.IsEmpty)
                continue;

            if (slot.ingredientPrefab == prefab)
            {
                if (slot.price != price)
                {
                    Debug.LogWarning(
                        "O mesmo ingrediente foi configurado com preços diferentes."
                    );
                    return;
                }

                slot.AddAmount();
                UpdateTotal();
                return;
            }
        }

        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot == null || !slot.IsEmpty)
                continue;

            slot.SetItem(prefab, price);
            UpdateTotal();
            return;
        }

        Debug.Log("O carrinho está cheio!");
    }

    public void BuyItems()
    {
        if (!IsOpen)
            return;

        // Recupera as referências novamente antes da compra.
        FindInventoryControllers();

        if (CoinManager.Instance == null)
        {
            Debug.LogError(
                "CoinManager não encontrado na cena!"
            );
            return;
        }

        if (hotbar == null || hotbar.hotbarPanel == null)
        {
            Debug.LogError(
                "Hotbar ou hotbarPanel não configurados no MarketUIController!"
            );
            return;
        }

        if (inventory == null)
        {
            Debug.LogError(
                "InventoryController não encontrado no GameController!"
            );
            return;
        }

        int availableCoins = CoinManager.Instance.TotalCoins;
        int purchasedCost = 0;

        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot == null || slot.IsEmpty || slot.amount <= 0)
                continue;

            int unitPrice = GetPurchasePrice(slot.price);
            int requestedQuantity = slot.amount;
            int purchasedQuantity = 0;

            for (int i = 0; i < requestedQuantity; i++)
            {
                if (unitPrice > 0 &&
                    purchasedCost + unitPrice > availableCoins)
                {
                    break;
                }

                InventoryItem item = new InventoryItem
                {
                    itemPrefab = slot.ingredientPrefab,
                    amount = 1
                };

                bool added = InventoryStacking.AddItem(
                    hotbar.hotbarPanel.transform,
                    item
                );

                if (!added)
                {
                    added = inventory.AddItem(item);
                }

                if (!added)
                    break;

                purchasedQuantity++;
                purchasedCost += unitPrice;
            }

            slot.amount -= purchasedQuantity;

            if (slot.amount <= 0)
            {
                slot.Clear();
            }
            else if (purchasedQuantity > 0)
            {
                slot.AddAmount(0);
            }
        }

        if (purchasedCost > 0)
        {
            bool paymentSuccessful =
                CoinManager.Instance.SpendCoins(purchasedCost);

            if (!paymentSuccessful)
            {
                Debug.LogError(
                    "Não foi possível descontar o valor da compra."
                );
            }
        }

        UpdateTotal();
    }

    public void ClearCart()
    {
        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot != null)
                slot.Clear();
        }

        UpdateTotal();
    }

    public void RefreshTotal()
    {
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        if (totalText == null || cartSlots == null)
            return;

        int total = 0;

        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot == null || slot.IsEmpty)
                continue;

            total += GetPurchasePrice(slot.price) * slot.amount;
        }

        totalText.text = total.ToString();
    }
}

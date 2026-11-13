
using TMPro;
using UnityEngine;

public class MarketUIController : MonoBehaviour
{
    [Header("Painel")]
    [SerializeField] private GameObject marketPanel;
    [SerializeField] private MarketOpeningHours openingHours;
    [SerializeField] private GameObject vendor;
    [SerializeField] private GameObject vendorInteractionArea;

    private bool tradingSessionActive;
    private float sessionMarkup;
    private bool sessionAfterHours;
    public bool IsTradingAllowed => tradingSessionActive && IsOpen;

    [System.Serializable] private struct Product
    {
        public GameObject prefab;
        [Min(0)] public int price;
    }
    [Header("Slots da loja")]
    [SerializeField] private MarketIngredientSlot ingredientTemplate;
    [SerializeField] private Transform ingredientGrid;
    [SerializeField] private Product[] products;
    private MarketIngredientSlot[] ingredientSlots;

    [Header("Carrinho")]
    [SerializeField] private MarketCartSlot cartTemplate;
    [SerializeField] private Transform cartGrid;
    [SerializeField, Min(1)] private int cartCapacity = 12;
    private MarketCartSlot[] cartSlots;
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
        float discount = UpgradePurchases.Has(UpgradePurchases.Wallet) ? Mathf.Clamp01(upgradedDiscount) : 0f;
        float markup = IsTradingAllowed ? sessionMarkup
            : openingHours != null && !openingHours.IsOpen && openingHours.CanAccess ? openingHours.AfterHoursMarkup : 0f;
        // Apply both modifiers before rounding once to whole coins.
        return Mathf.Max(1, (int)System.Math.Ceiling(
            basePrice * (1m - (decimal)discount) * (1m + (decimal)markup)));
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

        ingredientSlots = new MarketIngredientSlot[products.Length];
        for (int i = 0; i < products.Length; i++)
        {
            var slot = Instantiate(ingredientTemplate, ingredientGrid);
            slot.ingredientPrefab = products[i].prefab;
            slot.price = products[i].price;
            slot.Initialize(this);
            ingredientSlots[i] = slot;
        }
        cartSlots = new MarketCartSlot[cartCapacity];
        for (int i = 0; i < cartSlots.Length; i++)
        {
            cartSlots[i] = Instantiate(cartTemplate, cartGrid);
            cartSlots[i].SetMarket(this);
        }

        UpdateTotal();
    }

    private void Update()
    {
        RefreshVendorPresence();
    }

    private void RefreshVendorPresence()
    {
        // Finish serving an existing customer before leaving for the day.
        bool present = openingHours != null && (openingHours.IsOpen || (IsTradingAllowed && !sessionAfterHours));
        bool canInteract = openingHours != null && (openingHours.CanAccess || IsTradingAllowed);
        if (vendor != null && vendor.activeSelf != present) vendor.SetActive(present);
        if (vendorInteractionArea != null && vendorInteractionArea.activeSelf != canInteract)
            vendorInteractionArea.SetActive(canInteract);
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
        // An already-open shopping session may finish after closing time.
        if (IsTradingAllowed) return;
        if (openingHours == null || !openingHours.CanAccess)
        {
            if (openingHours != null) openingHours.ShowClosedNotice();
            return;
        }
        if (marketPanel == null)
            return;

        InventoryPanelSession.Open(marketPanel, CloseMarket);
        FindInventoryControllers();

        sessionAfterHours = !openingHours.IsOpen;
        sessionMarkup = sessionAfterHours ? openingHours.AfterHoursMarkup : 0f;
        tradingSessionActive = true;
        marketPanel.SetActive(true);
        InventoryWorkspace.SetShopHotbarVisible(false);
        RefreshVendorPresence();
        RefreshPrices();
    }

    public void CloseMarket()
    {
        InventoryPanelSession.Closed(marketPanel);
        InventoryWorkspace.SetShopHotbarVisible(true);
        tradingSessionActive = false;
        RefreshVendorPresence();
        ClearCart();

        if (marketPanel != null)
            marketPanel.SetActive(false);
    }

    public void AddToCart(GameObject prefab, int price)
    {
        if (!IsTradingAllowed) return;
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
        if (!IsTradingAllowed) return;
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


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

    private void Awake()
    {
        if (marketPanel != null)
            marketPanel.SetActive(false);

        foreach (MarketIngredientSlot slot in ingredientSlots)
        {
            if (slot != null)
                slot.Initialize(this);
        }

        // Associa cada slot do carrinho a este controlador.
        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot != null)
                slot.SetMarket(this);
        }

        UpdateTotal();
    }

    public void OpenMarket()
    {
        if (marketPanel == null)
            return;

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

        // Primeiro, procura o mesmo ingrediente no carrinho.
        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot == null || slot.IsEmpty)
                continue;

            if (slot.ingredientPrefab == prefab)
            {
                // Evita misturar o mesmo ingrediente com preços diferentes.
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

        // Se o ingrediente ainda não existe, usa um slot vazio.
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

        if (CoinManager.Instance == null)
        {
            Debug.LogError("CoinManager não encontrado na cena!");
            return;
        }

        if (hotbar == null || hotbar.hotbarPanel == null)
        {
            Debug.LogError("Hotbar não configurada no MarketUIController!");
            return;
        }

        if (inventory == null)
        {
            Debug.LogError(
                "InventoryController não configurado no MarketUIController!"
            );
            return;
        }

        int availableCoins = CoinManager.Instance.TotalCoins;
        int purchasedCost = 0;

        // Percorre todos os ingredientes presentes no carrinho.
        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot == null || slot.IsEmpty || slot.amount <= 0)
                continue;

            int unitPrice = slot.price;
            int requestedQuantity = slot.amount;
            int purchasedQuantity = 0;

            // Tenta transferir cada unidade individualmente.
            for (int i = 0; i < requestedQuantity; i++)
            {
                // Verifica se há saldo para a próxima unidade.
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

                // Primeiro, tenta adicionar à hotbar.
                bool added = InventoryStacking.AddItem(
                    hotbar.hotbarPanel.transform,
                    item
                );

                // Se a hotbar estiver cheia, tenta o inventário pessoal.
                if (!added)
                {
                    added = inventory.AddItem(item);
                }

                // Se nenhum inventário aceitar o item,
                // mantém as unidades restantes no carrinho.
                if (!added)
                    break;

                purchasedQuantity++;
                purchasedCost += unitPrice;
            }

            // Remove somente a quantidade transferida.
            slot.amount -= purchasedQuantity;

            if (slot.amount <= 0)
            {
                slot.Clear();
            }
            else if (purchasedQuantity > 0)
            {
                // Atualiza o número exibido no carrinho.
                slot.AddAmount(0);
            }
        }

        // Cobra somente o valor das unidades transferidas.
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

    // Público para permitir a atualização após remover uma unidade.
    public void RefreshTotal()
    {
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        if (totalText == null)
            return;

        int total = 0;

        foreach (MarketCartSlot slot in cartSlots)
        {
            if (slot == null || slot.IsEmpty)
                continue;

            total += slot.price * slot.amount;
        }

        totalText.text = total.ToString();
    }
}

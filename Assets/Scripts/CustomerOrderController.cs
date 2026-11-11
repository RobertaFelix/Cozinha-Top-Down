
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomerOrderController : MonoBehaviour
{
    [Header("Interface do pedido")]
    [SerializeField] private NPCOrderUI orderUI;

    [Header("Paciência do NPC")]
    [SerializeField] private float patienceDuration = 30f;

    private StoveController stoveController;
    private CookingRecipe requestedRecipe;

    private bool orderCompleted;
    private bool orderExpired;
    private bool patienceRunning;
    private bool orderRevealed;

    private float patienceRemaining;

    public CookingRecipe RequestedRecipe => requestedRecipe;
    public bool OrderCompleted => orderCompleted;
    public bool OrderExpired => orderExpired;
    public bool OrderRevealed => orderRevealed;

    private void Awake()
    {
        if (orderUI != null)
            orderUI.Hide();
    }

    private void Update()
    {
        if (!patienceRunning || orderCompleted || orderExpired)
            return;

        patienceRemaining -= Time.deltaTime;

        float normalizedPatience =
            1f - Mathf.Clamp01(
                patienceRemaining /
                Mathf.Max(0.1f, patienceDuration)
            );

        if (orderUI != null)
            orderUI.SetPatience(normalizedPatience);

        if (patienceRemaining <= 0f)
            ExpireOrder();
    }

    public void Initialize(StoveController controller)
    {
        stoveController = controller;

        requestedRecipe = null;
        orderCompleted = false;
        orderExpired = false;
        patienceRunning = false;
        orderRevealed = false;
        patienceRemaining = 0f;

        if (orderUI != null)
            orderUI.Hide();

        if (stoveController == null)
        {
            Debug.LogError(
                "CustomerOrderController: StoveController não configurado.",
                this
            );
            return;
        }

        ChooseRandomRecipe();
    }

    private void ChooseRandomRecipe()
    {
        // O NPC consulta somente o cardápio do restaurante.
        RestaurantMenuController menuController =
            RestaurantMenuController.Instance;

        if (menuController == null)
        {
            Debug.LogWarning(
                "Não foi encontrado um RestaurantMenuController na cena. " +
                "O NPC não poderá escolher um prato.",
                this
            );
            return;
        }

        IReadOnlyList<CookingRecipe> menuRecipes =
            menuController.GetSelectedRecipes();

        if (menuRecipes == null || menuRecipes.Count == 0)
        {
            Debug.Log(
                "O cardápio está vazio. Este NPC não tem um prato para pedir.",
                this
            );
            return;
        }

        List<CookingRecipe> validRecipes =
            new List<CookingRecipe>();

        foreach (CookingRecipe recipe in menuRecipes)
        {
            if (recipe == null ||
                recipe.result == null ||
                recipe.result.itemPrefab == null)
            {
                continue;
            }

            // Evita escolher pratos cuja imagem não pode ser exibida.
            Sprite foodSprite =
                GetFoodSprite(recipe.result.itemPrefab);

            if (foodSprite == null)
            {
                Debug.LogWarning(
                    "O prato não possui um Sprite válido: " +
                    recipe.result.itemPrefab.name,
                    this
                );
                continue;
            }

            validRecipes.Add(recipe);
        }

        if (validRecipes.Count == 0)
        {
            Debug.LogWarning(
                "Nenhum prato válido foi encontrado no cardápio. " +
                "O NPC não poderá fazer um pedido.",
                this
            );
            return;
        }

        requestedRecipe =
            validRecipes[Random.Range(0, validRecipes.Count)];

        Debug.Log(
            "Prato escolhido do cardápio para o NPC: " +
            requestedRecipe.result.itemPrefab.name
        );
    }

    // Chamado pela interação quando o jogador aperta E perto do NPC.
    public bool RequestOrder()
    {
        if (orderRevealed ||
            orderCompleted ||
            orderExpired ||
            requestedRecipe == null)
        {
            return false;
        }

        if (orderUI == null)
        {
            Debug.LogError(
                "CustomerOrderController: NPCOrderUI não configurado.",
                this
            );
            return false;
        }

        Sprite foodSprite =
            GetFoodSprite(requestedRecipe.result.itemPrefab);

        if (foodSprite == null)
            return false;

        orderRevealed = true;

        CustomerNPC customerNPC = GetComponent<CustomerNPC>();

        if (customerNPC != null)
            customerNPC.HideOrderAvailableIcon();

        orderUI.Show(foodSprite);

        patienceRemaining = Mathf.Max(0.1f, patienceDuration);
        patienceRunning = true;

        Debug.Log(
            "Pedido revelado: " +
            requestedRecipe.result.itemPrefab.name +
            ". A paciência começou a contar."
        );

        return true;
    }

    public bool TryServe(InventoryItem offeredItem)
    {
        if (!orderRevealed ||
            orderCompleted ||
            orderExpired ||
            requestedRecipe == null ||
            offeredItem == null ||
            offeredItem.itemPrefab == null)
        {
            return false;
        }

        if (patienceRunning && patienceRemaining <= 0f)
        {
            ExpireOrder();
            return false;
        }

        GameObject requestedPrefab =
            requestedRecipe.result.itemPrefab;

        if (offeredItem.itemPrefab != requestedPrefab)
        {
            Debug.Log(
                "Pedido incorreto! O NPC quer: " +
                requestedPrefab.name
            );
            return false;
        }

        CustomerNPC customerNPC = GetComponent<CustomerNPC>();

        if (customerNPC == null)
        {
            Debug.LogError(
                "O NPC não possui o componente CustomerNPC.",
                this
            );
            return false;
        }

        Sprite foodSprite = GetFoodSprite(requestedPrefab);

        if (foodSprite == null)
            return false;

        patienceRunning = false;
        orderCompleted = true;

        if (orderUI != null)
            orderUI.Hide();

        // Calcula o valor na entrega, inclusive para pedidos feitos antes da compra.
        int salePrice = CoinManager.Instance != null
            ? CoinManager.Instance.GetDishSalePrice(requestedRecipe.basePrice)
            : Mathf.Max(0, requestedRecipe.basePrice);

        customerNPC.ServeFood(foodSprite, salePrice);

        Debug.Log(
            "Pedido correto entregue: " + requestedPrefab.name
        );

        return true;
    }

    private void ExpireOrder()
    {
        if (orderExpired || orderCompleted)
            return;

        orderExpired = true;
        patienceRunning = false;
        patienceRemaining = 0f;

        if (orderUI != null)
            orderUI.Hide();

        Debug.LogWarning(
            "O NPC perdeu a paciência e foi embora sem comer."
        );

        CustomerNPC customerNPC = GetComponent<CustomerNPC>();

        if (customerNPC != null)
            customerNPC.Leave();
        else
            Destroy(gameObject);
    }

    private Sprite GetFoodSprite(GameObject foodPrefab)
    {
        if (foodPrefab == null)
            return null;

        SpriteRenderer spriteRenderer =
            foodPrefab.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                foodPrefab.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderer != null &&
            spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite;
        }

        Image image = foodPrefab.GetComponent<Image>();

        if (image == null)
            image = foodPrefab.GetComponentInChildren<Image>(true);

        return image != null ? image.sprite : null;
    }
}

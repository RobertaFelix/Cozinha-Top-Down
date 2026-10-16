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

    private float patienceRemaining;

    public CookingRecipe RequestedRecipe => requestedRecipe;
    public bool OrderCompleted => orderCompleted;

    private void Update()
    {
        if (!patienceRunning || orderCompleted || orderExpired)
            return;

        patienceRemaining -= Time.deltaTime;

        // Começa em 0 e aumenta até 1.
        float normalizedPatience =
            1f - Mathf.Clamp01(
                patienceRemaining / Mathf.Max(0.1f, patienceDuration)
            );

        if (orderUI != null)
            orderUI.SetPatience(normalizedPatience);

        if (patienceRemaining <= 0f)
            ExpireOrder();
    }

    public void Initialize(StoveController controller)
    {
        stoveController = controller;

        if (stoveController == null)
        {
            Debug.LogError(
                "CustomerOrderController: StoveController não configurado.",
                this
            );
            return;
        }

        orderCompleted = false;
        orderExpired = false;
        patienceRunning = false;

        ChooseRandomRecipe();
    }

    private void ChooseRandomRecipe()
    {
        if (stoveController.recipes == null ||
            stoveController.recipes.Length == 0)
        {
            Debug.LogWarning(
                "Não existem receitas configuradas no fogão."
            );
            return;
        }

        List<CookingRecipe> validRecipes =
            new List<CookingRecipe>();

        foreach (CookingRecipe recipe in stoveController.recipes)
        {
            if (recipe == null ||
                recipe.result == null ||
                recipe.result.itemPrefab == null)
            {
                continue;
            }

            validRecipes.Add(recipe);
        }

        if (validRecipes.Count == 0)
        {
            Debug.LogWarning(
                "Nenhuma receita possui um prato de resultado válido."
            );
            return;
        }

        int randomIndex =
            Random.Range(0, validRecipes.Count);

        requestedRecipe = validRecipes[randomIndex];

        Sprite foodSprite = GetFoodSprite(
            requestedRecipe.result.itemPrefab
        );

        if (foodSprite == null)
        {
            Debug.LogError(
                "Não foi possível encontrar o Sprite do prato: " +
                requestedRecipe.result.itemPrefab.name
            );
            return;
        }

        if (orderUI == null)
        {
            Debug.LogError(
                "CustomerOrderController: NPCOrderUI não foi configurado.",
                this
            );
            return;
        }

        orderUI.Show(foodSprite);

        // A paciência começa quando o pedido aparece.
        patienceRemaining = Mathf.Max(0.1f, patienceDuration);
        patienceRunning = true;

        Debug.Log(
            "Pedido do NPC: " +
            requestedRecipe.result.itemPrefab.name +
            ". Paciência: " +
            patienceRemaining +
            " segundos."
        );
    }

    public bool TryServe(InventoryItem offeredItem)
    {
        if (orderCompleted ||
            orderExpired ||
            requestedRecipe == null ||
            offeredItem == null ||
            offeredItem.itemPrefab == null)
        {
            return false;
        }

        // Evita aceitar um prato se o prazo já tiver terminado.
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

        CustomerNPC customerNPC =
            GetComponent<CustomerNPC>();

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
        {
            Debug.LogError(
                "Não foi possível obter o Sprite do prato entregue.",
                this
            );
            return false;
        }

        // A entrega correta interrompe a paciência.
        patienceRunning = false;
        orderCompleted = true;

        if (orderUI != null)
            orderUI.Hide();

        // Mantém o comportamento existente de servir a comida.
        customerNPC.ServeFood(foodSprite);

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

        CustomerNPC customerNPC =
            GetComponent<CustomerNPC>();

        if (customerNPC != null)
        {
            // Leave libera a cadeira e remove o NPC.
            customerNPC.Leave();
        }
        else
        {
            Debug.LogError(
                "Não foi possível remover o NPC: CustomerNPC não encontrado.",
                this
            );
        }
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
        {
            image =
                foodPrefab.GetComponentInChildren<Image>(true);
        }

        if (image != null)
            return image.sprite;

        return null;
    }
}
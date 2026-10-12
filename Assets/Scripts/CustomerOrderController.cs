using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomerOrderController : MonoBehaviour
{
    [Header("Interface do pedido")]
    [SerializeField] private NPCOrderUI orderUI;

    private StoveController stoveController;
    private CookingRecipe requestedRecipe;
    private bool orderCompleted;

    public CookingRecipe RequestedRecipe => requestedRecipe;
    public bool OrderCompleted => orderCompleted;

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

        int randomIndex = Random.Range(0, validRecipes.Count);
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

        Debug.Log(
            "Pedido do NPC: " +
            requestedRecipe.result.itemPrefab.name
        );
    }

    public bool TryServe(InventoryItem offeredItem)
    {
        if (orderCompleted ||
            requestedRecipe == null ||
            offeredItem == null ||
            offeredItem.itemPrefab == null)
        {
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

        // Confere se o NPC pode receber e mostrar o prato.
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

        // Registra a entrega e esconde o ícone do pedido.
        orderCompleted = true;

        if (orderUI != null)
            orderUI.Hide();

        // Exibe o prato sobre a mesa e inicia os 15 segundos.
        customerNPC.ServeFood(foodSprite);

        Debug.Log(
            "Pedido correto entregue: " + requestedPrefab.name
        );

        return true;
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

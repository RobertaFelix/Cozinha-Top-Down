using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Stove : MonoBehaviour
{
    [SerializeField]
    private StoveController stoveController;

    [SerializeField]
    private InventoryController inventoryController;

    [SerializeField]
    private HotbarController hotbarController;

    private bool playerNearby;

    private void Update()
    {
        if (!playerNearby)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact();
        }
    }

    private void Interact()
    {
        if (stoveController.CookingFinished)
        {
            CollectFood();
            return;
        }

        if (stoveController.IsCooking)
        {
            Debug.Log(
                "O fogão ainda está cozinhando."
            );

            return;
        }

        StartCooking();
    }

    private void StartCooking()
    {
        List<InventoryItem> allItems =
            new List<InventoryItem>();

        // Itens do inventário grande
        InventoryItem[] inventoryItems =
            inventoryController.GetInventoryItems();

        // Itens da hotbar
        InventoryItem[] hotbarItems =
            hotbarController.GetHotbarItems();

        allItems.AddRange(inventoryItems);
        allItems.AddRange(hotbarItems);

        InventoryItem[] combinedInventory =
            allItems.ToArray();

        Debug.Log(
            "Itens disponíveis para cozinhar: " +
            combinedInventory.Length
        );

        CookingRecipe recipe =
            stoveController.FindRecipe(
                combinedInventory
            );

        if (recipe == null)
        {
            Debug.Log(
                "NENHUMA RECEITA DISPONÍVEL."
            );

            return;
        }

        Debug.Log(
            "RECEITA ENCONTRADA!"
        );

        // Remove os ingredientes.
        foreach (
            InventoryItem ingredient
            in recipe.ingredients)
        {
            bool removed =
                RemoveIngredient(
                    ingredient
                );

            if (!removed)
            {
                Debug.LogError(
                    "Não conseguiu remover: " +
                    ingredient.itemPrefab.name
                );

                return;
            }
        }

        stoveController.StartCooking(recipe);
    }

    private bool RemoveIngredient(
        InventoryItem ingredient)
    {
        // Primeiro tenta remover da Hotbar
        if (hotbarController.RemoveItem(
                ingredient))
        {
            return true;
        }

        // Depois tenta remover do inventário
        if (inventoryController.RemoveItem(
                ingredient))
        {
            return true;
        }

        return false;
    }

    private void CollectFood()
    {
    InventoryItem food =
        stoveController.GetCookedFood();

    if (food == null)
        return;

    // Tenta colocar na Hotbar ANTES
    // de retirar a comida do fogão.
    bool added =
        hotbarController.AddItem(food);

    if (!added)
    {
        Debug.Log(
            "Hotbar cheia. Não foi possível pegar a comida."
        );

        return;
    }

    stoveController.ClearCookedFood();

    Debug.Log(
        "COMIDA ADICIONADA À HOTBAR: " +
        food.itemPrefab.name
    );
}

    private void OnTriggerEnter2D(
        Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = true;

            Debug.Log(
                "Player entrou perto do fogão."
            );
        }
    }

    private void OnTriggerExit2D(
        Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = false;

            Debug.Log(
                "Player saiu do fogão."
            );
        }
    }
}
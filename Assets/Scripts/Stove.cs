using UnityEngine;
using UnityEngine.InputSystem;

public class Stove : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField]
    private StoveController stoveController;

    [SerializeField]
    private StoveInventoryController stoveInventoryController;

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
        // ==========================================
        // RECEITA PRONTA
        // ==========================================

        if (stoveController.CookingFinished)
        {
            CollectFood();

            return;
        }

        // ==========================================
        // AINDA COZINHANDO
        // ==========================================

        if (stoveController.IsCooking)
        {
            Debug.Log(
                "O fogão ainda está cozinhando."
            );

            return;
        }

        // ==========================================
        // INTERFACE JÁ ESTÁ ABERTA
        // SEGUNDO E
        // ==========================================

        if (stoveInventoryController.IsOpen)
        {
            StartCooking();

            return;
        }

        // ==========================================
        // PRIMEIRO E
        // ABRIR INTERFACE
        // ==========================================

        stoveInventoryController
            .OpenStoveInventory();

        Debug.Log(
            "Interface do fogão aberta."
        );
    }

    private void StartCooking()
    {
        InventoryItem[] stoveItems =
            stoveInventoryController
                .GetStoveItems();

        Debug.Log(
            "Itens colocados no fogão: " +
            stoveItems.Length
        );

        // ==========================================
        // VERIFICAR SE A PANELA FOI USADA
        // ==========================================

        bool panWasUsed =
            Pan.WasUsedInRecipe(stoveItems);

        // ==========================================
        // VERIFICAR SE O POT FOI USADO
        // ==========================================

        bool potWasUsed =
            Pot.WasUsedInRecipe(stoveItems);

        // ==========================================
        // PROCURAR RECEITA
        // ==========================================

        CookingRecipe recipe =
            stoveController.FindRecipe(
                stoveItems
            );

        // ==========================================
        // NÃO ENCONTROU RECEITA
        // ==========================================

        if (recipe == null)
        {
            Debug.Log(
                "NENHUMA RECEITA DISPONÍVEL COM OS ITENS DO FOGÃO."
            );

            return;
        }

        // ==========================================
        // RECEITA ENCONTRADA
        // ==========================================

        Debug.Log(
            "RECEITA ENCONTRADA!"
        );

        // ==========================================
        // REMOVER INGREDIENTES
        // ==========================================

        stoveInventoryController
            .ClearIngredients();

        // ==========================================
        // FECHAR INTERFACE
        // ==========================================

        stoveInventoryController
            .CloseStoveInventory();

        // ==========================================
        // COMEÇAR RECEITA
        // ==========================================

        stoveController.StartCooking(
            recipe,
            panWasUsed,
            potWasUsed
        );
    }

    private void CollectFood()
    {
        InventoryItem food =
            stoveController.GetCookedFood();

        if (food == null)
            return;

        // ==========================================
        // VERIFICAR UTENSÍLIOS USADOS
        // ==========================================

        bool panWasUsed =
            stoveController.PanWasUsed;

        bool potWasUsed =
            stoveController.PotWasUsed;

        // ==========================================
        // TENTAR COLOCAR NA HOTBAR
        // ==========================================

        bool added =
            hotbarController.AddItem(food);

        // ==========================================
        // HOTBAR CHEIA
        // ==========================================

        if (!added)
        {
            Debug.Log(
                "Hotbar cheia. Não foi possível pegar a comida."
            );

            return;
        }

        // ==========================================
        // COMIDA PEGADA
        // ==========================================

        stoveController.ClearCookedFood();

        // ==========================================
        // DEVOLVER A PANELA
        // ==========================================

        if (panWasUsed)
        {
            Pan.ReturnToCounter();
        }

        // ==========================================
        // DEVOLVER O POT
        // ==========================================

        if (potWasUsed)
        {
            Pot.ReturnToCounter();
        }

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
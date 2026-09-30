using UnityEngine;

public class StoveController : MonoBehaviour
{
    [Header("Receitas")]
    [SerializeField] private CookingRecipe[] recipes;


    private CookingRecipe currentRecipe;

    private float timer;

    private bool cooking;

    private bool finished;


    public bool IsCooking
    {
        get { return cooking; }
    }


    public bool CookingFinished
    {
        get { return finished; }
    }


    // ==========================================
    // UPDATE
    // ==========================================

    private void Update()
    {
        if (!cooking)
            return;


        timer += Time.deltaTime;


        if (timer >= currentRecipe.cookingTime)
        {
            cooking = false;
            finished = true;

            Debug.Log(
                "COZIMENTO TERMINADO!"
            );
        }
    }


public CookingRecipe FindRecipe(InventoryItem[] inventory)
{
    Debug.Log("========== VERIFICANDO RECEITAS ==========");

    Debug.Log("Itens no inventário: " + inventory.Length);

    foreach (InventoryItem item in inventory)
    {
        if (item == null)
        {
            Debug.Log("Inventário contém item NULL");
            continue;
        }

        if (item.itemPrefab == null)
        {
            Debug.Log("InventoryItem sem itemPrefab");
            continue;
        }

        Debug.Log(
            "Inventário: " +
            item.itemPrefab.name
        );
    }


    if (recipes == null || recipes.Length == 0)
    {
        Debug.LogError(
            "O StoveController NÃO possui receitas configuradas."
        );

        return null;
    }


    foreach (CookingRecipe recipe in recipes)
    {
        if (recipe == null)
            continue;

        Debug.Log(
            "--------------------------------------"
        );

        Debug.Log(
            "Verificando receita: " +
            recipe.result.itemPrefab.name
        );


        foreach (InventoryItem ingredient
            in recipe.ingredients)
        {
            if (ingredient == null)
            {
                Debug.Log(
                    "Ingrediente NULL"
                );

                continue;
            }

            if (ingredient.itemPrefab == null)
            {
                Debug.Log(
                    "Ingrediente sem itemPrefab"
                );

                continue;
            }

            Debug.Log(
                "Ingrediente necessário: " +
                ingredient.itemPrefab.name
            );
        }


        if (RecipeCanBeMade(
            recipe,
            inventory))
        {
            Debug.Log(
                ">>> RECEITA ENCONTRADA <<<"
            );

            return recipe;
        }
    }


    Debug.Log(
        "========== NENHUMA RECEITA =========="
    );

    return null;
    }


    private bool RecipeCanBeMade(
    CookingRecipe recipe,
    InventoryItem[] inventory)
    {
    bool[] used =
        new bool[inventory.Length];


    foreach (InventoryItem required
        in recipe.ingredients)
    {
        if (required == null)
            return false;

        if (required.itemPrefab == null)
            return false;


        bool found = false;


        for (int i = 0;
             i < inventory.Length;
             i++)
        {
            if (used[i])
                continue;

            if (inventory[i] == null)
                continue;

            if (inventory[i].itemPrefab == null)
                continue;


            Debug.Log(
                "Comparando: " +
                inventory[i].itemPrefab.name +
                " VS " +
                required.itemPrefab.name
            );


            if (inventory[i].itemPrefab ==
                required.itemPrefab)
            {
                used[i] = true;

                found = true;

                break;
            }
        }


        if (!found)
        {
            Debug.Log(
                "Ingrediente não encontrado: " +
                required.itemPrefab.name
            );

            return false;
        }
    }


    return true;
    }


    // ==========================================
    // COMEÇAR
    // ==========================================

    public bool StartCooking(
        CookingRecipe recipe)
    {
        if (cooking)
            return false;


        if (finished)
            return false;


        if (recipe == null)
            return false;


        currentRecipe = recipe;

        timer = 0f;

        cooking = true;

        finished = false;


        Debug.Log(
            "COMEÇOU A COZINHAR: " +
            recipe.result.itemPrefab.name
        );


        return true;
    }

    public InventoryItem GetCookedFood()
    {
        if (!finished)
            return null;

        if (currentRecipe == null)
            return null;

        return currentRecipe.result;
    }

    public void ClearCookedFood()
    {
        currentRecipe = null;
        finished = false;
        timer = 0f;
    }
}
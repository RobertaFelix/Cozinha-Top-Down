using UnityEngine;

public class StoveController : MonoBehaviour
{
    [Header("Receitas")]
    public CookingRecipe[] recipes;

    private CookingRecipe currentRecipe;

    private float timer;

    private bool cooking;

    private bool finished;

    private bool panWasUsed;

    private bool potWasUsed;

    public bool IsCooking
    {
        get
        {
            return cooking;
        }
    }

    public bool CookingFinished
    {
        get
        {
            return finished;
        }
    }

    public bool PanWasUsed
    {
        get
        {
            return panWasUsed;
        }
    }

    public bool PotWasUsed
    {
        get
        {
            return potWasUsed;
        }
    }

    private void Update()
    {
        if (!cooking)
            return;

        timer += Time.deltaTime;

        if (timer >= currentRecipe.cookingTime)
        {
            timer = currentRecipe.cookingTime;

            cooking = false;

            finished = true;

            Debug.Log(
                "RECEITA TERMINOU DE COZINHAR!"
            );
        }
    }

    public CookingRecipe FindRecipe(
        InventoryItem[] stoveItems)
    {
        if (stoveItems == null)
            return null;

        foreach (CookingRecipe recipe in recipes)
        {
            if (recipe == null)
                continue;

            if (recipe.ingredients == null)
                continue;

            if (RecipeCanBeMade(
                recipe,
                stoveItems))
            {
                return recipe;
            }
        }

        return null;
    }

    private bool RecipeCanBeMade(
        CookingRecipe recipe,
        InventoryItem[] stoveItems)
    {
        bool[] used =
            new bool[stoveItems.Length];

        foreach (
            InventoryItem required
            in recipe.ingredients)
        {
            if (required == null)
                return false;

            if (required.itemPrefab == null)
                return false;

            bool found = false;

            for (
                int i = 0;
                i < stoveItems.Length;
                i++)
            {
                if (used[i])
                    continue;

                if (stoveItems[i] == null)
                    continue;

                if (stoveItems[i].itemPrefab == null)
                    continue;

                if (
                    stoveItems[i].itemPrefab ==
                    required.itemPrefab)
                {
                    used[i] = true;

                    found = true;

                    break;
                }
            }

            if (!found)
                return false;
        }

        return true;
    }

    public void StartCooking(
        CookingRecipe recipe,
        bool usedPan,
        bool usedPot)
    {
        if (recipe == null)
            return;

        currentRecipe = recipe;

        panWasUsed = usedPan;

        potWasUsed = usedPot;

        timer = 0f;

        cooking = true;

        finished = false;

        Debug.Log(
            "COMEÇOU A COZINHAR: " +
            recipe.result.itemPrefab.name
        );
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

        panWasUsed = false;

        potWasUsed = false;
    }
}
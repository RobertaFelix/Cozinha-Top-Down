using UnityEngine;
using UnityEngine.UI;

public class StoveController : MonoBehaviour
{
    [Header("Receitas")]
    public CookingRecipe[] recipes;


    [Header("UI do cozimento")]
    [SerializeField]
    private CookingProgressUI cookingProgressUI;


    [Header("Livro de Receitas")]
    [SerializeField]
    private RecipeBookController recipeBookController;


    private CookingRecipe currentRecipe;

    private float timer;

    private bool cooking;
    private bool finished;

    private bool panWasUsed;
    private bool potWasUsed;


    public bool IsCooking => cooking;

    public bool CookingFinished => finished;

    public bool PanWasUsed => panWasUsed;

    public bool PotWasUsed => potWasUsed;


    private void Update()
    {
        if (!cooking)
            return;


        if (currentRecipe == null)
            return;


        timer += Time.deltaTime;


        // Calcula o progresso entre 0 e 1
        float progress =
            timer / currentRecipe.cookingTime;


        // Atualiza a barra verde
        if (cookingProgressUI != null)
        {
            cookingProgressUI.SetProgress(progress);
        }


        // Verifica se terminou
        if (timer >= currentRecipe.cookingTime)
        {
            timer = currentRecipe.cookingTime;

            cooking = false;
            finished = true;


            // Garante que fique 100% preenchido
            if (cookingProgressUI != null)
            {
                cookingProgressUI.SetProgress(1f);
            }


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


        foreach (CookingRecipe recipe
                 in recipes)
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


        foreach (InventoryItem required
                 in recipe.ingredients)
        {
            if (required == null)
                return false;


            if (required.itemPrefab == null)
                return false;


            bool found = false;


            for (int i = 0;
                 i < stoveItems.Length;
                 i++)
            {
                if (used[i])
                    continue;


                if (stoveItems[i] == null)
                    continue;


                if (stoveItems[i].itemPrefab == null)
                    continue;


                if (stoveItems[i].itemPrefab ==
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


        if (recipe.result == null)
        {
            Debug.LogError(
                "A receita não possui resultado."
            );

            return;
        }


        if (recipe.result.itemPrefab == null)
        {
            Debug.LogError(
                "O resultado da receita não possui itemPrefab."
            );

            return;
        }


        currentRecipe = recipe;

        panWasUsed = usedPan;

        potWasUsed = usedPot;

        timer = 0f;

        cooking = true;

        finished = false;


        // Mostra a UI de cozimento
        if (cookingProgressUI != null)
        {
            Sprite foodSprite =
                GetFoodSprite(
                    recipe.result.itemPrefab
                );


            cookingProgressUI.Show(
                foodSprite,
                0f
            );
        }


        Debug.Log(
            "COMEÇOU A COZINHAR: " +
            recipe.result.itemPrefab.name
        );
    }


    private Sprite GetFoodSprite(
        GameObject foodPrefab)
    {
        if (foodPrefab == null)
            return null;


        // Primeiro tenta encontrar um Image
        // caso o item seja um prefab de UI.
        Image image =
            foodPrefab.GetComponent<Image>();


        if (image != null)
        {
            return image.sprite;
        }


        // Caso seja um objeto do mundo
        // com SpriteRenderer.
        SpriteRenderer spriteRenderer =
            foodPrefab.GetComponent<SpriteRenderer>();


        if (spriteRenderer != null)
        {
            return spriteRenderer.sprite;
        }


        // Caso o SpriteRenderer esteja em um filho.
        spriteRenderer =
            foodPrefab.GetComponentInChildren<SpriteRenderer>();


        if (spriteRenderer != null)
        {
            return spriteRenderer.sprite;
        }


        // Caso o Image esteja em um filho.
        image =
            foodPrefab.GetComponentInChildren<Image>();


        if (image != null)
        {
            return image.sprite;
        }


        Debug.LogWarning(
            "Não foi possível encontrar a imagem da comida: " +
            foodPrefab.name
        );


        return null;
    }


    public InventoryItem GetCookedFood()
    {
        if (!finished)
            return null;


        if (currentRecipe == null)
            return null;


        return currentRecipe.result;
    }


    public void UnlockCurrentRecipe()
    {
        if (currentRecipe == null)
        {
            Debug.LogWarning(
                "StoveController: Não existe receita atual para desbloquear."
            );

            return;
        }


        if (recipeBookController == null)
        {
            Debug.LogWarning(
                "StoveController: RecipeBookController não foi configurado."
            );

            return;
        }


        recipeBookController.UnlockRecipe(
            currentRecipe
        );
    }


    public void ClearCookedFood()
    {
        currentRecipe = null;

        finished = false;

        timer = 0f;

        panWasUsed = false;

        potWasUsed = false;


        // Esconde a UI depois que
        // a comida foi coletada.
        if (cookingProgressUI != null)
        {
            cookingProgressUI.Hide();
        }
    }
}
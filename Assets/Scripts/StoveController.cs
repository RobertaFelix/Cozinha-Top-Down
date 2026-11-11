using UnityEngine;

public class StoveController : MonoBehaviour
{
    [Header("Receitas")]
    public CookingRecipe[] recipes;

    [Header("Interface de cozimento")]
    [SerializeField]
    private CookingProgressUI cookingProgressUI;

    [Header("Livro de receitas")]
    [SerializeField]
    private RecipeBookController recipeBookController;

    [Header("Tempo para queimar")]
    [SerializeField]
    private float burnDuration = 18f;

    [Header("Upgrade de preparo")]
    [SerializeField, Range(0.01f, 1f)] private float upgradedCookingTimeMultiplier = 0.75f;

    public float CurrentCookingDuration => currentRecipe == null ? 0f : Mathf.Max(0.01f,
        currentRecipe.cookingTime * (UpgradePurchases.Has(UpgradePurchases.CookingClock)
            ? Mathf.Clamp(upgradedCookingTimeMultiplier, 0.01f, 1f) : 1f));

    private CookingRecipe currentRecipe;

    private float timer;
    private float burnTimer;

    private bool cooking;
    private bool finished;

    private bool panWasUsed;
    private bool potWasUsed;

    public bool IsCooking => cooking;
    public bool CookingFinished => finished;
    public bool PanWasUsed => panWasUsed;
    public bool PotWasUsed => potWasUsed;

    public CookingRecipe CurrentRecipe => currentRecipe;

    private void Update()
    {
        if (currentRecipe == null)
            return;

        // Cozimento normal: a barra verde aumenta.
        if (cooking)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                timer / CurrentCookingDuration
            );

            if (cookingProgressUI != null)
            {
                cookingProgressUI.SetProgress(progress);
            }

            if (timer >= CurrentCookingDuration)
            {
                cooking = false;
                finished = true;

                timer = CurrentCookingDuration;
                burnTimer = 0f;

                if (cookingProgressUI != null)
                {
                    cookingProgressUI.SetProgress(1f);
                    cookingProgressUI.SetBurnProgress(0f);
                }

                Debug.Log(
                    "Comida pronta! Você tem " +
                    burnDuration +
                    " segundos para recolhê-la."
                );
            }

            return;
        }

        // Comida pronta: a barra vermelha aumenta até queimar.
        if (finished)
        {
            burnTimer += Time.deltaTime;

            float burnProgress = Mathf.Clamp01(
                burnTimer / burnDuration
            );

            if (cookingProgressUI != null)
            {
                cookingProgressUI.SetBurnProgress(burnProgress);
            }

            if (burnTimer >= burnDuration)
            {
                BurnCurrentFood();
            }
        }
    }

    // Procura uma receita que corresponda aos itens do fogão.
    public CookingRecipe FindRecipe(InventoryItem[] stoveItems)
    {
        if (stoveItems == null || recipes == null)
            return null;

        // Conta os itens presentes nos slots do fogão.
        int itemCount = 0;

        foreach (InventoryItem item in stoveItems)
        {
            if (item != null && item.itemPrefab != null)
            {
                itemCount++;
            }
        }

        foreach (CookingRecipe recipe in recipes)
        {
            if (recipe == null || recipe.ingredients == null)
                continue;

            // Cada ingrediente da receita precisa de um item correspondente.
            if (recipe.ingredients.Length != itemCount)
                continue;

            bool[] usedItems = new bool[stoveItems.Length];
            bool allIngredientsFound = true;

            foreach (InventoryItem ingredient in recipe.ingredients)
            {
                if (ingredient == null || ingredient.itemPrefab == null)
                {
                    allIngredientsFound = false;
                    break;
                }

                bool found = false;

                for (int i = 0; i < stoveItems.Length; i++)
                {
                    InventoryItem stoveItem = stoveItems[i];

                    if (usedItems[i] ||
                        stoveItem == null ||
                        stoveItem.itemPrefab == null)
                    {
                        continue;
                    }

                    if (stoveItem.itemPrefab == ingredient.itemPrefab)
                    {
                        usedItems[i] = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    allIngredientsFound = false;
                    break;
                }
            }

            if (allIngredientsFound)
            {
                return recipe;
            }
        }

        return null;
    }

    public CookingSaveState CaptureCookingState() => new CookingSaveState
    {
        recipeId = currentRecipe == null ? "" : RecipeSaveModule.Id(currentRecipe),
        timer = timer, burnTimer = burnTimer, cooking = cooking, finished = finished,
        usedPan = panWasUsed, usedPot = potWasUsed
    };

    public void RestoreCookingState(CookingSaveState state)
    {
        ClearCookedFood();
        currentRecipe = RecipeSaveModule.FindRecipe(this, state.recipeId);
        timer = state.timer; burnTimer = state.burnTimer;
        cooking = state.cooking; finished = state.finished;
        panWasUsed = state.usedPan; potWasUsed = state.usedPot;
        if (currentRecipe != null && cookingProgressUI != null)
        {
            cookingProgressUI.Show(GetFoodSprite(currentRecipe), Mathf.Clamp01(timer / CurrentCookingDuration));
            cookingProgressUI.SetBurnProgress(Mathf.Clamp01(burnTimer / Mathf.Max(.01f, burnDuration)));
        }
    }

    // Inicia o preparo da receita.
    public void StartCooking(
        CookingRecipe recipe,
        bool usedPan,
        bool usedPot
    )
    {
        if (recipe == null)
        {
            Debug.LogWarning("Receita não encontrada.");
            return;
        }

        if (recipe.result == null ||
            recipe.result.itemPrefab == null)
        {
            Debug.LogWarning(
                "A receita não possui um resultado configurado."
            );
            return;
        }

        if (cooking || finished)
        {
            Debug.Log("O fogão já está ocupado.");
            return;
        }

        if (recipe.cookingTime <= 0f)
        {
            Debug.LogWarning(
                "O tempo de cozimento precisa ser maior que zero."
            );
            return;
        }

        if (burnDuration <= 0f)
        {
            Debug.LogWarning(
                "O tempo para queimar precisa ser maior que zero."
            );
            return;
        }

        currentRecipe = recipe;

        timer = 0f;
        burnTimer = 0f;

        cooking = true;
        finished = false;

        panWasUsed = usedPan;
        potWasUsed = usedPot;

        Sprite foodSprite = GetFoodSprite(recipe);
        Debug.Log(
            foodSprite == null
                ? "ERRO: sprite do prato não encontrado."
                : "Sprite do prato encontrado: "
        );

        if (cookingProgressUI != null)
        {
            // Sua versão de Show exige Sprite e progresso.
            cookingProgressUI.Show(foodSprite, 0f);
            cookingProgressUI.SetProgress(0f);
            cookingProgressUI.SetBurnProgress(0f);
        }

        Debug.Log("Iniciando o preparo da receita.");
    }

    // Obtém o sprite do prato.
    private Sprite GetFoodSprite(CookingRecipe recipe)
    {
        if (recipe == null ||
            recipe.result == null ||
            recipe.result.itemPrefab == null)
        {
            Debug.LogError(
                "A receita ou o prefab do resultado não está configurado."
            );

            return null;
        }

        GameObject foodPrefab = recipe.result.itemPrefab;

        // Primeiro, procura um SpriteRenderer.
        SpriteRenderer spriteRenderer =
            foodPrefab.GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Debug.Log(
                "Sprite encontrado no SpriteRenderer "
            );

            return spriteRenderer.sprite;
        }

        // Se não houver SpriteRenderer, procura uma Image de UI.
        UnityEngine.UI.Image foodImage =
            foodPrefab.GetComponentInChildren<UnityEngine.UI.Image>(true);

        if (foodImage != null && foodImage.sprite != null)
        {
            Debug.Log(
                "Sprite encontrado no componente Image"
            );

            return foodImage.sprite;
        }

        Debug.LogError(
            "O prefab não possui SpriteRenderer ou Image com sprite atribuído."
        );

        return null;
    }

    // Retorna a comida somente quando estiver pronta.
    public InventoryItem GetCookedFood()
    {
        if (!finished || currentRecipe == null)
            return null;

        return currentRecipe.result;
    }

    // Desbloqueia a receita no livro.
    public void UnlockCurrentRecipe()
    {
        if (currentRecipe == null)
            return;

        if (recipeBookController != null)
        {
            // O método do livro recebe CookingRecipe.
            recipeBookController.UnlockRecipe(currentRecipe);
        }
    }

    // Libera o fogão depois que a comida é recolhida.
    public void ClearCookedFood()
    {
        currentRecipe = null;

        timer = 0f;
        burnTimer = 0f;

        cooking = false;
        finished = false;

        panWasUsed = false;
        potWasUsed = false;

        if (cookingProgressUI != null)
        {
            cookingProgressUI.Hide();
        }

        Debug.Log("Fogão liberado para uma nova receita.");
    }

    // Queima a comida e devolve os utensílios usados.
    private void BurnCurrentFood()
    {
        if (currentRecipe == null || !finished)
            return;

        // Guardamos os estados antes de limpar a receita.
        bool returnPan = panWasUsed;
        bool returnPot = potWasUsed;

        Debug.LogWarning("A comida queimou! A receita foi perdida.");

        currentRecipe = null;

        timer = 0f;
        burnTimer = 0f;

        cooking = false;
        finished = false;

        panWasUsed = false;
        potWasUsed = false;

        if (cookingProgressUI != null)
        {
            cookingProgressUI.Hide();
        }

        // ReturnToCounter é estático no seu projeto.
        if (returnPan)
        {
            Pan.ReturnToCounter();
        }

        if (returnPot)
        {
            Pot.ReturnToCounter();
        }
    }
}
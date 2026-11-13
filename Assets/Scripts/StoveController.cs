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

    [Header("Upgrade de temperatura")]
    [SerializeField, Min(1f)] private float upgradedBurnTimeMultiplier = 1.5f;
    public float CurrentBurnDuration => Mathf.Max(.01f, burnDuration *
        (UpgradePurchases.Has(UpgradePurchases.Thermometer) ? Mathf.Max(1f, upgradedBurnTimeMultiplier) : 1f));

    public float CurrentCookingDuration => currentRecipe == null ? 0f : Mathf.Max(0.01f,
        currentRecipe.cookingTime * (UpgradePurchases.Has(UpgradePurchases.CookingClock)
            ? Mathf.Clamp(upgradedCookingTimeMultiplier, 0.01f, 1f) : 1f));

    public GameObject Mounted { get; set; }
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
                    "Comida pronta! VocÃƒÂª tem " +
                    CurrentBurnDuration +
                    " segundos para recolhÃƒÂª-la."
                );
            }

            return;
        }

        // Comida pronta: a barra vermelha aumenta atÃƒÂ© queimar.
        if (finished)
        {
            burnTimer += Time.deltaTime;

            float burnProgress = Mathf.Clamp01(
                burnTimer / CurrentBurnDuration
            );

            if (cookingProgressUI != null)
            {
                cookingProgressUI.SetBurnProgress(burnProgress);
            }

            if (burnTimer >= CurrentBurnDuration)
            {
                BurnCurrentFood();
            }
        }
    }

    // Procura uma receita que corresponda aos itens do fogÃƒÂ£o.
    public CookingRecipe FindRecipe(InventoryItem[] stoveItems)
    {
        if (stoveItems == null || recipes == null)
            return null;

        var available = new System.Collections.Generic.Dictionary<GameObject, int>();
        foreach (var item in stoveItems)
        {
            if (item?.itemPrefab == null) continue;
            available.TryGetValue(item.itemPrefab, out var count);
            available[item.itemPrefab] = count + item.amount;
        }
        foreach (var recipe in recipes)
        {
            if (recipe?.ingredients == null || recipe.ingredients.Length == 0) continue;
            var required = new System.Collections.Generic.Dictionary<GameObject, int>();
            bool valid = true;
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient?.itemPrefab == null) { valid = false; break; }
                required.TryGetValue(ingredient.itemPrefab, out var count);
                required[ingredient.itemPrefab] = count + Mathf.Max(1, ingredient.amount);
            }
            if (!valid || required.Count != available.Count) continue;
            foreach (var pair in required)
                if (!available.TryGetValue(pair.Key, out var count) || count < pair.Value) { valid = false; break; }
            if (valid) return recipe;
        }
        return null;
    }

    public CookingSaveState CaptureCookingState() => new CookingSaveState
    {
        recipeId = currentRecipe == null ? "" : RecipeSaveModule.Id(currentRecipe),
        timer = timer, burnTimer = burnTimer, cooking = cooking, finished = finished,
        mountedId = SaveItemCatalog.Instance.Id(Mounted), usedPan = panWasUsed, usedPot = potWasUsed
    };

    public void RestoreCookingState(CookingSaveState state)
    {
        ClearCookedFood();
        Mounted = SaveItemCatalog.Instance.Prefab(state.mountedId);
        currentRecipe = RecipeSaveModule.FindRecipe(this, state.recipeId);
        if (!Mounted && currentRecipe != null)
            foreach (var ingredient in currentRecipe.ingredients)
                if (ingredient.itemPrefab.TryGetComponent<CookwareItem>(out var tool) && !tool.isPlate)
                    Mounted = ingredient.itemPrefab;
        timer = state.timer; burnTimer = state.burnTimer;
        cooking = state.cooking; finished = state.finished;
        panWasUsed = state.usedPan; potWasUsed = state.usedPot;
        if (currentRecipe != null && cookingProgressUI != null)
        {
            cookingProgressUI.Show(GetFoodSprite(currentRecipe), Mathf.Clamp01(timer / CurrentCookingDuration));
            cookingProgressUI.SetBurnProgress(Mathf.Clamp01(burnTimer / CurrentBurnDuration));
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
            Debug.LogWarning("Receita nÃƒÂ£o encontrada.");
            return;
        }

        if (recipe.result == null ||
            recipe.result.itemPrefab == null)
        {
            Debug.LogWarning(
                "A receita nÃƒÂ£o possui um resultado configurado."
            );
            return;
        }

        if (cooking || finished)
        {
            Debug.Log("O fogÃƒÂ£o jÃƒÂ¡ estÃƒÂ¡ ocupado.");
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
                ? "ERRO: sprite do prato nÃƒÂ£o encontrado."
                : "Sprite do prato encontrado: "
        );

        if (cookingProgressUI != null)
        {
            // Sua versÃƒÂ£o de Show exige Sprite e progresso.
            cookingProgressUI.Show(foodSprite, 0f);
            cookingProgressUI.SetProgress(0f);
            cookingProgressUI.SetBurnProgress(0f);
        }

        Debug.Log("Iniciando o preparo da receita.");
    }

    // ObtÃƒÂ©m o sprite do prato.
    private Sprite GetFoodSprite(CookingRecipe recipe)
    {
        if (recipe == null ||
            recipe.result == null ||
            recipe.result.itemPrefab == null)
        {
            Debug.LogError(
                "A receita ou o prefab do resultado nÃƒÂ£o estÃƒÂ¡ configurado."
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

        // Se nÃƒÂ£o houver SpriteRenderer, procura uma Image de UI.
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
            "O prefab nÃƒÂ£o possui SpriteRenderer ou Image com sprite atribuÃƒÂ­do."
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
            // O mÃƒÂ©todo do livro recebe CookingRecipe.
            recipeBookController.UnlockRecipe(currentRecipe);
        }
    }

    // Libera o fogÃƒÂ£o depois que a comida ÃƒÂ© recolhida.
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

        Debug.Log("FogÃƒÂ£o liberado para uma nova receita.");
    }

    // Queima a comida e devolve os utensÃƒÂ­lios usados.
    private void BurnCurrentFood()
    {
        if (currentRecipe == null || !finished)
            return;

        ClearCookedFood();
    }
}

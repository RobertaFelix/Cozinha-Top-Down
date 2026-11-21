
using System.Collections.Generic;
using UnityEngine;

public class RecipeBookController : MonoBehaviour
{
    public static RecipeBookController Instance { get; private set; }

    [Header("Página de receitas")]
    [SerializeField] private GameObject recipePanel;

    [Header("Slots")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private int slotCount;

    [Header("Fonte das receitas")]
    [SerializeField] private StoveController stoveController;

    private readonly List<RecipeBookSlot> createdSlots =
        new List<RecipeBookSlot>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        FindRecipePanel();
        CreateSlots();
    }

    // ==========================================
    // RECUPERAR RECIPES PAGE
    // ==========================================

    private void FindRecipePanel()
    {
        GameObject persistentUI =
            GameObject.Find("PersistentUI");

        if (persistentUI == null)
        {
            Debug.LogError(
                "RecipeBookController: PersistentUI não encontrado!"
            );

            recipePanel = null;
            return;
        }

        Transform[] children =
            persistentUI.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == "RecipesPage")
            {
                recipePanel = child.gameObject;

                Debug.Log(
                    "RecipeBookController: RecipesPage recuperado com sucesso!"
                );

                return;
            }
        }

        Debug.LogError(
            "RecipeBookController: RecipesPage não encontrado dentro de PersistentUI!"
        );

        recipePanel = null;
    }

    // ==========================================
    // CRIAR SLOTS
    // ==========================================

    private void CreateSlots()
    {
        if (recipePanel == null)
        {
            Debug.LogError(
                "RecipeBookController: Recipe Panel não encontrado."
            );

            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError(
                "RecipeBookController: Slot Prefab não configurado."
            );

            return;
        }

        if (stoveController == null)
        {
            Debug.LogError(
                "RecipeBookController: Stove Controller não configurado."
            );

            return;
        }

        // The UI survives a trip to the dungeon. Rebind its slots to the
        // current scene recipes while keeping discoveries from the prior visit.
        var existingSlots = recipePanel.GetComponentsInChildren<RecipeBookSlot>(true);
        var discovered = new HashSet<GameObject>();
        foreach (var slot in existingSlots)
            if (slot.IsUnlocked() && slot.GetRecipe()?.result?.itemPrefab != null)
                discovered.Add(slot.GetRecipe().result.itemPrefab);

        var recipes = new List<CookingRecipe>(ApplianceRecipeCatalog.AllIncludingStove(stoveController));
        int count = Mathf.Max(slotCount, recipes.Count);
        for (int i = 0; i < count; i++)
        {
            var slot = i < existingSlots.Length ? existingSlots[i] :
                Instantiate(slotPrefab, recipePanel.transform).GetComponent<RecipeBookSlot>();
            if (i >= recipes.Count || recipes[i]?.result?.itemPrefab == null) continue;
            slot.Setup(recipes[i]);
            slot.SetUnlocked(discovered.Contains(recipes[i].result.itemPrefab));
            createdSlots.Add(slot);
        }
    }

    // ==========================================
    // DESBLOQUEAR RECEITA
    // ==========================================

    public void UnlockRecipe(CookingRecipe recipe)
    {
        if (recipe == null)
        {
            Debug.LogWarning(
                "RecipeBookController: receita nula."
            );

            return;
        }

        foreach (RecipeBookSlot slot in createdSlots)
        {
            if (slot != null && slot.GetRecipe() == recipe)
            {
                slot.Unlock();

                Debug.Log(
                    "Receita desbloqueada no livro de receitas."
                );

                return;
            }
        }

        Debug.LogWarning(
            "RecipeBookController: receita não encontrada nos slots."
        );
    }

    // ==========================================
    // VERIFICAR RECEITA DESBLOQUEADA
    // ==========================================

    public bool HasUnlockedRecipe(CookingRecipe recipe)
    {
        if (recipe == null)
            return false;

        foreach (RecipeBookSlot slot in createdSlots)
        {
            if (slot != null && slot.GetRecipe() == recipe)
                return slot.IsUnlocked();
        }

        return false;
    }

    public void RestoreUnlockedRecipes(CookingRecipe[] recipes)
    {
        var unlocked = new HashSet<CookingRecipe>(recipes);
        foreach (var slot in createdSlots)
            if (slot != null) slot.SetUnlocked(unlocked.Contains(slot.GetRecipe()));
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

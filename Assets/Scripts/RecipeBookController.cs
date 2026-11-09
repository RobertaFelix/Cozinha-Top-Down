
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

        // Verificar se os slots já existem.
        RecipeBookSlot[] existingSlots =
            recipePanel.GetComponentsInChildren<RecipeBookSlot>(true);

        if (existingSlots.Length > 0)
        {
            foreach (RecipeBookSlot slot in existingSlots)
            {
                if (slot != null && !createdSlots.Contains(slot))
                    createdSlots.Add(slot);
            }

            Debug.Log(
                "RecipeBookController: slots existentes recuperados. Total: " +
                createdSlots.Count
            );

            return;
        }

        CookingRecipe[] recipes = stoveController.recipes;

        if (recipes == null)
        {
            Debug.LogError(
                "RecipeBookController: não existem receitas configuradas."
            );

            return;
        }

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject =
                Instantiate(slotPrefab, recipePanel.transform);

            RecipeBookSlot slot =
                slotObject.GetComponent<RecipeBookSlot>();

            if (slot == null)
            {
                Debug.LogError(
                    "O Slot Prefab não possui RecipeBookSlot."
                );

                Destroy(slotObject);
                continue;
            }

            if (i < recipes.Length && recipes[i] != null)
            {
                slot.Setup(recipes[i]);
                createdSlots.Add(slot);
            }
            else
            {
                Debug.LogWarning(
                    "Não existe receita configurada para o slot " + i + "."
                );
            }
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

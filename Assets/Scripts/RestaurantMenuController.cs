
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RestaurantMenuController : MonoBehaviour
{
    public static RestaurantMenuController Instance { get; private set; }

    [Header("Configuração do cardápio")]
    [SerializeField, Min(1)]
    private int maxDishes = 4;

    [Header("Interface")]
    [SerializeField] private Transform selectedDishesContainer;
    [SerializeField] private GameObject selectedDishPrefab;

    [Header("Livro de receitas")]
    [SerializeField] private RecipeBookController recipeBookController;

    private readonly List<CookingRecipe> selectedRecipes =
        new List<CookingRecipe>();

    private readonly Dictionary<CookingRecipe, GameObject> dishObjects =
        new Dictionary<CookingRecipe, GameObject>();

    public int MaxDishes => maxDishes;
    public int SelectedDishCount => selectedRecipes.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "Existe mais de um RestaurantMenuController na cena."
            );

            return;
        }

        Instance = this;
    }

    private void Start()
    {
        FindSelectedDishesContainer();
        FindRecipeBookController();
        RecoverSelectedDishes();
    }

    // ==========================================
    // RECUPERAR SELECTED DISHES CONTAINER
    // ==========================================

    private void FindSelectedDishesContainer()
    {
        GameObject persistentUI =
            GameObject.Find("PersistentUI");

        if (persistentUI == null)
        {
            Debug.LogError(
                "RestaurantMenuController: PersistentUI não encontrado!"
            );

            selectedDishesContainer = null;
            return;
        }

        Transform[] children =
            persistentUI.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == "MenuPage")
            {
                Transform container =
                    FindChildRecursive(
                        child,
                        "SelectedDishesContainer"
                    );

                if (container != null)
                {
                    selectedDishesContainer = container;

                    Debug.Log(
                        "RestaurantMenuController: SelectedDishesContainer recuperado!"
                    );

                    return;
                }
            }
        }

        Debug.LogError(
            "RestaurantMenuController: SelectedDishesContainer não encontrado dentro de MenuPage!"
        );

        selectedDishesContainer = null;
    }

    private Transform FindChildRecursive(
        Transform parent,
        string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result =
                FindChildRecursive(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    // ==========================================
    // RECUPERAR RECIPE BOOK CONTROLLER
    // ==========================================

    private void FindRecipeBookController()
    {
        if (recipeBookController != null)
            return;

        recipeBookController =
            RecipeBookController.Instance;

        if (recipeBookController != null)
        {
            Debug.Log(
                "RestaurantMenuController: RecipeBookController recuperado!"
            );
        }
        else
        {
            Debug.LogError(
                "RestaurantMenuController: RecipeBookController não encontrado!"
            );
        }
    }

    // ==========================================
    // RECUPERAR PRATOS JÁ SELECIONADOS
    // ==========================================

    private void RecoverSelectedDishes()
    {
        if (selectedDishesContainer == null)
            return;

        Image[] images =
            selectedDishesContainer.GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image.transform == selectedDishesContainer)
                continue;

            Button button =
                image.GetComponent<Button>();

            if (button == null)
                button = image.GetComponentInParent<Button>();

            if (button == null)
                continue;

            // Os pratos recuperados permanecem visíveis.
            // As receitas continuam na lista se este controlador
            // também tiver permanecido ativo.
        }
    }

    // ==========================================
    // ADICIONAR RECEITA
    // ==========================================

    public bool TryAddRecipe(CookingRecipe recipe)
    {
        if (recipe == null)
            return false;

        if (recipeBookController == null)
            FindRecipeBookController();

        if (recipeBookController == null)
        {
            Debug.LogError(
                "RestaurantMenuController: RecipeBookController não encontrado."
            );

            return false;
        }

        if (selectedDishesContainer == null)
            FindSelectedDishesContainer();

        if (selectedDishesContainer == null)
            return false;

        if (!recipeBookController.HasUnlockedRecipe(recipe))
        {
            Debug.Log("Essa receita ainda não foi desbloqueada.");
            return false;
        }

        if (selectedRecipes.Contains(recipe))
        {
            Debug.Log("Esse prato já está no cardápio.");
            return false;
        }

        if (selectedRecipes.Count >= maxDishes)
        {
            Debug.Log("O cardápio já atingiu o limite de pratos.");
            return false;
        }

        if (selectedDishPrefab == null)
        {
            Debug.LogError(
                "Configure o prefab dos pratos selecionados."
            );

            return false;
        }

        if (recipe.result == null || recipe.result.itemPrefab == null)
        {
            Debug.LogWarning(
                "A receita não possui um prato de resultado válido."
            );

            return false;
        }

        Sprite dishSprite = GetSprite(recipe.result.itemPrefab);

        if (dishSprite == null)
        {
            Debug.LogWarning(
                "Não foi possível encontrar o sprite do prato."
            );

            return false;
        }

        GameObject dishObject = Instantiate(
            selectedDishPrefab,
            selectedDishesContainer
        );

        Image dishImage = dishObject.GetComponent<Image>();

        if (dishImage == null)
            dishImage = dishObject.GetComponentInChildren<Image>(true);

        Button dishButton = dishObject.GetComponent<Button>();

        if (dishButton == null)
            dishButton = dishObject.GetComponentInChildren<Button>(true);

        if (dishImage == null || dishButton == null)
        {
            Debug.LogError(
                "O prefab do prato precisa conter uma Image e um Button."
            );

            Destroy(dishObject);
            return false;
        }

        dishImage.sprite = dishSprite;
        dishImage.color = Color.white;
        dishImage.preserveAspect = true;

        dishButton.onClick.RemoveAllListeners();

        dishButton.onClick.AddListener(
            () => RemoveRecipe(recipe)
        );

        selectedRecipes.Add(recipe);
        dishObjects.Add(recipe, dishObject);

        Debug.Log(
            "Prato adicionado ao cardápio. Total: " +
            selectedRecipes.Count + "/" + maxDishes
        );

        return true;
    }

    // ==========================================
    // REMOVER RECEITA
    // ==========================================

    public bool RemoveRecipe(CookingRecipe recipe)
    {
        if (recipe == null || !selectedRecipes.Contains(recipe))
            return false;

        selectedRecipes.Remove(recipe);

        if (dishObjects.TryGetValue(recipe, out GameObject dishObject))
        {
            if (dishObject != null)
                Destroy(dishObject);

            dishObjects.Remove(recipe);
        }

        Debug.Log(
            "Prato removido do cardápio. Total: " +
            selectedRecipes.Count + "/" + maxDishes
        );

        return true;
    }

    // ==========================================
    // VERIFICAR SE ESTÁ NO CARDÁPIO
    // ==========================================

    public bool IsInMenu(CookingRecipe recipe)
    {
        return recipe != null && selectedRecipes.Contains(recipe);
    }

    public IReadOnlyList<CookingRecipe> GetSelectedRecipes()
    {
        return selectedRecipes.AsReadOnly();
    }

    // ==========================================
    // OBTER SPRITE
    // ==========================================

    private Sprite GetSprite(GameObject prefab)
    {
        if (prefab == null)
            return null;

        SpriteRenderer spriteRenderer =
            prefab.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                prefab.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderer != null)
            return spriteRenderer.sprite;

        Image image = prefab.GetComponent<Image>();

        if (image == null)
            image = prefab.GetComponentInChildren<Image>(true);

        return image != null ? image.sprite : null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

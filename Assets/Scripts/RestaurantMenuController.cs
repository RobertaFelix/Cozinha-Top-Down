
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

    public bool TryAddRecipe(CookingRecipe recipe)
    {
        if (recipe == null)
            return false;

        if (recipeBookController == null)
        {
            Debug.LogError(
                "RestaurantMenuController: configure o RecipeBookController."
            );
            return false;
        }

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

        if (selectedDishesContainer == null || selectedDishPrefab == null)
        {
            Debug.LogError(
                "Configure o container e o prefab dos pratos selecionados."
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

        // Clicar no prato selecionado remove-o do cardápio.
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

    public bool IsInMenu(CookingRecipe recipe)
    {
        return recipe != null && selectedRecipes.Contains(recipe);
    }

    public IReadOnlyList<CookingRecipe> GetSelectedRecipes()
    {
        return selectedRecipes.AsReadOnly();
    }

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

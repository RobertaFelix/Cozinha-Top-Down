
using UnityEngine;
using UnityEngine.UI;

public class RecipeBookSlot : MonoBehaviour
{
    [Header("Imagem da receita")]
    [SerializeField] private Image recipeImage;

    [Header("Botão de seleção")]
    [SerializeField] private Button recipeButton;

    [Header("Cores")]
    [SerializeField]
    private Color lockedColor =
        new Color(0.25f, 0.25f, 0.25f, 1f);

    [SerializeField] private Color unlockedColor = Color.white;

    private CookingRecipe currentRecipe;
    private bool unlocked;

    private void Awake()
    {
        if (recipeButton == null)
            recipeButton = GetComponent<Button>();

        if (recipeButton == null)
            recipeButton = GetComponentInChildren<Button>(true);

        if (recipeButton != null)
            recipeButton.onClick.AddListener(OnRecipeClicked);
    }

    private void OnDestroy()
    {
        if (recipeButton != null)
            recipeButton.onClick.RemoveListener(OnRecipeClicked);
    }

    public void Setup(CookingRecipe recipe)
    {
        currentRecipe = recipe;
        unlocked = false;

        if (recipeImage == null)
        {
            Debug.LogError(
                "RecipeBookSlot: configure a Recipe Image no prefab."
            );
            return;
        }

        if (currentRecipe == null ||
            currentRecipe.result == null ||
            currentRecipe.result.itemPrefab == null)
        {
            Debug.LogWarning(
                "RecipeBookSlot: receita ou resultado inválido."
            );
            UpdateVisual();
            return;
        }

        Sprite sprite = GetSprite(currentRecipe.result.itemPrefab);

        if (sprite == null)
        {
            Debug.LogWarning(
                "RecipeBookSlot: não foi possível encontrar o sprite."
            );
            UpdateVisual();
            return;
        }

        recipeImage.sprite = sprite;
        recipeImage.preserveAspect = true;

        UpdateVisual();
    }

    private void OnRecipeClicked()
    {
        Debug.Log("CLIQUE: slot de receita pressionado.");

        if (!unlocked)
        {
            Debug.Log("BLOQUEADA: essa receita ainda não foi desbloqueada.");
            return;
        }

        if (currentRecipe == null)
        {
            Debug.LogError("ERRO: currentRecipe está nula.");
            return;
        }

        if (RestaurantMenuController.Instance == null)
        {
            Debug.LogError(
                "ERRO: RestaurantMenuController.Instance está nulo."
            );
            return;
        }

        Debug.Log("Tentando adicionar: " + currentRecipe.result.itemPrefab.name);

        bool added =
            RestaurantMenuController.Instance.TryAddRecipe(currentRecipe);

        Debug.Log("Resultado da tentativa de adicionar: " + added);
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

    public void Unlock()
    {
        unlocked = true;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (recipeImage != null)
            recipeImage.color = unlocked ? unlockedColor : lockedColor;

        // Mantém o botão clicável para mostrar a mensagem de bloqueio.
        if (recipeButton != null)
            recipeButton.interactable = true;
    }

    public CookingRecipe GetRecipe()
    {
        return currentRecipe;
    }

    public bool IsUnlocked()
    {
        return unlocked;
    }
}

using UnityEngine;
using UnityEngine.UI;

public class RecipeBookSlot : MonoBehaviour
{
    [Header("Imagem da Receita")]
    [SerializeField]
    private Image recipeImage;

    [Header("Cores")]
    [SerializeField]
    private Color lockedColor =
        new Color(0.25f, 0.25f, 0.25f, 1f);

    [SerializeField]
    private Color unlockedColor = Color.white;

    private CookingRecipe currentRecipe;
    private bool unlocked;


    public void Setup(CookingRecipe recipe)
    {
        currentRecipe = recipe;
        unlocked = false;

        if (recipeImage == null)
        {
            Debug.LogError(
                "RecipeBookSlot: Recipe Image não foi configurada no prefab."
            );

            return;
        }

        if (currentRecipe == null)
        {
            Debug.LogWarning(
                "RecipeBookSlot: A receita recebida é nula."
            );

            return;
        }

        if (currentRecipe.result == null)
        {
            Debug.LogWarning(
                "RecipeBookSlot: A receita não possui resultado."
            );

            return;
        }

        if (currentRecipe.result.itemPrefab == null)
        {
            Debug.LogWarning(
                "RecipeBookSlot: O resultado da receita não possui itemPrefab."
            );

            return;
        }


        GameObject foodPrefab =
            currentRecipe.result.itemPrefab;


        Debug.Log(
            "RecipeBookSlot: Configurando receita com itemPrefab: " +
            foodPrefab.name
        );


        Sprite sprite = GetSprite(foodPrefab);


        if (sprite == null)
        {
            Debug.LogWarning(
                "RecipeBookSlot: NÃO foi possível encontrar o Sprite de: " +
                foodPrefab.name
            );

            return;
        }


        recipeImage.sprite = sprite;

        UpdateVisual();
    }


    private Sprite GetSprite(GameObject prefab)
    {
        if (prefab == null)
            return null;


        // ==========================================
        // SPRITE RENDERER NO OBJETO PRINCIPAL
        // ==========================================

        SpriteRenderer spriteRenderer =
            prefab.GetComponent<SpriteRenderer>();


        if (spriteRenderer != null)
        {
            return spriteRenderer.sprite;
        }


        // ==========================================
        // SPRITE RENDERER EM UM FILHO
        // ==========================================

        spriteRenderer =
            prefab.GetComponentInChildren<SpriteRenderer>(
                true
            );


        if (spriteRenderer != null)
        {
            return spriteRenderer.sprite;
        }


        // ==========================================
        // IMAGE NO OBJETO PRINCIPAL
        // ==========================================

        Image image =
            prefab.GetComponent<Image>();


        if (image != null)
        {
            return image.sprite;
        }


        // ==========================================
        // IMAGE EM UM FILHO
        // ==========================================

        image =
            prefab.GetComponentInChildren<Image>(
                true
            );


        if (image != null)
        {
            return image.sprite;
        }


        return null;
    }


    public void Unlock()
    {
        unlocked = true;

        UpdateVisual();
    }


    private void UpdateVisual()
    {
        if (recipeImage == null)
            return;


        if (unlocked)
        {
            recipeImage.color = unlockedColor;
        }
        else
        {
            recipeImage.color = lockedColor;
        }
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
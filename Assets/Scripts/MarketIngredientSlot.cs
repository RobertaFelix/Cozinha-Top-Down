
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketIngredientSlot : MonoBehaviour
{
    [Header("Ingrediente")]
    public GameObject ingredientPrefab;

    [Header("Interface")]
    public Image ingredientImage;
    public TMP_Text priceText;
    public Button button;

    [Header("Preço")]
    [Min(0)]
    public int price = 10;

    private MarketUIController market;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClicked);

        UpdateDisplay();
    }

    public void Initialize(MarketUIController controller)
    {
        market = controller;
        UpdateDisplay();
    }

    private void OnClicked()
    {
        if (market == null || ingredientPrefab == null)
            return;

        market.AddToCart(ingredientPrefab, price);
    }

    private void UpdateDisplay()
    {
        if (priceText != null)
            priceText.text = price.ToString();

        if (ingredientImage != null && ingredientPrefab != null)
        {
            Image prefabImage = ingredientPrefab.GetComponent<Image>();
            SpriteRenderer spriteRenderer =
                ingredientPrefab.GetComponent<SpriteRenderer>();

            if (prefabImage != null)
                ingredientImage.sprite = prefabImage.sprite;
            else if (spriteRenderer != null)
                ingredientImage.sprite = spriteRenderer.sprite;

            ingredientImage.enabled = ingredientImage.sprite != null;
        }
    }
}

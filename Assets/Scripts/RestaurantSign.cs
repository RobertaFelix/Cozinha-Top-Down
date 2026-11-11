using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RestaurantSign : MonoBehaviour
{
    [SerializeField] private CustomerSpawner customerSpawner;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite closedSprite;

    private SpriteRenderer signRenderer;

    private void Awake()
    {
        signRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        RefreshSprite();
    }

    // Ligado ao InteractionPromptTrigger: aproximar-se e pressionar E.
    public void ToggleOpen()
    {
        if (customerSpawner == null)
        {
            Debug.LogError("RestaurantSign: configure o CustomerSpawner.", this);
            return;
        }

        customerSpawner.SetRestaurantOpen(!customerSpawner.IsRestaurantOpen);
        RefreshSprite();
    }

    private void RefreshSprite()
    {
        if (signRenderer == null)
            signRenderer = GetComponent<SpriteRenderer>();

        signRenderer.sprite = customerSpawner != null && customerSpawner.IsRestaurantOpen
            ? openSprite
            : closedSprite;
    }
}

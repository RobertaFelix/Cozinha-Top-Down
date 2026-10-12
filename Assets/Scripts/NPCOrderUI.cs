using UnityEngine;

public class NPCOrderUI : MonoBehaviour
{
    [Header("Ícone do pedido")]
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("Fundo decorativo")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Header("Animação")]
    [SerializeField] private float floatHeight = 0.04f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 initialPosition;
    private bool isVisible;

    private void Awake()
    {
        initialPosition = transform.localPosition;

        if (iconRenderer != null)
            iconRenderer.enabled = false;

        if (backgroundRenderer != null)
            backgroundRenderer.enabled = false;
    }

    private void Update()
    {
        if (!isVisible)
            return;

        float offset =
            Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        transform.localPosition =
            initialPosition + Vector3.up * offset;
    }

    public void Show(Sprite sprite)
    {
        if (iconRenderer == null)
        {
            Debug.LogError(
                "NPCOrderUI: Icon Renderer não foi configurado.",
                this
            );
            return;
        }

        if (sprite == null)
        {
            Debug.LogError(
                "NPCOrderUI: o prato não possui Sprite.",
                this
            );
            return;
        }

        iconRenderer.sprite = sprite;
        iconRenderer.enabled = true;

        if (backgroundRenderer != null)
            backgroundRenderer.enabled = true;

        isVisible = true;
    }

    public void Hide()
    {
        isVisible = false;

        if (iconRenderer != null)
            iconRenderer.enabled = false;

        if (backgroundRenderer != null)
            backgroundRenderer.enabled = false;

        transform.localPosition = initialPosition;
    }
}
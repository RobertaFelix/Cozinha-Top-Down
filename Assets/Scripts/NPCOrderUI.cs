
using UnityEngine;
using UnityEngine.UI;

public class NPCOrderUI : MonoBehaviour
{
    [Header("Ícone do pedido")]
    [SerializeField] private Image iconImage;

    [Header("Moldura da barra")]
    [SerializeField] private Image backgroundImage;

    [Header("Preenchimento vermelho")]
    [SerializeField] private Image patienceBar;

    [Header("Posição da interface")]
    [SerializeField] private Vector2 positionOffset = new Vector2(0f, 0.5f);

    [Header("Animação")]
    [SerializeField] private float floatHeight = 0.04f;
    [SerializeField] private float floatSpeed = 2f;

    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private bool isVisible;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogError(
                "NPCOrderUI precisa estar em um objeto com RectTransform.",
                this
            );
            return;
        }

        initialPosition = positionOffset;
        rectTransform.localPosition = new Vector3(
            initialPosition.x,
            initialPosition.y,
            rectTransform.localPosition.z
        );

        Hide();
    }

    private void Update()
    {
        if (!isVisible || rectTransform == null)
            return;

        float offset =
            Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        rectTransform.localPosition = new Vector3(
            initialPosition.x,
            initialPosition.y + offset,
            rectTransform.localPosition.z
        );
    }

    public void Show(Sprite sprite)
    {
        if (rectTransform == null)
            return;

        if (iconImage == null || sprite == null)
        {
            Debug.LogError(
                "NPCOrderUI: configure o Icon Image e o Sprite do prato.",
                this
            );
            return;
        }

        iconImage.sprite = sprite;
        iconImage.enabled = true;

        if (backgroundImage != null)
            backgroundImage.enabled = true;

        if (patienceBar != null)
        {
            patienceBar.fillAmount = 0f;
            patienceBar.enabled = true;
        }

        isVisible = true;

        SetPatience(0f);
    }

    public void SetPatience(float progress)
    {
        if (patienceBar == null)
            return;

        patienceBar.fillAmount = Mathf.Clamp01(progress);
    }

    public void Hide()
    {
        isVisible = false;

        if (iconImage != null)
            iconImage.enabled = false;

        if (backgroundImage != null)
            backgroundImage.enabled = false;

        if (patienceBar != null)
        {
            patienceBar.fillAmount = 0f;
            patienceBar.enabled = false;
        }

        if (rectTransform != null)
        {
            rectTransform.localPosition = new Vector3(
                initialPosition.x,
                initialPosition.y,
                rectTransform.localPosition.z
            );
        }
    }
}

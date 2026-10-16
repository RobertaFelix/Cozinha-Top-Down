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

    [Header("Animação")]
    [SerializeField] private float floatHeight = 0.04f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 initialPosition;
    private bool isVisible;

    private void Awake()
    {
        initialPosition = transform.localPosition;

        if (iconImage != null)
            iconImage.enabled = false;

        if (backgroundImage != null)
            backgroundImage.enabled = false;

        if (patienceBar != null)
        {
            patienceBar.fillAmount = 0f;
            patienceBar.enabled = false;
        }
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

        // A barra começa vazia.
        SetPatience(0f);
    }

    public void SetPatience(float progress)
    {
        if (patienceBar == null)
            return;

        // Preenche a barra de 0 a 1.
        // A direção é configurada no Inspector:
        // Filled > Horizontal > Left.
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

        transform.localPosition = initialPosition;
    }
}
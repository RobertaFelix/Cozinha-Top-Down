using UnityEngine;
using UnityEngine.UI;

public class CookingProgressUI : MonoBehaviour
{
    [Header("Elementos da interface")]
    [SerializeField] private Image greenProgress;
    [SerializeField] private Image redProgress;
    [SerializeField] private Image foodImage;

    [Header("Movimento")]
    [SerializeField] private float floatHeight = 0.1f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 initialPosition;

    private void Awake()
    {
        initialPosition = transform.localPosition;

        ValidateReferences();

        // O objeto começa escondido.
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        float offset =
            Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        transform.localPosition =
            initialPosition + Vector3.up * offset;
    }

    private void ValidateReferences()
    {
        if (greenProgress == null)
            Debug.LogError("Green Progress não foi atribuído.", this);

        if (redProgress == null)
            Debug.LogError("Red Progress não foi atribuído.", this);

        if (foodImage == null)
            Debug.LogError("Food Image não foi atribuído.", this);
    }

    public void Show(Sprite foodSprite, float progress)
    {
        // Ativa a interface inteira.
        gameObject.SetActive(true);

        // A barra verde começa com o progresso do cozimento.
        if (greenProgress != null)
        {
            greenProgress.gameObject.SetActive(true);
            greenProgress.enabled = true;
            greenProgress.type = Image.Type.Filled;
            greenProgress.fillMethod = Image.FillMethod.Horizontal;
            greenProgress.fillOrigin = (int)Image.OriginHorizontal.Left;
            greenProgress.fillAmount = Mathf.Clamp01(progress);
        }

        // A barra vermelha fica escondida até a comida ficar pronta.
        if (redProgress != null)
        {
            redProgress.type = Image.Type.Filled;
            redProgress.fillMethod = Image.FillMethod.Horizontal;
            redProgress.fillOrigin = (int)Image.OriginHorizontal.Left;
            redProgress.fillAmount = 0f;
            redProgress.enabled = false;
            redProgress.gameObject.SetActive(false);
        }

        // Mantém o sprite do prato visível durante todo o cozimento.
        if (foodImage != null)
        {
            foodImage.sprite = foodSprite;
            foodImage.preserveAspect = true;
            foodImage.enabled = foodSprite != null;
            foodImage.gameObject.SetActive(foodSprite != null);

            // O prato deve ficar na frente das barras.
            foodImage.transform.SetAsLastSibling();
        }

        // Restaura a posição inicial a cada preparo.
        initialPosition = transform.localPosition;
    }

    public void SetProgress(float progress)
    {
        if (greenProgress == null)
            return;

        greenProgress.gameObject.SetActive(true);
        greenProgress.enabled = true;
        greenProgress.fillAmount = Mathf.Clamp01(progress);
    }

    public void SetBurnProgress(float progress)
    {
        if (redProgress == null)
            return;

        redProgress.gameObject.SetActive(true);
        redProgress.enabled = true;
        redProgress.fillAmount = Mathf.Clamp01(progress);

        // O sprite do prato continua na frente.
        if (foodImage != null && foodImage.gameObject.activeSelf)
        {
            foodImage.transform.SetAsLastSibling();
        }
    }

    public void Hide()
    {
        if (greenProgress != null)
        {
            greenProgress.fillAmount = 0f;
            greenProgress.gameObject.SetActive(false);
        }

        if (redProgress != null)
        {
            redProgress.fillAmount = 0f;
            redProgress.gameObject.SetActive(false);
        }

        if (foodImage != null)
        {
            foodImage.enabled = false;
            foodImage.gameObject.SetActive(false);
            foodImage.sprite = null;
        }

        gameObject.SetActive(false);
    }
}

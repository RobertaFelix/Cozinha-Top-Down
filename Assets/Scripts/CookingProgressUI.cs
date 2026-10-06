using UnityEngine;
using UnityEngine.UI;

public class CookingProgressUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField]
    private Image greenProgress;

    [SerializeField]
    private Image foodImage;

    [Header("Animação")]
    [SerializeField]
    private float floatHeight = 0.03f;

    [SerializeField]
    private float floatSpeed = 2f;

    private Vector3 initialPosition;

    private void Awake()
    {
        initialPosition = transform.localPosition;

        Hide();
    }

    private void Update()
    {
        if (!gameObject.activeSelf)
            return;

        float offset =
            Mathf.Sin(Time.time * floatSpeed)
            * floatHeight;

        transform.localPosition =
            initialPosition +
            new Vector3(0f, offset, 0f);
    }

    public void Show(
        Sprite foodSprite,
        float progress)
    {
        gameObject.SetActive(true);

        transform.localPosition =
            initialPosition;

        if (foodImage != null)
        {
            foodImage.sprite = foodSprite;

            foodImage.enabled =
                foodSprite != null;
        }

        SetProgress(progress);
    }

    public void SetProgress(float progress)
    {
        if (greenProgress == null)
            return;

        greenProgress.fillAmount =
            Mathf.Clamp01(progress);
    }

    public void Hide()
    {
        gameObject.SetActive(false);

        if (greenProgress != null)
        {
            greenProgress.fillAmount = 0f;
        }

        if (foodImage != null)
        {
            foodImage.enabled = false;
        }
    }
}
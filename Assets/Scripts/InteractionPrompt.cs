using UnityEngine;

public class InteractionPrompt : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject promptUI;

    [Header("Animação")]
    [SerializeField]
    private float floatHeight = 0.1f;

    [SerializeField]
    private float floatSpeed = 2f;

    private Vector3 initialPosition;

    private void Awake()
    {
        if (promptUI == null)
            return;

        initialPosition =
            promptUI.transform.localPosition;

        promptUI.SetActive(false);
    }

    private void Update()
    {
        if (promptUI == null)
            return;

        if (!promptUI.activeSelf)
            return;

        float offset =
            Mathf.Sin(
                Time.time * floatSpeed
            ) * floatHeight;

        promptUI.transform.localPosition =
            initialPosition +
            new Vector3(0f, offset, 0f);
    }

    public void Show()
    {
        if (promptUI == null)
            return;

        promptUI.SetActive(true);
    }

    public void Hide()
    {
        if (promptUI == null)
            return;

        promptUI.SetActive(false);

        promptUI.transform.localPosition =
            initialPosition;
    }
}
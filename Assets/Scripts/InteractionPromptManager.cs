using UnityEngine;

public class InteractionPromptManager : MonoBehaviour
{
    public static InteractionPromptManager Instance;

    [Header("UI Global")]
    [SerializeField]
    private GameObject promptUI;

    [Header("Posição")]
    [SerializeField]
    private Vector3 offset = new Vector3(0f, 1f, 0f);

    [Header("Animação")]
    [SerializeField]
    private float floatHeight = 0.1f;

    [SerializeField]
    private float floatSpeed = 2f;

    private Transform currentTarget;

    private Vector3 initialPosition;

    private void Awake()
    {
        Instance = this;

        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (promptUI == null)
            return;

        if (!promptUI.activeSelf)
            return;

        if (currentTarget == null)
        {
            Hide();
            return;
        }

        // ==========================================
        // POSIÇÃO BASE
        // ==========================================

        Vector3 targetPosition =
            currentTarget.position + offset;

        // ==========================================
        // ANIMAÇÃO DE FLUTUAÇÃO
        // ==========================================

        float floatOffset =
            Mathf.Sin(
                Time.time * floatSpeed
            ) * floatHeight;

        promptUI.transform.position =
            targetPosition +
            new Vector3(
                0f,
                floatOffset,
                0f
            );
    }

    public void Show(Transform target)
    {
        if (target == null)
            return;

        currentTarget = target;

        if (promptUI == null)
            return;

        promptUI.SetActive(true);
    }

    public void Hide(Transform target)
    {
        if (target == null)
            return;

        // Só esconde se o objeto que está
        // pedindo para esconder for o atual.

        if (currentTarget != target)
            return;

        Hide();
    }

    public void Hide()
    {
        currentTarget = null;

        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }
}
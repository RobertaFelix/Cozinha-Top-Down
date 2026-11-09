
using UnityEngine;

public class InteractionPromptManager : MonoBehaviour
{
    public static InteractionPromptManager Instance;

    [Header("UI Global")]
    [SerializeField] private GameObject promptUI;

    [Header("Posição")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);

    [Header("Animação")]
    [SerializeField] private float floatHeight = 0.1f;
    [SerializeField] private float floatSpeed = 2f;

    private Transform currentTarget;
    private Vector3 currentOffsetAdicional;

    private void Awake()
    {
        // Impede a existência de dois Managers.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (promptUI != null)
            promptUI.SetActive(false);

        Debug.Log("InteractionPromptManager inicializado!");
    }

    private void Update()
    {
        if (promptUI == null || !promptUI.activeSelf)
            return;

        if (currentTarget == null)
        {
            Hide();
            return;
        }

        Vector3 targetPosition =
            currentTarget.position + offset + currentOffsetAdicional;

        float floatOffset =
            Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        promptUI.transform.position =
            targetPosition + new Vector3(0f, floatOffset, 0f);
    }

    public void Show(Transform target)
    {
        Show(target, Vector3.zero);
    }

    public void Show(Transform target, Vector3 offsetAdicional)
    {
        if (target == null)
            return;

        currentTarget = target;
        currentOffsetAdicional = offsetAdicional;

        if (promptUI == null)
        {
            Debug.LogError("PromptUI não foi configurado no Manager!");
            return;
        }

        promptUI.SetActive(true);
    }

    public void Hide(Transform target)
    {
        if (target == null || currentTarget != target)
            return;

        Hide();
    }

    public void Hide()
    {
        currentTarget = null;
        currentOffsetAdicional = Vector3.zero;

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

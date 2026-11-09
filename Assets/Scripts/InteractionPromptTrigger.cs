
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractionPromptTrigger : MonoBehaviour
{
    [Header("Objeto que o E deve acompanhar")]
    [SerializeField] private Transform target;

    [Header("Ajuste adicional da posição do E")]
    [SerializeField] private Vector3 offsetAdicional = Vector3.zero;

    [Header("Ação ao pressionar E")]
    [SerializeField] private UnityEvent onInteract;

    [Header("Troca de cenas")]
    [SerializeField] private bool useSceneTransition;
    [SerializeField] private bool returnToFirstScene;

    private bool playerNearby;

    private void Awake()
    {
        if (target == null)
            target = transform;
    }

    private void Update()
    {
        if (!playerNearby)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("Tecla E pressionada em: " + gameObject.name);

            if (useSceneTransition)
            {
                SceneTransitionController controller =
                    FindAnyObjectByType<SceneTransitionController>();

                if (controller == null)
                {
                    Debug.LogError("SceneTransitionController não encontrado!");
                    return;
                }

                if (returnToFirstScene)
                    controller.ReturnToFirstScene();
                else
                    controller.LoadDungeon();

                return;
            }

            onInteract?.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        ShowPrompt();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (!playerNearby)
            ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        playerNearby = false;

        if (InteractionPromptManager.Instance != null)
            InteractionPromptManager.Instance.Hide(target);
    }

    private void ShowPrompt()
    {
        playerNearby = true;

        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.Show(
                target,
                offsetAdicional
            );
        }
    }

    private void OnDisable()
    {
        playerNearby = false;

        if (InteractionPromptManager.Instance != null)
            InteractionPromptManager.Instance.Hide(target);
    }
}


using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractionPromptTrigger : MonoBehaviour
{
    [Header("Objeto que o E deve acompanhar")]
    [SerializeField]
    private Transform target;

    [Header("Ajuste adicional da posição do E")]
    [SerializeField]
    private Vector3 offsetAdicional = Vector3.zero;

    [Header("Ação ao pressionar E")]
    [SerializeField]
    private UnityEvent onInteract;

    private bool playerNearby;

    private void Awake()
    {
        Debug.Log(
            "[InteractionPromptTrigger] Awake: " + gameObject.name
        );

        if (target == null)
        {
            target = transform;

            Debug.Log(
                "[InteractionPromptTrigger] Target não configurado. " +
                "Utilizando o próprio objeto: " + gameObject.name
            );
        }
        else
        {
            Debug.Log(
                "[InteractionPromptTrigger] Target configurado: " +
                target.name
            );
        }
    }

    private void Update()
    {
        if (!playerNearby)
            return;

        if (Keyboard.current == null)
        {
            Debug.LogWarning(
                "[InteractionPromptTrigger] Keyboard.current está nulo."
            );

            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log(
                "[InteractionPromptTrigger] E pressionado dentro do trigger: " +
                gameObject.name
            );

            if (onInteract == null)
            {
                Debug.LogWarning(
                    "[InteractionPromptTrigger] On Interact está nulo!"
                );

                return;
            }

            if (onInteract.GetPersistentEventCount() == 0)
            {
                Debug.LogWarning(
                    "[InteractionPromptTrigger] Nenhuma ação foi configurada " +
                    "no evento On Interact!"
                );
            }

            Debug.Log(
                "[InteractionPromptTrigger] Executando evento On Interact..."
            );

            onInteract.Invoke();

            Debug.Log(
                "[InteractionPromptTrigger] Evento On Interact executado."
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(
            "[InteractionPromptTrigger] OnTriggerEnter2D: " +
            collision.name
        );

        if (!collision.CompareTag("Player"))
        {
            Debug.Log(
                "[InteractionPromptTrigger] Objeto ignorado. Tag encontrada: " +
                collision.tag
            );

            return;
        }

        playerNearby = true;

        Debug.Log(
            "[InteractionPromptTrigger] Jogador entrou na área: " +
            gameObject.name
        );

        if (InteractionPromptManager.Instance == null)
        {
            Debug.LogWarning(
                "[InteractionPromptTrigger] InteractionPromptManager.Instance " +
                "não foi encontrado!"
            );

            return;
        }

        InteractionPromptManager.Instance.Show(
            target,
            offsetAdicional
        );

        Debug.Log(
            "[InteractionPromptTrigger] Aviso de interação exibido."
        );
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log(
            "[InteractionPromptTrigger] OnTriggerExit2D: " +
            collision.name
        );

        if (!collision.CompareTag("Player"))
            return;

        playerNearby = false;

        Debug.Log(
            "[InteractionPromptTrigger] Jogador saiu da área: " +
            gameObject.name
        );

        if (InteractionPromptManager.Instance == null)
        {
            Debug.LogWarning(
                "[InteractionPromptTrigger] InteractionPromptManager.Instance " +
                "não foi encontrado!"
            );

            return;
        }

        InteractionPromptManager.Instance.Hide(target);

        Debug.Log(
            "[InteractionPromptTrigger] Aviso de interação escondido."
        );
    }
}

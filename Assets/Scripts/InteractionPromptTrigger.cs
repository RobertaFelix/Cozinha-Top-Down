
using UnityEngine;

public class InteractionPromptTrigger : MonoBehaviour
{
    [Header("Objeto que o E deve acompanhar")]
    [SerializeField]
    private Transform target;

    [Header("Ajuste adicional da posição do E")]
    [SerializeField]
    private Vector3 offsetAdicional = Vector3.zero;

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }
    }

    private void OnTriggerEnter2D(
        Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (InteractionPromptManager.Instance == null)
            return;

        InteractionPromptManager.Instance.Show(
            target,
            offsetAdicional
        );
    }

    private void OnTriggerExit2D(
        Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (InteractionPromptManager.Instance == null)
            return;

        InteractionPromptManager.Instance.Hide(
            target
        );
    }
}

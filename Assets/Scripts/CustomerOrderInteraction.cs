
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomerOrderInteraction : MonoBehaviour
{
    private CustomerOrderController orderController;

    private bool playerNearby;
    private bool interactionUsed;

    private void Awake()
    {
        orderController =
            GetComponent<CustomerOrderController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (interactionUsed || orderController == null)
            return;

        if (orderController.OrderRevealed ||
            orderController.OrderCompleted ||
            orderController.OrderExpired)
        {
            return;
        }

        playerNearby = true;

        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.Show(transform);
        }
    }

    private void Update()
    {
        if (!playerNearby || interactionUsed)
            return;

        if (orderController == null)
            return;

        if (orderController.OrderRevealed ||
            orderController.OrderCompleted ||
            orderController.OrderExpired)
        {
            HidePrompt();
            interactionUsed = true;
            return;
        }

        if (Keyboard.current == null ||
            !Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        // O E só pode ser usado uma vez neste NPC.
        if (orderController.RequestOrder())
        {
            interactionUsed = true;
            playerNearby = false;
            HidePrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;
        HidePrompt();
    }

    private void HidePrompt()
    {
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.Hide(transform);
        }
    }

    private void OnDisable()
    {
        playerNearby = false;
        HidePrompt();
    }
}

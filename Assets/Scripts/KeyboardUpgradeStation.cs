
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardUpgradeStation : MonoBehaviour
{
    [Header("Interface")]
    [SerializeField] private UpgradeUIController upgradeUI;

    [Header("Interação")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private string playerTag = "Player";

    private bool playerNearby;

    private void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerNearby || upgradeUI == null)
            return;

        // Mostra o prompt somente se a interface estiver fechada.
        if (interactionPrompt != null)
            interactionPrompt.SetActive(!upgradeUI.IsOpen);

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame &&
            !upgradeUI.IsOpen)
        {
            upgradeUI.Open();

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerNearby = true;

        if (interactionPrompt != null &&
            (upgradeUI == null || !upgradeUI.IsOpen))
        {
            interactionPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerNearby = false;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        // Sair da área cancela somente a seleção pendente.
        if (upgradeUI != null && upgradeUI.IsOpen)
            upgradeUI.CancelPurchase();
    }
}

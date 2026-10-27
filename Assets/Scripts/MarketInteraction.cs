
using UnityEngine;
using UnityEngine.InputSystem;

public class MarketInteraction : MonoBehaviour
{
    [Header("Loja")]
    [SerializeField] private MarketUIController marketUI;

    private bool playerNearby;

    private void Update()
    {
        if (!playerNearby || Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (marketUI == null)
            {
                Debug.LogError(
                    "MarketUIController não foi configurado!"
                );
                return;
            }

            if (marketUI.IsOpen)
                marketUI.CloseMarket();
            else
                marketUI.OpenMarket();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;

        if (marketUI != null && marketUI.IsOpen)
            marketUI.CloseMarket();
    }
}

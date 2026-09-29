using UnityEngine;
using UnityEngine.InputSystem;

public class Vegetables : MonoBehaviour
{
    [SerializeField] private InventoryVegetablesController inventoryVegetablesController;

    [SerializeField] private InventoryItem[] items;

    private bool playerNearby;

    private void Update()
    {
        if (playerNearby && Keyboard.current.eKey.wasPressedThisFrame)
        {
            inventoryVegetablesController.ToggleVegetables(items);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = false;
        }
    }
}
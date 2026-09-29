using UnityEngine;
using UnityEngine.InputSystem;

public class Refrigerator : MonoBehaviour
{
    [SerializeField] private InventoryRefrigeratorController inventoryRefrigeratorController;

    [SerializeField] private InventoryItem[] items;

    private bool playerNearby;

    private void Update()
    {
        if (playerNearby && Keyboard.current.eKey.wasPressedThisFrame)
        {
            inventoryRefrigeratorController.OpenRefrigerator(items);
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
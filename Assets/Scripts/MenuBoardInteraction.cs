
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuBoardInteraction : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private MenuController menuController;

    private bool playerInRange;

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (menuController == null)
            {
                Debug.LogError(
                    "MenuBoardInteraction: configure o MenuController."
                );
                return;
            }

            menuController.OpenRestaurantMenu();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            playerInRange = false;
    }
}

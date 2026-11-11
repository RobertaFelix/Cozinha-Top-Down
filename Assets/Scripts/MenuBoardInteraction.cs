
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuBoardInteraction : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private MenuController menuController;

    private bool playerInRange;

    private void Start()
    {
        FindMenuController();
    }

    // ==========================================
    // RECUPERAR MENU CONTROLLER
    // ==========================================

    private void FindMenuController()
    {
        GameObject persistentUI =
            GameObject.Find("PersistentUI");

        if (persistentUI == null)
        {
            Debug.LogError(
                "MenuBoardInteraction: PersistentUI não encontrado!"
            );

            menuController = null;
            return;
        }

        menuController =
            persistentUI.GetComponentInChildren<MenuController>(true);

        if (menuController != null)
        {
            Debug.Log(
                "MenuBoardInteraction: MenuController recuperado com sucesso!"
            );
        }
        else
        {
            Debug.LogError(
                "MenuBoardInteraction: MenuController não encontrado dentro de PersistentUI!"
            );
        }
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            // Recupera novamente caso a referência tenha sido perdida.
            if (menuController == null)
                FindMenuController();

            if (menuController == null)
            {
                Debug.LogError(
                    "MenuBoardInteraction: não foi possível encontrar o MenuController."
                );

                return;
            }

            menuController.OpenRestaurantMenu(GetComponent<Collider2D>());
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

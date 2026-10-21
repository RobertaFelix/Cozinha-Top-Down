using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [Header("Menu principal")]
    public GameObject menuCanvas;

    [Header("Páginas existentes")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject recipePanel;

    [Header("Painel do cardápio")]
    [SerializeField] private GameObject restaurantMenuPanel;

    [Header("Hotbar")]
    [SerializeField] private GameObject hotbarPanel;

    private void Start()
    {
        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        if (restaurantMenuPanel != null)
            restaurantMenuPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (menuCanvas == null)
                return;

            if (menuCanvas.activeSelf)
            {
                menuCanvas.SetActive(false);

                if (restaurantMenuPanel != null)
                    restaurantMenuPanel.SetActive(false);

                if (hotbarPanel != null)
                    hotbarPanel.SetActive(true);
            }
            else
            {
                menuCanvas.SetActive(true);

                if (inventoryPanel != null)
                    inventoryPanel.SetActive(true);

                if (recipePanel != null)
                    recipePanel.SetActive(false);

                if (restaurantMenuPanel != null)
                    restaurantMenuPanel.SetActive(false);

                if (hotbarPanel != null)
                    hotbarPanel.SetActive(true);
            }
        }
    }

    public void OpenRestaurantMenu()
    {
        if (menuCanvas == null)
        {
            Debug.LogError("MenuController: menuCanvas não foi configurado.");
            return;
        }

        menuCanvas.SetActive(true);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (recipePanel != null)
            recipePanel.SetActive(true);

        if (restaurantMenuPanel != null)
            restaurantMenuPanel.SetActive(true);

        if (hotbarPanel != null)
            hotbarPanel.SetActive(false);
    }
}
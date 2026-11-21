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
        if (Time.timeScale <= 0 || RestaurantFurnitureEditor.ClosedThisFrame) return;
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (InventoryPanelSession.CloseCurrent()) return;
            if (menuCanvas == null)
                return;

            if (menuCanvas.activeSelf)
            {
                CloseMenu();
            }
            else
            {
                InventoryPanelSession.Bind(menuCanvas, null);
                InventoryPanelSession.Open(menuCanvas, CloseMenu);
                menuCanvas.SetActive(true);

                SelectPage(inventoryPanel);

                if (restaurantMenuPanel != null)
                    restaurantMenuPanel.SetActive(false);

                if (hotbarPanel != null)
                    InventoryWorkspace.SetShopHotbarVisible(true);
            }
        }
    }

    private void SelectPage(GameObject page)
    {
        var tabs = menuCanvas.GetComponentInChildren<TabController>(true);
        int index = tabs ? System.Array.IndexOf(tabs.pages, page) : -1;
        if (index >= 0)
        {
            tabs.ActivateTab(index);
            return;
        }
        if (inventoryPanel) inventoryPanel.SetActive(page == inventoryPanel);
        if (recipePanel) recipePanel.SetActive(page == recipePanel);
    }

    public void CloseMenu()
    {
        InventoryPanelSession.Closed(menuCanvas);
        InventoryWorkspace.RestoreHud();
        if (menuCanvas != null)
        {
            var tabs = menuCanvas.GetComponentInChildren<TabController>(true);
            if (tabs && tabs.FurniturePage) tabs.FurniturePage.gameObject.SetActive(false);
            menuCanvas.SetActive(false);
        }
        if (restaurantMenuPanel != null) restaurantMenuPanel.SetActive(false);
        if (hotbarPanel != null) InventoryWorkspace.SetShopHotbarVisible(true);
    }

    public void OpenRestaurantMenu() { OpenRestaurantMenu(null); }

    public void OpenRestaurantMenu(Collider2D interactionArea)
    {
        if (menuCanvas == null)
        {
            Debug.LogError("MenuController: menuCanvas não foi configurado.");
            return;
        }

        InventoryPanelSession.Bind(menuCanvas, interactionArea);
        InventoryPanelSession.Open(menuCanvas, CloseMenu);
        menuCanvas.SetActive(true);

        SelectPage(recipePanel);

        if (restaurantMenuPanel != null)
            restaurantMenuPanel.SetActive(true);

        if (hotbarPanel != null)
            InventoryWorkspace.SetShopHotbarVisible(false);
    }
}
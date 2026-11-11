using UnityEngine;

public class InventoryVegetablesController : MonoBehaviour
{
    public GameObject vegetablesPanel;

    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    // Abre ou fecha os vegetais
    public void ToggleVegetables(InventoryItem[] items)
    {
        if (vegetablesPanel.activeSelf)
        {
            CloseVegetables();
        }
        else
        {
            OpenVegetables(items);
        }
    }

    // Abre os vegetais
    public void OpenVegetables(InventoryItem[] items)
    {
        // Garante que não existam slots antigos
        if (vegetablesPanel == null) return;
        InventoryPanelSession.Open(vegetablesPanel, CloseVegetables);
        if (vegetablesPanel.GetComponentsInChildren<Slot>(true).Length > 0)
        {
            vegetablesPanel.SetActive(true);
            return;
        }

        vegetablesPanel.SetActive(true);

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            if (items[i].itemPrefab == null)
                continue;

            Slot slot = Instantiate(
                slotPrefab,
                vegetablesPanel.transform
            ).GetComponent<Slot>();

            // Cria o item visual
            GameObject item = Instantiate(
                items[i].itemPrefab,
                slot.transform
            );

            RectTransform rect =
                item.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchoredPosition =
                    Vector2.zero;
            }

            // Guarda o objeto visual
            slot.currentItem = item;

            // Guarda os dados do item
            slot.inventoryItem =
                new InventoryItem();

            slot.inventoryItem.itemPrefab =
                items[i].itemPrefab;

            slot.inventoryItem.amount =
                items[i].amount;

            slot.UpdateQuantityDisplay();
        }
    }

    // Fecha os vegetais
    public void CloseVegetables()
    {
        InventoryPanelSession.Closed(vegetablesPanel);

        vegetablesPanel.SetActive(false);
    }

    // Remove os slots criados para os vegetais
    private void ClearVegetablesSlots()
    {
        Slot[] slots =
            vegetablesPanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
    }
}
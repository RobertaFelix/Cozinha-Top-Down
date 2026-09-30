using UnityEngine;

public class InventoryRefrigeratorController : MonoBehaviour
{
    public GameObject refrigeratorPanel;

    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    // Abre ou fecha a geladeira
    public void ToggleRefrigerator(
        InventoryItem[] items)
    {
        if (refrigeratorPanel.activeSelf)
        {
            CloseRefrigerator();
        }
        else
        {
            OpenRefrigerator(items);
        }
    }

    // Abre a geladeira
    public void OpenRefrigerator(
        InventoryItem[] items)
    {
        ClearRefrigeratorSlots();

        refrigeratorPanel.SetActive(true);

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            if (items[i].itemPrefab == null)
                continue;

            Slot slot = Instantiate(
                slotPrefab,
                refrigeratorPanel.transform
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
        }
    }

    // Fecha a geladeira
    public void CloseRefrigerator()
    {
        ClearRefrigeratorSlots();

        refrigeratorPanel.SetActive(false);
    }

    // Remove os slots que foram criados para a geladeira
    private void ClearRefrigeratorSlots()
    {
        Slot[] slots =
            refrigeratorPanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
    }
}
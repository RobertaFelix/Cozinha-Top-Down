using UnityEngine;

public class InventoryFrizerController : MonoBehaviour
{
    public GameObject frizerPanel;

    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    // Abre ou fecha o freezer
    public void ToggleFrizer(InventoryItem[] items)
    {
        if (frizerPanel.activeSelf)
        {
            CloseFrizer();
        }
        else
        {
            OpenFrizer(items);
        }
    }

    // Abre o freezer
    public void OpenFrizer(InventoryItem[] items)
    {
        // Garante que não existam slots antigos
        ClearFrizerSlots();

        frizerPanel.SetActive(true);

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            if (items[i].itemPrefab == null)
                continue;

            Slot slot = Instantiate(
                slotPrefab,
                frizerPanel.transform
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

    // Fecha o freezer
    public void CloseFrizer()
    {
        ClearFrizerSlots();

        frizerPanel.SetActive(false);
    }

    // Remove os slots criados para o freezer
    private void ClearFrizerSlots()
    {
        Slot[] slots =
            frizerPanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
    }
}
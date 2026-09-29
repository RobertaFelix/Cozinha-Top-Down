using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryVegetablesController : MonoBehaviour
{
    public GameObject vegetablesPanel;

    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    // Abre ou fecha a geladeira
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

    // Abre a geladeira
    public void OpenVegetables(InventoryItem[] items)
    {
        // Garante que não existam slots antigos
        ClearVegetablesSlots();

        vegetablesPanel.SetActive(true);

        for (int i = 0; i < items.Length; i++)
        {
            Slot slot = Instantiate(
                slotPrefab,
                vegetablesPanel.transform
            ).GetComponent<Slot>();

            GameObject item = Instantiate(
                items[i].itemPrefab,
                slot.transform
            );

            item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            slot.currentItem = item;
        }
    }

    // Fecha a geladeira
    public void CloseVegetables()
    {
        ClearVegetablesSlots();

        vegetablesPanel.SetActive(false);
    }

    // Remove os slots que foram criados para a geladeira
    private void ClearVegetablesSlots()
    {
        Slot[] slots = vegetablesPanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
    }

}

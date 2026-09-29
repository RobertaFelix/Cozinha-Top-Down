using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryRefrigeratorController : MonoBehaviour
{
    public GameObject refrigeratorPanel;

    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    // Abre ou fecha a geladeira
    public void ToggleRefrigerator(InventoryItem[] items)
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
    public void OpenRefrigerator(InventoryItem[] items)
    {
        // Garante que não existam slots antigos
        ClearRefrigeratorSlots();

        refrigeratorPanel.SetActive(true);

        for (int i = 0; i < items.Length; i++)
        {
            Slot slot = Instantiate(
                slotPrefab,
                refrigeratorPanel.transform
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
    public void CloseRefrigerator()
    {
        ClearRefrigeratorSlots();

        refrigeratorPanel.SetActive(false);
    }

    // Remove os slots que foram criados para a geladeira
    private void ClearRefrigeratorSlots()
    {
        Slot[] slots = refrigeratorPanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
    }

}

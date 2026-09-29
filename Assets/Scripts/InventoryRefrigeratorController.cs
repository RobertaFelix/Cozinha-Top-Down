using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryRefrigeratorController : MonoBehaviour
{
    public GameObject refrigeratorPanel;

    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    public void OpenRefrigerator(InventoryItem[] items)
    {
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


    public void CloseRefrigerator()
    {
        refrigeratorPanel.SetActive(false);
    }

}

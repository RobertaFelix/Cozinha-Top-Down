using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryFrizerController : MonoBehaviour
{
    public GameObject frizerPanel;

    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    // Abre ou fecha a geladeira
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

    // Abre a geladeira
    public void OpenFrizer(InventoryItem[] items)
    {
        // Garante que não existam slots antigos
        ClearFrizerSlots();

        frizerPanel.SetActive(true);

        for (int i = 0; i < items.Length; i++)
        {
            Slot slot = Instantiate(
                slotPrefab,
                frizerPanel.transform
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
    public void CloseFrizer()
    {
        ClearFrizerSlots();

        frizerPanel.SetActive(false);
    }

    // Remove os slots que foram criados para a geladeira
    private void ClearFrizerSlots()
    {
        Slot[] slots = frizerPanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
    }

}

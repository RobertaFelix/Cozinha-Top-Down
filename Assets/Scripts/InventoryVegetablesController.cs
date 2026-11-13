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
        InventoryWorkspace.OpenStorage(vegetablesPanel);
        if (vegetablesPanel.GetComponentsInChildren<Slot>(true).Length > 0)
        {
            vegetablesPanel.SetActive(true);
            return;
        }

        vegetablesPanel.SetActive(true);

        int count = Mathf.Max(slotCount, items?.Length ?? 0);
        for (int i = 0; i < count; i++)
            InventoryStacking.CreateSlot(slotPrefab, vegetablesPanel.transform, items != null && i < items.Length ? items[i] : null);
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
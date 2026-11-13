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
        if (refrigeratorPanel == null) return;
        InventoryPanelSession.Open(refrigeratorPanel, CloseRefrigerator);
        InventoryWorkspace.OpenStorage(refrigeratorPanel);
        if (refrigeratorPanel.GetComponentsInChildren<Slot>(true).Length > 0)
        {
            refrigeratorPanel.SetActive(true);
            return;
        }

        refrigeratorPanel.SetActive(true);

        int count = Mathf.Max(slotCount, items?.Length ?? 0);
        for (int i = 0; i < count; i++)
            InventoryStacking.CreateSlot(slotPrefab, refrigeratorPanel.transform, items != null && i < items.Length ? items[i] : null);
    }

    // Fecha a geladeira
    public void CloseRefrigerator()
    {
        InventoryPanelSession.Closed(refrigeratorPanel);

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
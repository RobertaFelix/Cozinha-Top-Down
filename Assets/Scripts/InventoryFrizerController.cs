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
        if (frizerPanel == null) return;
        InventoryPanelSession.Open(frizerPanel, CloseFrizer);
        InventoryWorkspace.OpenStorage(frizerPanel);
        if (frizerPanel.GetComponentsInChildren<Slot>(true).Length > 0)
        {
            frizerPanel.SetActive(true);
            return;
        }

        frizerPanel.SetActive(true);

        int count = Mathf.Max(slotCount, items?.Length ?? 0);
        for (int i = 0; i < count; i++)
            InventoryStacking.CreateSlot(slotPrefab, frizerPanel.transform, items != null && i < items.Length ? items[i] : null);
    }

    // Fecha o freezer
    public void CloseFrizer()
    {
        InventoryPanelSession.Closed(frizerPanel);

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
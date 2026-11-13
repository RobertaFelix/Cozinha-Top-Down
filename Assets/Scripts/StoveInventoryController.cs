using System.Linq;
using UnityEngine;

public class StoveInventoryController : ConfirmableInventoryPanel
{
    public GameObject stovePanel => panelRoot;
    public void CloseStoveInventory() => Close();

    public InventoryItem[] GetStoveItems() => stovePanel.GetComponentsInChildren<Slot>(true)
        .Select(slot => slot.inventoryItem).Where(item => item?.itemPrefab != null).ToArray();
}

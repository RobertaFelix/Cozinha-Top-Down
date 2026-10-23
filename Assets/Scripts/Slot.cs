
using UnityEngine;
using TMPro;

public class Slot : MonoBehaviour
{
    public GameObject currentItem;
    public InventoryItem inventoryItem;

    [Header("Quantidade do item")]
    [SerializeField] private TextMeshProUGUI amountText;

    public void UpdateQuantityDisplay()
    {
        if (amountText == null)
            return;

        if (inventoryItem == null ||
            inventoryItem.itemPrefab == null)
        {
            amountText.text = "";
            return;
        }

        amountText.text = "" + Mathf.Max(1, inventoryItem.amount);
        amountText.transform.SetAsLastSibling();
    }
}

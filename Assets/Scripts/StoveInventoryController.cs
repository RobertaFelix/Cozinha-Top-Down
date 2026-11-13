using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StoveInventoryController : MonoBehaviour
{
    public GameObject stovePanel;
    public GameObject slotPrefab;
    public int slotCount = 6;
    [SerializeField] private RectTransform backpack;
    [SerializeField] private Sprite confirmIcon;
    [SerializeField] private Sprite cancelIcon;
    private Button confirmButton;
    public bool IsOpen => stovePanel && stovePanel.activeInHierarchy;

    public void EnsureSlots()
    {
        if (stovePanel.GetComponentInChildren<Slot>(true)) return;
        for (int i = 0; i < slotCount; i++)
            InventoryStacking.CreateSlot(slotPrefab, stovePanel.transform);
    }

    public void Open(Action cook)
    {
        if (IsOpen) return;
        if (!backpack)
            backpack = FindAnyObjectByType<InventoryController>().inventoryPanel.GetComponent<RectTransform>();
        InventoryPanelSession.Open(stovePanel, CloseStoveInventory);
        EnsureSlots();
        InventoryWorkspace.OpenStorage(stovePanel);
        stovePanel.SetActive(true);
        stovePanel.transform.SetAsLastSibling();
        var panel = (RectTransform)stovePanel.transform;
        if (!confirmButton)
        {
            confirmButton = CreateButton(panel, "Confirm", confirmIcon, -78);
            var cancel = CreateButton(panel, "Cancel", cancelIcon, 78);
            cancel.onClick.AddListener(CloseStoveInventory);
        }
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() => cook());
    }

    private static Button CreateButton(RectTransform panel, string name, Sprite sprite, float x)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(panel, false);
        obj.GetComponent<LayoutElement>().ignoreLayout = true;
        var rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
        rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(x, -8);
        rect.sizeDelta = new Vector2(120, 120);
        var icon = obj.GetComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        return obj.GetComponent<Button>();
    }

    public void CloseStoveInventory()
    {
        InventoryPanelSession.Closed(stovePanel);
        stovePanel.SetActive(false);
    }

    public InventoryItem[] GetStoveItems() => stovePanel.GetComponentsInChildren<Slot>(true)
        .Select(slot => slot.inventoryItem).Where(item => item?.itemPrefab != null).ToArray();
}

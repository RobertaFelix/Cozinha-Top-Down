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
    private Transform previousParent;
    private int previousIndex;
    private Vector2 previousPosition;
    private bool previouslyActive;
    public bool IsOpen => stovePanel && stovePanel.activeInHierarchy;

    public void EnsureSlots()
    {
        if (stovePanel.GetComponentInChildren<Slot>(true)) return;
        for (int i = 0; i < slotCount; i++)
        {
            var slot = Instantiate(slotPrefab, stovePanel.transform).GetComponent<Slot>();
            slot.inventoryItem = null;
            slot.UpdateQuantityDisplay();
        }
    }

    public void Open(Action cook)
    {
        if (IsOpen) return;
        if (!backpack)
            backpack = FindAnyObjectByType<InventoryController>().inventoryPanel.GetComponent<RectTransform>();
        InventoryPanelSession.Open(stovePanel, CloseStoveInventory);
        EnsureSlots();
        previousParent = backpack.parent;
        previousIndex = backpack.GetSiblingIndex();
        previousPosition = backpack.anchoredPosition;
        previouslyActive = backpack.gameObject.activeSelf;
        backpack.SetParent(stovePanel.transform.parent, false);
        backpack.anchoredPosition = new Vector2(-465, 0);
        backpack.gameObject.SetActive(true);
        stovePanel.SetActive(true);
        stovePanel.transform.SetAsLastSibling();
        var panel = (RectTransform)stovePanel.transform;
        panel.anchoredPosition = new Vector2(465, 0);
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
        if (previousParent)
        {
            backpack.SetParent(previousParent, false);
            backpack.SetSiblingIndex(previousIndex);
            backpack.anchoredPosition = previousPosition;
            backpack.gameObject.SetActive(previouslyActive);
            previousParent = null;
        }
        stovePanel.SetActive(false);
    }

    public InventoryItem[] GetStoveItems() => stovePanel.GetComponentsInChildren<Slot>(true)
        .Select(slot => slot.inventoryItem).Where(item => item?.itemPrefab != null).ToArray();
}

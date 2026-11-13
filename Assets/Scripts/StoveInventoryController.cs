using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoveInventoryController : MonoBehaviour
{
    public GameObject stovePanel;
    public GameObject slotPrefab;
    public int slotCount = 6;
    [SerializeField] private RectTransform backpack;
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
        var button = panel.GetComponentInChildren<Button>(true);
        if (!button)
        {
            var obj = new GameObject("Cook", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            obj.transform.SetParent(panel, false);
            obj.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)obj.transform;
            rect.anchoredPosition = new Vector2(0, -145);
            rect.sizeDelta = new Vector2(200, 72);
            obj.GetComponent<Image>().color = new Color(.4f,.23f,.13f);
            button = obj.GetComponent<Button>();
            var label = new GameObject("Label", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(rect, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            label.text = "Cook";
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => cook());
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

using System;
using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmableInventoryPanel : MonoBehaviour
{
    [FormerlySerializedAs("stovePanel")] public GameObject panelRoot;
    public GameObject slotPrefab;
    public int slotCount = 6;
    [SerializeField] private RectTransform backpack;
    [SerializeField] private Sprite confirmIcon;
    [SerializeField] private Sprite cancelIcon;
    private Button confirmButton;
    private Action onClosed;
    public bool IsOpen => panelRoot && panelRoot.activeInHierarchy;

    public void EnsureSlots()
    {
        int existing = panelRoot.GetComponentsInChildren<Slot>(true).Length;
        for (int i = existing; i < slotCount; i++)
            InventoryStacking.CreateSlot(slotPrefab, panelRoot.transform);
    }

    public void Open(Action confirm, Action closed = null)
    {
        if (IsOpen) return;
        if (!backpack)
            backpack = FindAnyObjectByType<InventoryController>().inventoryPanel.GetComponent<RectTransform>();
        InventoryPanelSession.Open(panelRoot, Close);
        onClosed = closed;
        EnsureSlots();
        InventoryWorkspace.OpenStorage(panelRoot);
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        var panel = (RectTransform)panelRoot.transform;
        if (!confirmButton)
        {
            confirmButton = CreateButton(panel, "Confirm", confirmIcon, -78);
            var cancel = CreateButton(panel, "Cancel", cancelIcon, 78);
            cancel.onClick.AddListener(Close);
        }
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() => confirm());
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

    public void Close()
    {
        InventoryPanelSession.Closed(panelRoot);
        panelRoot.SetActive(false);
        var callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

}

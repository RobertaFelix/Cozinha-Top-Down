using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ItemTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Func<ItemTooltipContent> provider;
    public bool ShowInventorySelection { get; private set; } = true;
    public RectTransform TooltipAnchor { get; private set; }

    public static void Bind(GameObject target, Func<ItemTooltipContent> content, bool showInventorySelection = true, RectTransform tooltipAnchor = null)
    {
        var component = target.GetComponent<ItemTooltipTarget>();
        if (component == null) component = target.AddComponent<ItemTooltipTarget>();
        component.provider = content;
        component.ShowInventorySelection = showInventorySelection;
        component.TooltipAnchor = tooltipAnchor;
        InventoryHoverSelection.EnsureRunning();
    }

    public ItemTooltipContent GetContent() => provider != null ? provider() : default;

    public void OnPointerEnter(PointerEventData eventData)
    {
        InventoryHoverSelection.PointerEntered(this);
        if (!ItemTooltipView.IsDragging)
            ItemTooltipView.Show(this, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData) => ItemTooltipView.Hide(this);
    private void OnDisable() => ItemTooltipView.Hide(this);
    private void OnDestroy() { ItemTooltipView.Hide(this); provider = null; }
}

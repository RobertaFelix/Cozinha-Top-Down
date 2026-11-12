using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ItemTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Func<ItemTooltipContent> provider;

    public static void Bind(GameObject target, Func<ItemTooltipContent> content)
    {
        var component = target.GetComponent<ItemTooltipTarget>();
        if (component == null) component = target.AddComponent<ItemTooltipTarget>();
        component.provider = content;
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

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CombatInputGate
{
    private static readonly List<RaycastResult> hits = new List<RaycastResult>();
    public static bool IsBlocked()
    {
        if (Time.timeScale <= 0 || InventoryPanelSession.HasOpenPanel || InventoryWorkspace.HotbarInBackpack ||
            !InventoryWorkspace.HotbarAcceptsInput || ItemTooltipView.IsDragging || InventoryStackCursor.IsHolding) return true;
        var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        return selected && (selected.GetComponentInParent<TMP_InputField>() || selected.GetComponentInParent<InputField>());
    }
    public static bool PointerOverUI(Vector2 position)
    {
        if (!EventSystem.current) return false;
        var pointer = new PointerEventData(EventSystem.current) { position = position };
        hits.Clear();
        EventSystem.current.RaycastAll(pointer, hits);
        foreach (var hit in hits) if (hit.module is GraphicRaycaster) return true;
        return false;
    }
}

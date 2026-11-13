using UnityEngine;
using UnityEngine.UI;

public sealed class HotbarEquippedIndicator : MonoBehaviour
{
    private RectTransform underline;
    private HotbarController hotbar;
    private void LateUpdate()
    {
        if (hotbar == null) hotbar = GetComponent<HotbarController>();
        var slot = hotbar != null ? hotbar.SelectedSlot : null;
        if (!slot || !slot.gameObject.activeInHierarchy) return;
        if (!underline)
        {
            underline = new GameObject("HandUnderline", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<RectTransform>();
            underline.GetComponent<LayoutElement>().ignoreLayout = true;
            var image = underline.GetComponent<Image>();
            image.color = new Color32(170, 121, 89, 255);
            image.raycastTarget = false;
            underline.anchorMin = underline.anchorMax = new Vector2(.5f, 0f);
            underline.pivot = new Vector2(.5f, 1f);
        }
        if (underline.parent != slot.transform) underline.SetParent(slot.transform, false);
        float pixel = ((RectTransform)slot.transform).rect.width / 16f;
        underline.sizeDelta = new Vector2(6f * pixel, pixel);
        underline.anchoredPosition = new Vector2(0f, InventoryWorkspace.HotbarInBackpack ? pixel : -pixel);
    }

    private void OnDestroy()
    {
        if (underline) Destroy(underline.gameObject);
    }
}

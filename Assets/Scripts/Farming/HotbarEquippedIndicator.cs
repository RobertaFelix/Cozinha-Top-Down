using UnityEngine;
using UnityEngine.UI;

public sealed class HotbarEquippedIndicator : MonoBehaviour
{
    private RectTransform frame;
    private HotbarController hotbar;
    private void LateUpdate()
    {
        if (hotbar == null) hotbar = GetComponent<HotbarController>();
        var slot = hotbar != null ? hotbar.EquippedSlot : null;
        if (slot == null || !slot.gameObject.activeInHierarchy) { if (frame != null) frame.gameObject.SetActive(false); return; }
        if (frame == null && !Create()) return;
        frame.SetParent(slot.transform, false);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
        frame.anchoredPosition = Vector2.zero;frame.localScale = Vector3.one;
        var rect = (RectTransform)slot.transform;var scale = rect.lossyScale;
        float side = rect.rect.width * Mathf.Abs(scale.x) * 1.25f;
        frame.sizeDelta = new Vector2(side / Mathf.Max(.0001f, Mathf.Abs(scale.x)), side / Mathf.Max(.0001f, Mathf.Abs(scale.y)));
        frame.SetAsLastSibling();
        if (slot.QuantityLabel != null) slot.QuantityLabel.transform.SetAsLastSibling();
        frame.gameObject.SetActive(true);
    }
    private bool Create()
    {
        var style = Resources.Load<InventorySelectionStyle>("InventorySelectionStyle");
        if (style == null) return false;
        frame = new GameObject("EquippedItemFrame", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
        frame.GetComponent<LayoutElement>().ignoreLayout = true;
        Corner(style.topLeft, new Vector2(0,1));Corner(style.topRight, Vector2.one);
        Corner(style.bottomLeft, Vector2.zero);Corner(style.bottomRight, Vector2.right);
        return true;
    }
    private void Corner(Sprite sprite, Vector2 corner)
    {
        if (sprite == null) return;
        var image = new GameObject("Corner", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(frame, false);image.sprite = sprite;image.raycastTarget = false;image.color = new Color(1f,.8f,.35f,1f);
        var size = sprite.rect.size / 40f;
        image.rectTransform.anchorMin = new Vector2(corner.x == 0 ? 0 : 1-size.x, corner.y == 0 ? 0 : 1-size.y);
        image.rectTransform.anchorMax = new Vector2(corner.x == 0 ? size.x : 1, corner.y == 0 ? size.y : 1);
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
    }
    private void OnDestroy() { if (frame != null) Destroy(frame.gameObject); }
}

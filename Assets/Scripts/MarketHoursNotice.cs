using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Short non-interactive feedback shared by the entrance and both shop counters.
public sealed class MarketHoursNotice : MonoBehaviour
{
    private static MarketHoursNotice instance;
    private TMP_Text label;
    private float hideAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { instance = null; }

    public static void Show(string message)
    {
        if (instance == null)
        {
            var root = new GameObject("MarketHoursNotice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(root);
            instance = root.AddComponent<MarketHoursNotice>();
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var panel = new GameObject("Notice", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .2f);
            rect.sizeDelta = new Vector2(680, 100);
            var image = panel.GetComponent<Image>();
            image.color = new Color32(242, 218, 170, 245);
            image.raycastTarget = false;
            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(panel.transform, false);
            instance.label = text.GetComponent<TextMeshProUGUI>();
            instance.label.rectTransform.anchorMin = Vector2.zero;
            instance.label.rectTransform.anchorMax = Vector2.one;
            instance.label.rectTransform.offsetMin = new Vector2(15, 10);
            instance.label.rectTransform.offsetMax = new Vector2(-15, -10);
            instance.label.font = TMP_Settings.defaultFontAsset;
            instance.label.fontSize = 28;
            instance.label.alignment = TextAlignmentOptions.Center;
            instance.label.color = new Color32(67, 43, 28, 255);
            instance.label.raycastTarget = false;
        }
        instance.gameObject.SetActive(true);
        instance.label.text = message;
        instance.hideAt = Time.unscaledTime + 3f;
    }

    private void Update() { if (Time.unscaledTime >= hideAt) gameObject.SetActive(false); }
}

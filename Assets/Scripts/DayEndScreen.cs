using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DayEndScreen : MonoBehaviour
{
    private GameObject screen;
    private TMP_Text title;
    private TMP_Text status;
    private TMP_Text buttonText;
    private Button continueButton;
    private RectTransform summaryContent;
    private Action continueAction;
    public bool IsOpen => screen != null && screen.activeSelf;
    // Future daily profit/XP widgets can be placed here without changing sleep/save logic.
    public RectTransform SummaryContent { get { EnsureCreated(); return summaryContent; } }

    private TMP_FontAsset Font
    {
        get
        {
            foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (font.name.Contains("Jersey10")) return font;
            return TMP_Settings.defaultFontAsset;
        }
    }

    public void ShowSummary(int day, Action onContinue)
    {
        EnsureCreated();
        title.text = "Day " + day + " complete";
        status.text = "Your game has been saved.";
        buttonText.text = "Continue";
        continueAction = onContinue;
        screen.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(continueButton.gameObject);
    }

    public void ShowError(string message, Action retry)
    {
        EnsureCreated();
        title.text = "Save unavailable";
        status.text = message;
        buttonText.text = "Retry";
        continueAction = retry;
        screen.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(continueButton.gameObject);
    }

    public void Hide()
    {
        if (screen != null) screen.SetActive(false);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == continueButton?.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void EnsureCreated()
    {
        if (screen != null) return;
        screen = new GameObject("DayEndScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        screen.transform.SetParent(transform, false);
        var canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32600;
        var scaler = screen.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        var backdrop = Image("Backdrop", screen.transform, new Color(0, 0, 0, .7f));
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
        var panel = Image("SummaryPanel", backdrop.transform, new Color32(242, 218, 170, 255));
        panel.rectTransform.sizeDelta = new Vector2(760, 500);
        panel.gameObject.AddComponent<Outline>().effectColor = new Color32(100, 64, 42, 255);
        title = Text("Title", panel.transform, 50, new Vector2(0, 170), new Vector2(680, 70));
        status = Text("Status", panel.transform, 32, new Vector2(0, 100), new Vector2(680, 80));

        var content = new GameObject("SummaryContent", typeof(RectTransform));
        summaryContent = content.GetComponent<RectTransform>();
        summaryContent.SetParent(panel.transform, false);
        summaryContent.sizeDelta = new Vector2(660, 160);
        summaryContent.anchoredPosition = new Vector2(0, -30);

        var buttonImage = Image("ContinueButton", panel.transform, new Color32(167, 112, 62, 255));
        buttonImage.rectTransform.sizeDelta = new Vector2(320, 70);
        buttonImage.rectTransform.anchoredPosition = new Vector2(0, -180);
        continueButton = buttonImage.gameObject.AddComponent<Button>();
        continueButton.targetGraphic = buttonImage;
        continueButton.onClick.AddListener(() => continueAction?.Invoke());
        buttonText = Text("Label", buttonImage.transform, 38, Vector2.zero, new Vector2(300, 70));
        screen.SetActive(false);
    }

    private static Image Image(string name, Transform parent, Color color)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(parent, false);
        var image = root.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TMP_Text Text(string name, Transform parent, int size, Vector2 position, Vector2 dimensions)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        root.transform.SetParent(parent, false);
        var label = root.GetComponent<TextMeshProUGUI>();
        label.font = Font;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(67, 43, 28, 255);
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = dimensions;
        label.rectTransform.anchoredPosition = position;
        return label;
    }
}

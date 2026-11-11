#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Created only during Editor Play Mode, never serialized into a scene or prefab.
public sealed class DevConsoleOverlay : ScriptableObject
{
    private readonly List<string> output = new List<string>();
    private readonly List<string> history = new List<string>();
    private readonly Dictionary<Behaviour, bool> suspended = new Dictionary<Behaviour, bool>();
    private TMP_InputField input;
    private TMP_Text log;
    private TMP_Text title;
    private ScrollRect scroll;
    private int historyIndex;
    private string draft = "";
    private float resumeTimeScale;
    private GameObject previousSelection;
    private bool closed;
    private GameObject root;
    private GameObject gameObject => root;
    private Transform transform => root.transform;
    private bool focusPending;
    private int focusAfterFrame;
    private int lastInputFrame = -1;
    private bool released;
    public bool HasInputFocus => input != null && input.isFocused;

    public void Open()
    {
        root = new GameObject("EditorDevConsole", typeof(RectTransform));
        root.hideFlags = HideFlags.DontSave;
        DontDestroyOnLoad(root);
        EditorApplication.update += Update;
        resumeTimeScale = Time.timeScale;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        Time.timeScale = 0;
        BuildUI();
        Log("Editor only. F8 / Esc closes. Type help. Up/Down: history. Tab: command completion.");
        Refocus();
        SuspendGameplayInput();
        ItemTooltipView.HideAll();
    }

    private static bool HandlesGameplayInput(Behaviour component) => component is PlayerController ||
        component is HotbarController || component is MenuController || component is InteractionPromptTrigger ||
        component is Frizer || component is Refrigerator || component is Vegetables || component is Stove ||
        component is Pan || component is Pot || component is MenuBoardInteraction || component is MarketInteraction ||
        component is KeyboardUpgradeStation || component is CustomerOrderInteraction;

    private void SuspendGameplayInput()
    {
        foreach (var component in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            if (HandlesGameplayInput(component))
            {
                if (!suspended.ContainsKey(component)) suspended.Add(component, component.enabled);
                component.enabled = false;
            }
    }

    private void Update()
    {
        if (closed || root == null) return;
        if (focusPending && Time.frameCount >= focusAfterFrame) FocusInput();
        // Scene loads can enable movement or briefly restore time; keep console ownership.
        if (Time.timeScale != 0) { resumeTimeScale = Time.timeScale; Time.timeScale = 0; }
        SuspendGameplayInput();
        title.text = "EDITOR CONSOLE — " + SceneManager.GetActiveScene().name + " — paused";
        if (lastInputFrame == Time.frameCount) return;
        lastInputFrame = Time.frameCount;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
        if (!HasInputFocus) return;
        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            if (historyIndex == history.Count) draft = input.text;
            if (history.Count > 0) { historyIndex = Mathf.Max(0, historyIndex - 1); input.SetTextWithoutNotify(history[historyIndex]); }
            Refocus();
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            historyIndex = Mathf.Min(history.Count, historyIndex + 1);
            input.SetTextWithoutNotify(historyIndex < history.Count ? history[historyIndex] : draft);
            Refocus();
        }
        else if (keyboard.tabKey.wasPressedThisFrame)
        {
            var matches = DevConsoleCommands.CommandNames.Where(c => c.StartsWith(input.text, System.StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 1) input.SetTextWithoutNotify(matches[0] + " ");
            else if (matches.Length > 1) Log(string.Join("  ", matches));
            Refocus();
        }
    }

    public void Close()
    {
        if (closed) return;
        closed = true;
        Release();
        if (root != null)
        {
            root.SetActive(false);
            if (Application.isPlaying) Destroy(root); else DestroyImmediate(root);
        }
        DestroyImmediate(this);
    }

    private void OnDisable() { Release(); }
    private void Release()
    {
        if (released) return;
        released = true;
        EditorApplication.update -= Update;
        foreach (var pair in suspended) if (pair.Key != null) pair.Key.enabled = pair.Value;
        suspended.Clear();
        if (Application.isPlaying) Time.timeScale = resumeTimeScale;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
    }

    private void Refocus()
    {
        if (closed) return;
        focusPending = true;
        focusAfterFrame = Time.frameCount + 1;
    }

    private void FocusInput()
    {
        // TMP deactivates a single-line field after onSubmit returns. Re-select afterwards.
        focusPending = false;
        if (closed || input == null) return;
        EventSystem.current?.SetSelectedGameObject(input.gameObject);
        input.Select();
        input.ActivateInputField();
        input.caretPosition = input.text.Length;
        input.selectionAnchorPosition = input.selectionFocusPosition = input.text.Length;
    }

    private void SubmitCurrent()
    {
        string command = input.text;
        input.SetTextWithoutNotify("");
        draft = "";
        Submit(command);
        Refocus();
    }

    public DevConsoleResult Submit(string command)
    {
        if (!string.IsNullOrWhiteSpace(command))
        {
            if (history.Count == 0 || history[history.Count - 1] != command) history.Add(command);
            if (history.Count > 100) history.RemoveAt(0);
            historyIndex = history.Count;
            Log("> " + command);
        }
        var result = DevConsoleCommands.Execute(command);
        if (result.Clear) { output.Clear(); log.text = ""; }
        foreach (string line in result.Lines) Log(line);
        if (result.CheckpointLoaded) { resumeTimeScale = Time.timeScale; Time.timeScale = 0; }
        Refocus();
        return result;
    }

    public static void LogGlobal(string text, bool error = false)
    {
        foreach (var overlay in Resources.FindObjectsOfTypeAll<DevConsoleOverlay>()) overlay.Log(text);
        if (error) Debug.LogError(text); else Debug.Log(text);
    }

    private void Log(string text)
    {
        output.Add(text);
        if (output.Count > 500) output.RemoveRange(0, output.Count - 500);
        log.text = string.Join("\n", output);
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 0;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }
    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static Image Image(RectTransform rect, Color color) { var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image; }
    private static TMP_Text Label(string name, Transform parent, string text, int size)
    {
        var rect = Rect(name, parent);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = size; label.text = text; label.color = Color.white;
        label.richText = false; label.raycastTarget = false;
        return label;
    }
    private static Button Button(string name, Transform parent, string text, System.Action action)
    {
        var rect = Rect(name, parent);
        var image = Image(rect, new Color(.22f, .27f, .3f, 1));
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() => action());
        var label = Label("Text", rect, text, 22); label.alignment = TextAlignmentOptions.Center;
        Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = short.MaxValue;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        var backdrop = Rect("InputBlocker", transform); Stretch(backdrop, Vector2.zero, Vector2.one); Image(backdrop, new Color(0, 0, 0, .12f));
        var panel = Rect("ConsolePanel", backdrop); Stretch(panel, new Vector2(0, .45f), Vector2.one);
        panel.offsetMin = new Vector2(16, 12); panel.offsetMax = new Vector2(-16, -16); Image(panel, new Color(.05f, .07f, .08f, .98f));
        title = Label("Title", panel, "EDITOR CONSOLE", 23); Stretch(title.rectTransform, new Vector2(0, 1), Vector2.one);
        title.rectTransform.offsetMin = new Vector2(14, -46); title.rectTransform.offsetMax = new Vector2(-240, -8);
        var help = Button("Help", panel, "Help", () => { Submit("help"); Refocus(); });
        help.GetComponent<RectTransform>().anchorMin = help.GetComponent<RectTransform>().anchorMax = Vector2.one;
        help.GetComponent<RectTransform>().pivot = Vector2.one; help.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 36); help.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130, -8);
        var close = Button("Close", panel, "Close (F8)", Close); var cr = close.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = cr.pivot = Vector2.one; cr.sizeDelta = new Vector2(110, 36); cr.anchoredPosition = new Vector2(-10, -8);
        var viewport = Rect("LogViewport", panel); Stretch(viewport, Vector2.zero, Vector2.one);
        viewport.offsetMin = new Vector2(14, 70); viewport.offsetMax = new Vector2(-14, -52); viewport.gameObject.AddComponent<RectMask2D>();
        Image(viewport, Color.clear);
        scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.viewport = viewport;
        var content = Rect("LogContent", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
        log = content.gameObject.AddComponent<TextMeshProUGUI>(); log.font = TMP_Settings.defaultFontAsset; log.fontSize = 22;
        log.richText = false; log.color = new Color(.85f, .94f, .87f); log.alignment = TextAlignmentOptions.TopLeft; log.raycastTarget = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; scroll.content = content;
        var field = Rect("CommandInput", panel); Stretch(field, Vector2.zero, new Vector2(1, 0)); field.offsetMin = new Vector2(14, 14); field.offsetMax = new Vector2(-110, 60); Image(field, new Color(.12f, .16f, .18f));
        input = field.gameObject.AddComponent<TMP_InputField>(); input.lineType = TMP_InputField.LineType.SingleLine; input.characterLimit = 2048;
        input.navigation = new Navigation { mode = Navigation.Mode.None }; input.richText = false;
        var textArea = Rect("TextArea", field); Stretch(textArea, Vector2.zero, Vector2.one); textArea.offsetMin = new Vector2(10, 0); textArea.offsetMax = new Vector2(-10, 0); textArea.gameObject.AddComponent<RectMask2D>();
        var text = Label("Text", textArea, "", 25); Stretch(text.rectTransform, Vector2.zero, Vector2.one); text.alignment = TextAlignmentOptions.MidlineLeft;
        input.textViewport = textArea; input.textComponent = text; input.caretColor = Color.white; input.customCaretColor = true;
        input.onSubmit.AddListener(_ => SubmitCurrent());
        var run = Button("Run", panel, "Run", SubmitCurrent); var rr = run.GetComponent<RectTransform>();
        rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(1, 0); rr.sizeDelta = new Vector2(86, 46); rr.anchoredPosition = new Vector2(-14, 14);
    }
}
#endif

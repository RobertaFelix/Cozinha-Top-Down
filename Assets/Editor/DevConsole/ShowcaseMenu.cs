using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class ShowcaseMenu
{
    private static readonly Color Ink = new Color32(65,41,36,255);
    private static readonly Color Paper = new Color32(245,223,185,255);
    private static readonly Color Wood = new Color32(115,76,60,255);
    private readonly GameObject canvas;
    private readonly RectTransform modal, panel, tabs, viewport;
    private readonly Text guide, footer;
    private readonly ScrollRect scroll;
    private readonly Dictionary<string,string> instructions = new();
    private readonly List<RectTransform> lists = new();
    private readonly List<Button> categories = new();
    private readonly Font font;
    private RectTransform currentList;
    private bool paused;
    private float originalScale;
    private GameObject previousSelection;

    public ShowcaseMenu()
    {
        var catalog = Resources.Load<RestaurantFurnitureCatalog>("RestaurantFurnitureCatalog");
        font = catalog && catalog.interfaceFont ? catalog.interfaceFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        canvas = new GameObject("Playtest menu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas.hideFlags = HideFlags.DontSave;
        UnityEngine.Object.DontDestroyOnLoad(canvas);
        var layer = canvas.GetComponent<Canvas>();
        layer.renderMode = RenderMode.ScreenSpaceOverlay;
        layer.sortingOrder = 31000;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.matchWidthOrHeight = .5f;
        var launch = ButtonAt(canvas.transform,"Tests [F7]",new Vector2(152,48),Toggle);
        Position((RectTransform)launch.transform,new Vector2(.5f,1),new Vector2(0,-30));

        var guideBox = Box(canvas.transform,"Test instructions",new Color(.18f,.12f,.11f,.95f),new Vector2(800,110));
        Position(guideBox,new Vector2(.5f,1),new Vector2(0,-112));
        guideBox.GetComponent<Image>().raycastTarget = false;
        guide = Label(guideBox,"",22,Paper);
        guide.resizeTextForBestFit = true;
        guide.resizeTextMinSize = 17;
        guide.resizeTextMaxSize = 22;
        guideBox.gameObject.SetActive(false);

        modal = Box(canvas.transform,"Test picker",new Color(0,0,0,.58f),Vector2.zero);
        Stretch(modal);
        panel = Box(modal,"Playtests",Paper,new Vector2(920,705));
        var title = Label(panel,"Playtests",38,Ink);
        At(title.rectTransform,new Vector2(680,52),new Vector2(0,295));
        var close = ButtonAt(panel,"Close",new Vector2(118,44),Toggle);
        close.GetComponent<RectTransform>().anchoredPosition = new Vector2(372,296);
        tabs = Node(panel,"Test categories",new Vector2(850,55));
        tabs.anchoredPosition = new Vector2(0,219);

        var listArea = Node(panel,"Scenarios",new Vector2(840,440));
        listArea.anchoredPosition = new Vector2(0,-45);
        viewport = Box(listArea,"Viewport",new Color(0,0,0,0),Vector2.zero);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll = listArea.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35;
        footer = Label(panel,"",20,Ink);
        At(footer.rectTransform,new Vector2(825,58),new Vector2(0,-306));
        modal.gameObject.SetActive(false);
    }

    public void Group(string name)
    {
        int index = lists.Count;
        var tab = ButtonAt(tabs,name,new Vector2(390,50),()=>SelectGroup(index));
        tab.GetComponent<RectTransform>().anchoredPosition = new Vector2(-205 + index*410,0);
        categories.Add(tab);
        currentList = Node(viewport,name,Vector2.zero);
        currentList.anchorMin = new Vector2(0,1);
        currentList.anchorMax = Vector2.one;
        currentList.pivot = new Vector2(.5f,1);
        var grid = currentList.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(397,62);
        grid.spacing = new Vector2(14,10);
        grid.padding = new RectOffset(10,10,8,8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        currentList.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        lists.Add(currentList);
        SelectGroup(0);
    }

    private void SelectGroup(int index)
    {
        for (int i=0;i<lists.Count;i++)
        {
            lists[i].gameObject.SetActive(i==index);
            categories[i].interactable = i!=index;
        }
        scroll.content = lists[index];
        scroll.StopMovement();
        scroll.content.anchoredPosition = Vector2.zero;
    }

    public void Add(string name,string instruction)
    {
        instructions.Add(name,instruction);
        ButtonAt(currentList,name,new Vector2(397,62),()=>ShowcaseLauncher.Run(name));
    }

    public void Toggle() => SetOpen(!modal.gameObject.activeSelf);

    public void SetOpen(bool open)
    {
        if (modal.gameObject.activeSelf == open) return;
        modal.gameObject.SetActive(open);
        if (open)
        {
            footer.text = ShowcaseLauncher.IsIsolated
                ? "Choose a test to start a fresh demo. Your test progress resets between scenarios."
                : "Choosing a test ends this Play session. Unsaved progress is discarded. Tests use a separate save.";
            if (SaveGameService.Instance && SaveGameService.Instance.IsReady && ShowcaseLauncher.Status == "Ready")
            {
                originalScale = Time.timeScale;
                Time.timeScale = 0;
                paused = true;
            }
            previousSelection = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            var first = scroll.content.GetComponentInChildren<Button>();
            if (first && EventSystem.current) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        else
        {
            if (paused) Time.timeScale = originalScale;
            paused = false;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(previousSelection);
        }
    }

    public void ShowInstructions(string name, string observation) => SetGuide(name + "\n" + instructions[name] +
        (string.IsNullOrEmpty(observation) ? "" : "\n" + observation));
    public void SetGuide(string message)
    {
        guide.text = message;
        guide.transform.parent.gameObject.SetActive(true);
    }

    public void Dispose()
    {
        if (paused && Application.isPlaying) Time.timeScale = originalScale;
        if (canvas) UnityEngine.Object.DestroyImmediate(canvas);
    }

    private static RectTransform Node(Transform parent,string name,Vector2 size)
    {
        var rt = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent,false);
        rt.sizeDelta = size;
        return rt;
    }
    private static RectTransform Box(Transform parent,string name,Color color,Vector2 size)
    {
        var rt = Node(parent,name,size);
        rt.gameObject.AddComponent<Image>().color = color;
        return rt;
    }
    private Text Label(Transform parent,string value,int size,Color color)
    {
        var rt = Node(parent,"Label",Vector2.zero);
        Stretch(rt);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }
    private Button ButtonAt(Transform parent,string name,Vector2 size,Action click)
    {
        var rt = Box(parent,name,Wood,size);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = rt.GetComponent<Image>();
        button.onClick.AddListener(()=>click());
        Label(rt,name,24,Paper);
        return button;
    }
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    private static void Position(RectTransform rt,Vector2 anchor,Vector2 position)
    {
        rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = position;
    }
    private static void At(RectTransform rt,Vector2 size,Vector2 position)
    {
        Position(rt,new Vector2(.5f,.5f),position); rt.sizeDelta = size;
    }
}

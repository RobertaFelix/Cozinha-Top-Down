using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class RestaurantFurnitureEditor : SaveParticipant<RestaurantLayoutData>, ISceneSaveParticipant
{
    private RestaurantLayout layout;
    private EnvironmentTransition environment;
    private UnityEngine.SceneManagement.Scene environmentScene;
    private RestaurantLayout.Piece selection;
    private RectTransform overlay, toolbar;
    private Button editButton, storeButton;
    private Text hint;
    private readonly List<RestaurantLayout.Piece> choices = new List<RestaurantLayout.Piece>();
    private readonly List<Button> buttons = new List<Button>();
    private int pageStart, openedFrame;
    private const int PageSize = 5;
    private SpriteRenderer preview;
    private float previousTimeScale;
    private Camera editCamera;
    private Unity.Cinemachine.CinemachineBrain cameraBrain;
    private bool restoreBrain;
    private Vector3 originalCameraPosition;
    private bool editing;
    private static RestaurantFurnitureEditor instance;
    private static int lastExitFrame = -1;
    internal static bool ClosedThisFrame => lastExitFrame == Time.frameCount;
    public static bool Editing => instance && instance.editing;
    public override string SaveKey => "restaurant-layout";
    public override int RestoreOrder => 35;
    public bool SceneStateAvailable => Layout;
    private RestaurantLayout Layout => layout ? layout : (layout = RestaurantLayout.FindLayout());
    private static RestaurantFurnitureCatalog catalog;
    private static RestaurantFurnitureCatalog Catalog => catalog ? catalog : (catalog = Resources.Load<RestaurantFurnitureCatalog>("RestaurantFurnitureCatalog"));
    internal static readonly Color Ink = new Color32(69,43,29,255);

    private void Awake()
    {
        instance = this;
        BuildOverlay();
    }

    protected override RestaurantLayoutData Capture()
    {
        FurnitureInventoryPage.Remember(Layout);
        return layout.Capture();
    }

    internal static void ChooseFromInventory(string label, Sprite sprite)
    {
        if (!instance || !instance.Layout || !instance.layout.gameObject.activeInHierarchy) return;
        InventoryPanelSession.CloseCurrent();
        if (!instance.editing) instance.Toggle();
        if (!instance.editing) return;
        var index = instance.choices.FindIndex(p => p.label == label && p.visual.sprite == sprite);
        if (index < 0) return;
        instance.pageStart = Mathf.Clamp(index,0,Mathf.Max(0, instance.choices.Count - PageSize));
        instance.Select(instance.choices[index]);
    }
    protected override void Restore(RestaurantLayoutData state) => Layout.Restore(state);
    protected override void Validate(RestaurantLayoutData state)
    {
        Require(state.pieces != null && state.pieces.Count < 2048, "Invalid restaurant furniture.");
        var keys = new HashSet<string>();
        foreach (var entry in state.pieces)
            Require(entry != null && !string.IsNullOrEmpty(entry.id) && keys.Add(entry.id) &&
                float.IsFinite(entry.localPosition.x) && float.IsFinite(entry.localPosition.y) && float.IsFinite(entry.localPosition.z),
                "Invalid furniture placement.");
    }

    private void BuildOverlay()
    {
        overlay = new GameObject("Restaurant editor", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<RectTransform>();
        overlay.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.matchWidthOrHeight = .5f;
        editButton = MakeButton(overlay, "Edit restaurant", new Vector2(214,54), Toggle);
        Anchor((RectTransform)editButton.transform, Vector2.right, new Vector2(-130,44));
        toolbar = MakePanel(overlay, "Furniture hotbar", new Vector2(926,130));
        Anchor(toolbar, new Vector2(.5f,0), new Vector2(0,76));
        var previous = MakeButton(toolbar, "<", new Vector2(45,70), () => Scroll(-1));
        previous.transform.localPosition = new Vector3(-429,-6,0);
        for (int i = 0; i < PageSize; i++)
        {
            int index = i;
            var button = MakeButton(toolbar, "", new Vector2(144,74), () => Select(choices[pageStart + index]));
            button.transform.localPosition = new Vector3((i - 2)*151,-7,0);
            var icon = new GameObject("Furniture", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.rectTransform.SetParent(button.transform, false);
            icon.rectTransform.sizeDelta = new Vector2(112,39);
            icon.rectTransform.anchoredPosition = Vector2.up * 12;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var label = button.GetComponentInChildren<Text>();
            label.rectTransform.anchorMax = new Vector2(1,.4f);
            label.fontSize = 22;
            buttons.Add(button);
        }
        var next = MakeButton(toolbar, ">", new Vector2(45,70), () => Scroll(1));
        next.transform.localPosition = new Vector3(429,-6,0);
        hint = Label(toolbar, "Choose a piece or click furniture to move it", 17);
        hint.rectTransform.anchorMin = new Vector2(0,.81f);
        storeButton = MakeButton(overlay, "Put away", new Vector2(180,54), Store);
        Anchor((RectTransform)storeButton.transform, Vector2.right, new Vector2(-130,110));
        toolbar.gameObject.SetActive(false);
        storeButton.gameObject.SetActive(false);
        editButton.gameObject.SetActive(false);
    }

    internal static RectTransform MakePanel(Transform parent, string name, Vector2 size)
    {
        var r = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        r.SetParent(parent,false);
        r.sizeDelta = size;
        var frame = r.GetComponent<Image>();
        frame.sprite = Catalog.panelFrame;
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 1;
        frame.color = Color.white;
        return r;
    }
    private static void Anchor(RectTransform r, Vector2 anchor, Vector2 offset)
    {
        r.anchorMin = r.anchorMax = anchor;
        r.anchoredPosition = offset;
    }
    internal static Text Label(Transform parent, string value, int size)
    {
        var t = new GameObject("Label", typeof(RectTransform),typeof(Text)).GetComponent<Text>();
        t.rectTransform.SetParent(parent,false);
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        t.font = Catalog.interfaceFont;
        t.text = value;
        t.fontSize = Mathf.RoundToInt(size * 1.35f);
        t.color = Ink;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        return t;
    }
    internal static Button MakeButton(Transform parent, string label, Vector2 size, UnityEngine.Events.UnityAction click)
    {
        var r = MakePanel(parent,label,size);
        var b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = r.GetComponent<Image>();
        b.targetGraphic.color = Color.white;
        b.onClick.AddListener(click);
        Label(r,label,21);
        return b;
    }

    private void Toggle()
    {
        if (editing) { Finish(); return; }
        if (!Layout || !layout.gameObject.activeInHierarchy || InventoryPanelSession.HasOpenPanel || Time.timeScale <= 0) return;
        editing = true;
        openedFrame = Time.frameCount;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0;
        editCamera = Camera.main;
        if (editCamera)
        {
            originalCameraPosition = editCamera.transform.position;
            cameraBrain = editCamera.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            restoreBrain = cameraBrain && cameraBrain.enabled;
            if (restoreBrain) cameraBrain.enabled = false;
        }
        InventoryWorkspace.SetShopHotbarVisible(false);
        toolbar.gameObject.SetActive(true);
        editButton.GetComponentInChildren<Text>().text = "Done";
        Select(null);
        Refresh();
    }

    private void Finish()
    {
        if (!editing) return;
        editing = false;
        if (editCamera) editCamera.transform.position = originalCameraPosition;
        if (cameraBrain && restoreBrain) cameraBrain.enabled = true;
        lastExitFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
        Select(null);
        toolbar.gameObject.SetActive(false);
        InventoryWorkspace.SetShopHotbarVisible(true);
        editButton.GetComponentInChildren<Text>().text = "Edit restaurant";
    }

    private void Update()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene != environmentScene)
        {
            environmentScene = scene;
            environment = FindAnyObjectByType<EnvironmentTransition>();
        }
        bool inRestaurant = environment && environment.CurrentInteriorId == "InteriorRestaurant" && !environment.IsTransitioning;
        if (inRestaurant && !layout) layout = RestaurantLayout.FindLayout();
        bool canOpen = inRestaurant && !InventoryPanelSession.HasOpenPanel && (editing || Time.timeScale > 0);
        editButton.gameObject.SetActive(canOpen);
        if (!canOpen) { Finish(); return; }
        var kb = Keyboard.current;
        if (kb != null && kb.bKey.wasPressedThisFrame) { Toggle(); return; }
        if (!editing || Time.frameCount == openedFrame) return;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (selection != null) Select(null); else Finish();
            return;
        }
        if (kb != null && editCamera)
        {
            Vector2 direction = new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0),
                (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
            if (direction.sqrMagnitude > 0)
            {
                var current = editCamera.transform.position;
                var target = layout.ClampView((Vector2)current + direction.normalized * (8f * Time.unscaledDeltaTime));
                editCamera.transform.position = new Vector3(target.x,target.y,current.z);
            }
        }
        var mouse = Mouse.current;
        if (mouse == null || !Camera.main) return;
        float wheel = mouse.scroll.ReadValue().y;
        if (wheel != 0) Scroll(wheel < 0 ? 1 : -1);
        if (mouse.rightButton.wasPressedThisFrame) { Select(null); return; }
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
        { if (preview) preview.enabled = false; return; }
        var point = (Vector2)Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        if (selection != null)
        {
            bool valid = layout.Preview(selection, point, out var anchor, out _);
            preview.enabled = true;
            preview.transform.position = anchor - preview.transform.TransformVector(preview.sprite.bounds.center);
            preview.color = valid ? new Color(.66f,1f,.72f,.8f) : new Color(1f,.42f,.38f,.7f);
            if (valid && mouse.leftButton.wasPressedThisFrame && layout.Place(selection, point))
                Select(null);
        }
        else if (mouse.leftButton.wasPressedThisFrame) Select(layout.Pick(point));
    }

    private void Select(RestaurantLayout.Piece piece)
    {
        if (piece != null && piece.root.TryGetComponent<Table>(out var table) && table.HasOccupiedChair)
        {
            hint.text = "A customer has reserved this table";
            return;
        }
        selection = piece;
        if (!preview && piece != null)
        {
            preview = new GameObject("Furniture placement preview").AddComponent<SpriteRenderer>();
            preview.transform.SetParent(layout.transform,true);
            preview.sortingOrder = 1000;
        }
        if (preview)
        {
            preview.enabled = false;
            if (piece != null)
            {
                preview.sprite = piece.visual.sprite;
                preview.sortingLayerID = piece.visual.sortingLayerID;
                preview.transform.localScale = piece.visual.transform.lossyScale;
            }
        }
        hint.text = piece == null ? "Click a piece to move or store it. WASD to pan." :
            piece.category + "   /   " + piece.label + (piece.tabletop ? "   •   Choose an empty counter" : "   •   Click to place");
        storeButton.gameObject.SetActive(editing && piece != null && piece.Placed);
        Refresh();
    }

    private void Store()
    {
        if (layout.Store(selection)) Select(null);
        else if (selection != null && selection.root.TryGetComponent<KitchenAppliance>(out var appliance) && !appliance.CanStore)
            hint.text = "Empty the appliance before putting it away";
        else if (selection?.id == "kitchen/stove") hint.text = "Collect the dish before storing the stove";
        else hint.text = "Remove the item on this counter first";
    }
    private void Scroll(int delta)
    {
        pageStart = Mathf.Clamp(pageStart + delta,0,Mathf.Max(0,choices.Count - PageSize));
        Refresh();
    }
    private void Refresh()
    {
        if (!editing || !layout) return;
        choices.Clear();
        foreach (string category in new[] { "Construction", "Kitchen", "Decoration", "Utility" })
            foreach (var piece in layout.Pieces)
            {
                if (piece.Placed || piece.category != category) continue;
                var index = choices.FindIndex(other => other.label == piece.label && other.visual.sprite == piece.visual.sprite);
                if (index < 0) choices.Add(piece);
            }
        pageStart = Mathf.Clamp(pageStart,0,Mathf.Max(0,choices.Count - PageSize));
        for (int i = 0; i < buttons.Count; i++)
        {
            bool available = i + pageStart < choices.Count;
            var button = buttons[i];
            button.gameObject.SetActive(available);
            if (!available) continue;
            var p = choices[i+pageStart];
            int stored = 0;
            foreach (var other in layout.Pieces)
                if (other.label == p.label && other.visual.sprite == p.visual.sprite && !other.Placed) stored++;
            button.GetComponentInChildren<Text>().text = p.label + "  x" + stored;
            button.transform.Find("Furniture").GetComponent<Image>().sprite = p.visual.sprite;
            button.targetGraphic.color = p == selection ? new Color32(239,197,119,255) : Color.white;
        }
    }
    private void OnDisable() { if (editing) Finish(); }
    private void OnDestroy() { if (instance == this) instance = null; }
}

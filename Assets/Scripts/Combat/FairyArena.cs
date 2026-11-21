using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public sealed class FairyArena : MonoBehaviour
{
    private FairyBoss fairy;
    private bool defeated, zoomed;
    private DungeonChest reward;
    private Tilemap floor;
    private Transform player;
    private CinemachineCamera view;
    private PixelPerfectCamera pixels;
    private int refX, refY;
    private float normalLens;
    private GameObject hud;
    private RectTransform healthFill;
    private TMP_Text title;
    private int displayedHealth = -1;

    public static FairyArena At(Transform arrival) => arrival.GetComponent<FairyArena>() ?? arrival.gameObject.AddComponent<FairyArena>();
    private void Awake()
    {
        foreach (var map in transform.parent.GetComponentsInChildren<Tilemap>())
            if (map.name == "TileBackGroundFloor") floor = map;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    public bool Contains(Vector3 point) => floor ? floor.HasTile(floor.WorldToCell(point)) : Vector2.Distance(point, transform.position) < 10;

    public void Enter()
    {
        if (fairy || defeated) return;
        var style = DungeonExpeditionStyle.Load();
        var actor = new GameObject("The Cinder Fairy", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D));
        actor.transform.SetParent(transform, false);
        actor.transform.localPosition = Vector3.right * 3f;
        var body = actor.GetComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.freezeRotation = true;
        body.bodyType = RigidbodyType2D.Kinematic;
        actor.GetComponent<CircleCollider2D>().radius = .38f;
        fairy = actor.AddComponent<FairyBoss>();
        fairy.Initialize(this, style);
        CreateHealthBar();
    }

    private void LateUpdate()
    {
        bool inside = player && Contains(player.position);
        if (inside != zoomed) SetZoom(inside);
        if (!hud) return;
        bool show = inside && fairy && !defeated;
        if (hud.activeSelf != show) hud.SetActive(show);
        if (!show || displayedHealth == fairy.Health) return;
        displayedHealth = fairy.Health;
        healthFill.anchorMax = new Vector2(fairy.Health / (float)fairy.Maximum,1);
        title.text = $"The Cinder Fairy   {fairy.Health} / {fairy.Maximum}";
    }

    private void SetZoom(bool wide)
    {
        if (wide)
        {
            view = FindAnyObjectByType<CinemachineCamera>();
            pixels = Camera.main ? Camera.main.GetComponent<PixelPerfectCamera>() : null;
            if (view) normalLens = view.Lens.OrthographicSize;
            if (pixels) { refX = pixels.refResolutionX; refY = pixels.refResolutionY; }
        }
        if (view) view.Lens.OrthographicSize = wide ? normalLens * 1.4f : normalLens;
        if (pixels)
        {
            pixels.refResolutionX = wide ? Mathf.RoundToInt(refX * 1.4f) : refX;
            pixels.refResolutionY = wide ? Mathf.RoundToInt(refY * 1.4f) : refY;
        }
        zoomed = wide;
    }

    private void CreateHealthBar()
    {
        hud = new GameObject("Boss health",typeof(Canvas),typeof(CanvasScaler));
        hud.transform.SetParent(transform,false);
        var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 12; canvas.pixelPerfect = true;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        var frame = new GameObject("Health frame",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
        frame.SetParent(hud.transform,false);
        frame.anchorMin = frame.anchorMax = new Vector2(.5f,0);
        frame.pivot = new Vector2(.5f,0); frame.anchoredPosition = new Vector2(0,170); frame.sizeDelta = new Vector2(490,24);
        var background = frame.GetComponent<Image>(); background.raycastTarget = false; background.color = new Color(.14f,.08f,.13f);
        healthFill = new GameObject("Life",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
        healthFill.SetParent(frame,false); healthFill.anchorMin = Vector2.zero; healthFill.anchorMax = Vector2.one;
        healthFill.offsetMin = new Vector2(3,3); healthFill.offsetMax = new Vector2(-3,-3);
        var fill = healthFill.GetComponent<Image>(); fill.color = new Color(.95f,.28f,.31f); fill.raycastTarget = false;
        title = new GameObject("Fairy",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        title.transform.SetParent(frame,false); title.rectTransform.anchoredPosition = new Vector2(0,29); title.rectTransform.sizeDelta = new Vector2(500,34);
        var uiStyle = Resources.Load<ItemTooltipStyle>("ItemTooltipStyle");
        if (uiStyle && uiStyle.font) title.font = uiStyle.font;
        title.fontSize = 28; title.alignment = TextAlignmentOptions.Center; title.raycastTarget = false;
    }

    private void SpawnRewards(bool claimed)
    {
        var layout = FindAnyObjectByType<DungeonLayout>();
        var door = layout.GetComponentInChildren<DungeonBossDoor>();
        reward = layout.GetComponent<DungeonChestSpawner>().CreateReward(transform,transform.position + Vector3.left * 1.7f,
            DungeonExpeditionStyle.Load().fairyTreasure);
        reward.RestoreOpened(claimed);
        DungeonExpedition.CreateExit(transform,transform.position + Vector3.right * 2.3f,door);
    }
    public void Defeat()
    {
        if (defeated) return;
        defeated = true;
        SpawnRewards(false);
        MarketHoursNotice.Show("The Cinder Fairy has fallen. Claim your treasure and return to the village.");
    }
    public void Capture(DungeonRunState state)
    {
        state.fairyEncountered = true;
        state.fairyDefeated = defeated;
        state.fairyHealth = fairy ? fairy.Health : 0;
        state.fairyMaximum = fairy ? fairy.Maximum : 0;
        state.fairyRewardClaimed = reward && reward.IsOpened;
    }
    public void Restore(DungeonRunState state)
    {
        defeated = state.fairyDefeated;
        if (defeated) { SpawnRewards(state.fairyRewardClaimed); return; }
        Enter();
        int previousMaximum = state.fairyMaximum > 0 ? state.fairyMaximum : 180;
        fairy.RestoreHealth(Mathf.RoundToInt(state.fairyHealth * (float)fairy.Maximum / previousMaximum));
    }
    private void OnDisable() { if (zoomed) SetZoom(false); }
}

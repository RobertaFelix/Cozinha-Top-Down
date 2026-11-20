using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerStamina : MonoBehaviour
{
    [SerializeField, Min(1)] private float maximum = 100;
    [SerializeField, Min(0)] private float regenerationPerSecond = 25;
    [SerializeField, Min(0)] private float regenerationDelay = .6f;
    [SerializeField] private Sprite barSprite;
    [SerializeField] private Vector2 hudPosition = new Vector2(24, -132);
    [SerializeField, Min(.1f)] private float pixelScale = 6.25f;
    [SerializeField] private Color fillColor = new Color32(253, 220, 27, 255);
    [SerializeField] private Color emptyColor = new Color32(80, 57, 38, 255);
    public float Maximum => maximum;
    public float Current { get; private set; }
    public bool InDungeon => health && health.InDungeon;
    private PlayerHealth health;
    private bool activeVisit;
    private float lastSpend;
    private GameObject hud;
    private readonly Image[] fillRows = new Image[5];
    // Interior scanlines of estamina.png, in source pixels. The stepped tip stays inside its border.
    private static readonly int[] rowWidths = { 43, 44, 45, 44, 43 };

    private void Awake() { health = GetComponent<PlayerHealth>(); }
    private void Update()
    {
        if (InDungeon != activeVisit)
        {
            activeVisit = InDungeon;
            Current = activeVisit ? maximum : 0;
            lastSpend = Time.time;
            if (activeVisit) BuildHud();
            else if (hud) Destroy(hud);
        }
        if (activeVisit && health.Current > 0 && Time.time >= lastSpend + regenerationDelay)
            Current = Mathf.Min(maximum, Current + regenerationPerSecond * Time.deltaTime);
        float width = 45 * Mathf.Clamp01(Current / maximum);
        for (int i = 0; i < fillRows.Length; i++)
            if (fillRows[i])
                fillRows[i].rectTransform.sizeDelta = new Vector2(Mathf.Min(rowWidths[i], width), 1) * pixelScale;
    }
    public bool TrySpend(float amount)
    {
        if (!InDungeon || health.Current <= 0 || amount < 0 || Current < amount) return false;
        Current -= amount;
        lastSpend = Time.time;
        return true;
    }
    public bool TryRestore(float amount)
    {
        if (!InDungeon || health.Current <= 0 || amount <= 0 || Current >= maximum) return false;
        Current = Mathf.Min(maximum, Current + amount);
        return true;
    }
    private void BuildHud()
    {
        if (hud) Destroy(hud);
        hud = new GameObject("Dungeon Stamina", typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(hud, SceneManager.GetActiveScene());
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var rect = new GameObject("Stamina", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(hud.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
        rect.anchoredPosition = hudPosition;
        rect.sizeDelta = (barSprite ? barSprite.rect.size : new Vector2(54, 16)) * pixelScale;
        for (int i = 0; i < fillRows.Length; i++)
        {
            Row(rect, "Empty stamina " + i, i, emptyColor);
            fillRows[i] = Row(rect, "Available stamina " + i, i, fillColor);
        }
        // Draw the entire artwork last: the lightning, border and chevrons never shrink or fade.
        var frame = new GameObject("Frame", typeof(Image)).GetComponent<Image>();
        frame.transform.SetParent(rect, false);
        frame.rectTransform.anchorMin = Vector2.zero;
        frame.rectTransform.anchorMax = Vector2.one;
        frame.rectTransform.offsetMin = frame.rectTransform.offsetMax = Vector2.zero;
        frame.sprite = barSprite;
        frame.color = Color.white;
        frame.raycastTarget = false;
    }
    private Image Row(RectTransform parent, string name, int row, Color color)
    {
        var image = new GameObject(name, typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.raycastTarget = false;
        image.color = color;
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(7, 3 + row) * pixelScale;
        rect.sizeDelta = new Vector2(rowWidths[row], 1) * pixelScale;
        return image;
    }
    private void OnDisable()
    {
        if (hud) Destroy(hud);
        activeVisit = false;
        Current = 0;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximum = 100;
    [SerializeField] private SceneTransitionController transitions;
    [SerializeField] private Sprite fullHeart, halfHeart, emptyHeart;

    public int Maximum => maximum;
    public int Current { get; private set; }
    public bool InDungeon { get; private set; }

    private const int HeartCount = 5;
    private readonly Image[] hearts = new Image[HeartCount];
    private GameObject hud;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        OnSceneChanged(default, SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        EndVisit();
    }

    private void OnSceneChanged(Scene previous, Scene next)
    {
        if (!transitions || next.name != transitions.DungeonSceneName)
        {
            EndVisit();
            return;
        }

        EndVisit();
        InDungeon = true;
        Current = Maximum;
        hud = new GameObject("Dungeon Health", typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(hud, next);
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        for (int i = 0; i < HeartCount; i++)
        {
            var heart = new GameObject("Heart", typeof(Image)).GetComponent<Image>();
            heart.raycastTarget = false;
            var rect = heart.rectTransform;
            rect.SetParent(hud.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            heart.sprite = fullHeart;
            heart.SetNativeSize();
            rect.anchoredPosition = new Vector2(24 + i * rect.sizeDelta.x, -24);
            hearts[i] = heart;
        }
        RefreshHearts();
    }

    private void EndVisit()
    {
        InDungeon = false;
        Current = 0;
        if (hud) Destroy(hud);
        hud = null;
    }

    public void TakeDamage(int amount)
    {
        if (!InDungeon || amount <= 0) return;
        Current -= Mathf.Min(Current, amount);
        RefreshHearts();
    }

    public void Heal(int amount)
    {
        if (!InDungeon || amount <= 0) return;
        Current += Mathf.Min(Maximum - Current, amount);
        RefreshHearts();
    }

    private void RefreshHearts()
    {
        int halves = Mathf.CeilToInt((float)Current / Maximum * HeartCount * 2);
        for (int i = 0; i < HeartCount; i++)
        {
            int remaining = halves - i * 2;
            hearts[i].sprite = remaining >= 2 ? fullHeart : remaining == 1 ? halfHeart : emptyHeart;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test/Damage 10 HP")]
    private void TestDamage() => TakeDamage(10);
    [ContextMenu("Test/Heal 10 HP")]
    private void TestHeal() => Heal(10);
#endif
}

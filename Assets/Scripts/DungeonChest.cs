using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class DungeonChest : MonoBehaviour
{
    [SerializeField] private Sprite[] openingFrames;
    [SerializeField, Min(.01f)] private float frameDuration = .09f;
    private DungeonChestSpawner owner;
    private bool containsBossKey;
    private SpriteRenderer chestRenderer;
    public int RewardCoins { get; internal set; }
    public bool IsOpened { get; private set; }
    public bool ContainsBossKey => containsBossKey && !IsOpened;

    private void Awake() => chestRenderer = GetComponent<SpriteRenderer>();

    internal void Initialize(DungeonChestSpawner spawner, bool hasKey)
    {
        owner = spawner;
        containsBossKey = hasKey;
        IsOpened = false;
        chestRenderer = GetComponent<SpriteRenderer>();
        if (openingFrames != null && openingFrames.Length > 0) chestRenderer.sprite = openingFrames[0];
    }

    public bool TryOpen()
    {
        if (IsOpened || owner == null) return false;
        if (containsBossKey && !owner.CollectBossKey(this)) return false;
        if (RewardCoins > 0)
        {
            if (!CoinManager.Instance) return false;
            CoinManager.Instance.AddCoins(RewardCoins);
        }
        IsOpened = true;
        var trigger = GetComponentInChildren<InteractionPromptTrigger>();
        if (trigger != null) trigger.enabled = false;
        if (openingFrames != null && openingFrames.Length > 0)
        {
            if (Application.isPlaying) StartCoroutine(AnimateOpening());
            else chestRenderer.sprite = openingFrames[openingFrames.Length - 1];
        }
        return true;
    }
    public void RestoreOpened(bool opened)
    {
        IsOpened = opened;
        var trigger = GetComponentInChildren<InteractionPromptTrigger>();
        if (trigger != null) trigger.enabled = !opened;
        if (openingFrames != null && openingFrames.Length > 0)
            chestRenderer.sprite = openingFrames[opened ? openingFrames.Length - 1 : 0];
    }

    // Uses the same proximity/E interaction as the other objects in the game.
    public void Interact()
    {
        if (!TryOpen())
        {
            if (!IsOpened && containsBossKey) MarketHoursNotice.Show("Make room in your inventory for the boss room key.");
            return;
        }
        MarketHoursNotice.Show(RewardCoins > 0 ? $"Fairy treasure: {RewardCoins} coins!" : containsBossKey ? "Boss room key found!" : "The chest is empty.");
    }

    private IEnumerator AnimateOpening()
    {
        foreach (var frame in openingFrames)
        {
            if (frame != null) chestRenderer.sprite = frame;
            yield return new WaitForSeconds(frameDuration);
        }
    }
}

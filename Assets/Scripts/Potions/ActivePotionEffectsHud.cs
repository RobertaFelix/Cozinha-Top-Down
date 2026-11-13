using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Each icon and its duration rail form one separate hover target.
public sealed class ActivePotionEffectsHud : MonoBehaviour
{
    private sealed class Entry
    {
        public TimedPotionEffect effect;
        public GameObject root;
        public RectTransform fill;
    }
    private readonly List<Entry> entries = new List<Entry>();
    private TimedPotionEffects effects;

    private void Start()
    {
        effects = TimedPotionEffects.Instance;
        if (effects == null || effects.Definitions == null) return;
        foreach (var effect in effects.Definitions)
        {
            if (effect == null) continue;
            var group = Image(effect.EffectId, transform, new Color32(61, 43, 35, 230));
            var rect = group.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(78, 58);
            group.raycastTarget = true;

            var track = Image("Duration Track", rect, new Color32(29, 22, 24, 255));
            TopLeft(track.rectTransform, new Vector2(4, -4), new Vector2(8, 50));
            var fill = Image("Duration Fill", track.transform,
                effect.Kind == TimedPotionKind.MovementSpeed
                    ? new Color32(181, 111, 222, 255) : new Color32(250, 204, 69, 255));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            // Bottom remains fixed; the top descends as the remaining duration decreases.
            fill.rectTransform.pivot = new Vector2(.5f, 0);

            var icon = Image("Icon", rect, Color.white);
            icon.sprite = effect.Icon;
            icon.preserveAspect = true;
            TopLeft(icon.rectTransform, new Vector2(20, -2), new Vector2(54, 54));
            ItemTooltipTarget.Bind(group.gameObject, () => effects != null && effects.RemainingSeconds(effect.EffectId) > 0
                ? new ItemTooltipContent(effect.Kind == TimedPotionKind.MovementSpeed ? "Speed Boost" : "Coin Boost",
                    "Effect: " + effect.Description, effect.Icon) : default, false, (RectTransform)transform);
            group.gameObject.SetActive(false);
            entries.Add(new Entry { effect = effect, root = group.gameObject, fill = fill.rectTransform });
        }
    }
    private static Image Image(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
    private static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
    private void LateUpdate()
    {
        if (effects == null) return;
        int visible = 0;
        foreach (var entry in entries)
        {
            float remaining = effects.RemainingSeconds(entry.effect.EffectId);
            bool active = remaining > 0;
            entry.root.SetActive(active);
            if (!active) continue;
            ((RectTransform)entry.root.transform).anchoredPosition = new Vector2(visible++ * 94, 0);
            float progress = Mathf.Clamp01(remaining / Mathf.Max(.001f, entry.effect.DurationSeconds));
            entry.fill.anchorMax = new Vector2(1, progress);
        }
    }
}

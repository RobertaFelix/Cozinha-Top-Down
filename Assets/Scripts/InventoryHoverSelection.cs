using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// One raycast per frame also follows the pointer during drag and while a panel opens/closes.
public sealed class InventoryHoverSelection : MonoBehaviour
{
    private static InventoryHoverSelection instance;
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private RectTransform frame;
    private InventorySelectionStyle style;
    private ItemTooltipTarget hoveredTarget;
    private AudioSource hoverAudio;
    private double lastSoundTime = double.NegativeInfinity;
    private readonly List<TMP_Text> labels = new List<TMP_Text>();

    public static void EnsureRunning()
    {
        if (instance != null) return;
        instance = new GameObject("InventoryHoverSelection").AddComponent<InventoryHoverSelection>();
        DontDestroyOnLoad(instance.gameObject);
    }

    private void Awake()
    {
        style = Resources.Load<InventorySelectionStyle>("InventorySelectionStyle");
        if (style == null || style.hoverSound == null) return;
        style.hoverSound.LoadAudioData();
        hoverAudio = gameObject.AddComponent<AudioSource>();
        hoverAudio.playOnAwake = false;
        hoverAudio.loop = false;
        hoverAudio.spatialBlend = 0f;
        hoverAudio.ignoreListenerPause = true;
        hoverAudio.clip = style.hoverSound;
    }

    public static void PointerEntered(ItemTooltipTarget target)
    {
        if (instance != null && Application.isFocused) instance.Show(target);
    }

    private void LateUpdate()
    {
        ItemTooltipTarget target = null;
        if (Mouse.current != null && EventSystem.current != null && Application.isFocused)
        {
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = Mouse.current.position.ReadValue() }, hits);
            // Only the frontmost UI surface may select a slot; never look through another panel.
            if (hits.Count > 0)
                target = hits[0].gameObject.GetComponentInParent<ItemTooltipTarget>();
        }
        Show(target);
    }

    private void Show(ItemTooltipTarget target)
    {
        if (target == null || !target.isActiveAndEnabled || !(target.transform is RectTransform))
        {
            hoveredTarget = null;
            if (frame != null) frame.gameObject.SetActive(false);
            return;
        }
        if (frame == null && !CreateFrame()) return;
        if (hoveredTarget != target)
        {
            hoveredTarget = target;
            PlayHoverSound();
        }
        if (frame.parent != target.transform)
        {
            frame.SetParent(target.transform, false);
            frame.anchorMin = frame.anchorMax = new Vector2(.5f, .5f);
            frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition = Vector2.zero;
            frame.localScale = Vector3.one;
            frame.localRotation = Quaternion.identity;
        }
        // A square in rendered space, even when a hotbar ancestor scales X and Y differently.
        var slot = (RectTransform)target.transform;
        var scale = slot.lossyScale;
        float side = slot.rect.width * Mathf.Abs(scale.x) *
            (1f + 2f * Mathf.Clamp(style.framePadding, 0f, 8f) / 32f);
        frame.sizeDelta = new Vector2(side / Mathf.Max(.0001f, Mathf.Abs(scale.x)),
            side / Mathf.Max(.0001f, Mathf.Abs(scale.y)));
        frame.SetAsLastSibling();
        labels.Clear();
        target.GetComponentsInChildren<TMP_Text>(true, labels);
        foreach (var label in labels)
            if (label.transform.parent == target.transform) label.transform.SetAsLastSibling();
        frame.gameObject.SetActive(true);
    }

    private void PlayHoverSound()
    {
        if (style.hoverSound == null || style.hoverVolume <= 0f) return;
        if (hoverAudio == null) return;
        // Never cut or overlap a tick. Fast sweeps keep one smooth sound instead of summing transients.
        double now = AudioSettings.dspTime;
        if (now < lastSoundTime) lastSoundTime = double.NegativeInfinity;
        if (hoverAudio.isPlaying || now - lastSoundTime < style.hoverSound.length + .002) return;
        lastSoundTime = now;
        hoverAudio.volume = 1f;
        hoverAudio.PlayOneShot(style.hoverSound, Mathf.Clamp01(style.hoverVolume));
    }

    private bool CreateFrame()
    {
        if (style == null) style = Resources.Load<InventorySelectionStyle>("InventorySelectionStyle");
        if (style == null || style.topLeft == null || style.topRight == null ||
            style.bottomLeft == null || style.bottomRight == null) return false;
        var root = new GameObject("HoverSelectionFrame", typeof(RectTransform), typeof(LayoutElement));
        frame = root.GetComponent<RectTransform>();
        root.GetComponent<LayoutElement>().ignoreLayout = true;
        Corner(style.topLeft, new Vector2(0, 1));
        Corner(style.topRight, Vector2.one);
        Corner(style.bottomLeft, Vector2.zero);
        Corner(style.bottomRight, new Vector2(1, 0));
        return true;
    }

    private void Corner(Sprite sprite, Vector2 corner)
    {
        var image = new GameObject(sprite.name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(frame, false);
        image.sprite = sprite;
        image.raycastTarget = false;
        // The artwork is drawn for a 32px slot: preserve its pixel proportions at any UI scale.
        var size = sprite.rect.size / (32f + 2f * Mathf.Clamp(style.framePadding, 0f, 8f));
        image.rectTransform.anchorMin = new Vector2(corner.x == 0 ? 0 : 1 - size.x,
            corner.y == 0 ? 0 : 1 - size.y);
        image.rectTransform.anchorMax = new Vector2(corner.x == 0 ? size.x : 1,
            corner.y == 0 ? size.y : 1);
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        hoveredTarget = null;
        if (frame != null) frame.gameObject.SetActive(false);
        if (hoverAudio != null) hoverAudio.Stop();
    }

    private void OnDestroy()
    {
        if (frame != null) Destroy(frame.gameObject);
        if (instance == this) instance = null;
    }
}

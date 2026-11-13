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
    private RectTransform keyboardTarget;
    private Transform navigationPanel;
    private EventSystem navigationEvents;
    private bool previousNavigation;
    private Vector2 lastPointer;
    private bool keyboardMode;
    private readonly List<Component> candidates = new List<Component>();
    private float inset;
    private float pressUntil;
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
        if (instance != null && !instance.keyboardTarget && Application.isFocused) instance.Show(target);
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
        Navigate(target);
        if (keyboardTarget)
        {
            var keyboardTooltip = keyboardTarget.GetComponent<ItemTooltipTarget>();
            Draw(keyboardTarget, keyboardTooltip);
        }
        else Show(target);
    }

    private void Navigate(ItemTooltipTarget hover)
    {
        var panel = InventoryPanelSession.CurrentPanel;
        var mouse = Mouse.current;
        Vector2 pointer = mouse != null ? mouse.position.ReadValue() : lastPointer;
        bool mouseUsed = mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0f || pointer != lastPointer ||
            mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.scroll.ReadValue() != Vector2.zero);
        if (mouseUsed && Application.isFocused) keyboardMode = false;
        if (panel != navigationPanel || !Application.isFocused || mouseUsed)
            ClearKeyboardFocus();
        navigationPanel = panel;
        lastPointer = pointer;
        // Inventory handles submission itself so Unity's default Submit cannot click a shop button twice.
        var events = panel && Application.isFocused ? EventSystem.current : null;
        if (navigationEvents != events)
        {
            if (navigationEvents) navigationEvents.sendNavigationEvents = previousNavigation;
            navigationEvents = events;
            if (events) { previousNavigation = events.sendNavigationEvents; events.sendNavigationEvents = false; }
        }
        var keys = Keyboard.current;
        if (keys == null || Time.timeScale <= 0 || !Application.isFocused) return;
        Vector2 direction = Vector2.zero;
        if (keys.leftArrowKey.wasPressedThisFrame) direction = Vector2.left;
        else if (keys.rightArrowKey.wasPressedThisFrame) direction = Vector2.right;
        else if (keys.upArrowKey.wasPressedThisFrame) direction = Vector2.up;
        else if (keys.downArrowKey.wasPressedThisFrame) direction = Vector2.down;
        bool submit = keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame;
        if (direction != Vector2.zero || (events && submit)) keyboardMode = true;
        if (!events || !keyboardMode) return;
        bool initialize = !keyboardTarget || !InventoryWorkspace.IsNavigationTarget(keyboardTarget);
        if (!initialize && direction == Vector2.zero && !submit) return;
        if (initialize) Canvas.ForceUpdateCanvases();
        candidates.Clear();
        foreach (var slot in FindObjectsByType<Slot>(FindObjectsSortMode.None))
            if (InventoryWorkspace.IsNavigationTarget(slot.transform)) candidates.Add(slot);
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (button.IsInteractable() && InventoryWorkspace.IsNavigationTarget(button.transform)) candidates.Add(button);
        var origin = initialize ? null : keyboardTarget;
        if (initialize && direction == Vector2.zero && !submit)
        {
            origin = InventoryWorkspace.FirstHotbarSlot();
            if (!origin)
            {
                var product = panel.GetComponentInChildren<MarketIngredientSlot>();
                if (product) origin = (RectTransform)product.transform;
            }
        }
        if ((direction != Vector2.zero || submit) && !origin && hover && InventoryWorkspace.IsNavigationTarget(hover.transform)) origin = hover.transform as RectTransform;
        RectTransform next = null;
        float best = float.PositiveInfinity;
        foreach (var candidate in candidates)
        {
            var rect = (RectTransform)candidate.transform;
            Vector2 center = rect.TransformPoint(rect.rect.center);
            float score;
            if (!origin) score = (Screen.height - center.y) * Screen.width + center.x;
            else if (direction == Vector2.zero) { if (rect != origin) continue; score = 0; }
            else
            {
                var delta = center - (Vector2)origin.TransformPoint(origin.rect.center);
                float forward = Vector2.Dot(delta, direction);
                float sideways = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
                if (forward < 1f || sideways > forward) continue;
                score = forward + 3f * sideways;
            }
            if (score < best) { best = score; next = rect; }
        }
        keyboardTarget = next ? next : origin;
        if (!keyboardTarget) return;
        InventoryStackCursor.AnchorTo(keyboardTarget);
        if (submit)
        {
            pressUntil = Time.unscaledTime + .09f;
            if (keyboardTarget.TryGetComponent<Slot>(out var slot))
            {
                if ((keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed) && !InventoryStackCursor.IsHolding)
                    InventoryWorkspace.QuickMove(slot);
                else InventoryStackCursor.ActivateSlot(slot);
            }
            else if (keyboardTarget.TryGetComponent<Button>(out var button)) button.onClick.Invoke();
        }
    }

    private void ClearKeyboardFocus()
    {
        keyboardTarget = null;
        InventoryStackCursor.AnchorTo(null);
    }

    private void Show(ItemTooltipTarget target)
    {
        if (target == null || !target.isActiveAndEnabled || !target.ShowInventorySelection || !(target.transform is RectTransform))
        {
            hoveredTarget = null;
            if (frame != null) frame.gameObject.SetActive(false);
            return;
        }
        Draw((RectTransform)target.transform, target);
    }

    private void Draw(RectTransform slot, ItemTooltipTarget target)
    {
        if (frame == null && !CreateFrame()) return;
        if (frame.parent != slot)
        {
            hoveredTarget = target;
            inset = 0f;
            if (!keyboardTarget) pressUntil = 0f;
            PlayHoverSound();
        }
        if (frame.parent != slot)
        {
            frame.SetParent(slot, false);
            frame.anchorMin = frame.anchorMax = new Vector2(.5f, .5f);
            frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition = Vector2.zero;
            frame.localScale = Vector3.one;
            frame.localRotation = Quaternion.identity;
        }
        // A square in rendered space, even when a hotbar ancestor scales X and Y differently.

        var scale = slot.lossyScale;
        float side = slot.rect.width * Mathf.Abs(scale.x) *
            (1f + 2f * Mathf.Clamp(style.framePadding, 0f, 8f) / 32f);
        var mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            pressUntil = Time.unscaledTime + .065f;
        bool pressing = Time.unscaledTime < pressUntil || (keyboardTarget ? Keyboard.current != null && (Keyboard.current.enterKey.isPressed || Keyboard.current.numpadEnterKey.isPressed) : mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed));
        inset = Mathf.MoveTowards(inset, pressing ? 1f : 0f, Time.unscaledDeltaTime / (pressing ? .055f : .09f));
        side *= 1f - .12f * Mathf.SmoothStep(0f, 1f, inset);
        frame.sizeDelta = new Vector2(side / Mathf.Max(.0001f, Mathf.Abs(scale.x)),
            side / Mathf.Max(.0001f, Mathf.Abs(scale.y)));
        frame.SetAsLastSibling();
        labels.Clear();
        slot.GetComponentsInChildren<TMP_Text>(true, labels);
        foreach (var label in labels)
            if (label.transform.parent == slot) label.transform.SetAsLastSibling();
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
        ClearKeyboardFocus();
        if (navigationEvents) navigationEvents.sendNavigationEvents = previousNavigation;
        if (frame != null) Destroy(frame.gameObject);
        if (instance == this) instance = null;
    }
}

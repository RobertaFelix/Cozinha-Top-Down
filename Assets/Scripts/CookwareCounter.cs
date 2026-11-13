using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public abstract class CookwareCounter : MonoBehaviour
{
    [SerializeField] private HotbarController hotbarController;
    public abstract GameObject InitialItem { get; }
    private GameObject stored;
    private bool initialized;
    private Collider2D player;
    private static int lastInteraction = -1;
    public GameObject Stored => initialized ? stored : InitialItem;
    public static bool InteractPressed => (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

    private SpriteRenderer display;
    protected void Awake() => Store(Stored);
    public void Store(GameObject item)
    {
        initialized = true;
        stored = item;
        if (!display)
        {
            var original = GetComponent<SpriteRenderer>();
            display = new GameObject("Stored utensil").AddComponent<SpriteRenderer>();
            display.transform.SetParent(transform, false);
            display.sortingLayerID = original.sortingLayerID;
            display.sortingOrder = original.sortingOrder;
            display.sharedMaterial = original.sharedMaterial;
            original.enabled = false;
        }
        display.sprite = item ? item.GetComponent<Image>().sprite : null;
        if (display.sprite)
        {
            var anchor = InitialItem.GetComponent<Image>().sprite.bounds.center;
            display.transform.localPosition = anchor - display.sprite.bounds.center;
        }
    }

    protected void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player")) player = other;
    }

    protected void Update()
    {
        if (Time.timeScale <= 0 || InventoryPanelSession.HasOpenPanel ||
            InventoryStackCursor.IsHolding || ItemTooltipView.IsDragging || lastInteraction == Time.frameCount ||
            !player || !GetComponent<Collider2D>().IsTouching(player)) return;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
            Camera.main && Stove.CanClickWorld();
        if (!click && !InteractPressed) return;
        Vector2 target = click ? (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) : (Vector2)player.bounds.center;
        CookwareCounter nearest = null;
        float best = float.PositiveInfinity;
        foreach (var spot in FindObjectsByType<CookwareCounter>(FindObjectsSortMode.None))
        {
            var area = spot.GetComponent<Collider2D>();
            if (!spot.enabled || !area.IsTouching(player)) continue;
            if (click && !area.OverlapPoint(target) && !(spot.display && spot.Stored &&
                spot.display.bounds.Contains(new Vector3(target.x, target.y, spot.display.bounds.center.z)))) continue;
            float distance = ((Vector2)area.bounds.center - target).sqrMagnitude;
            if (distance < best) { best = distance; nearest = spot; }
        }
        if (nearest != this) return;
        lastInteraction = Time.frameCount;
        if (Stored)
        {
            if (hotbarController.AddItem(new InventoryItem { itemPrefab = Stored, amount = 1 })) Store(null);
            return;
        }
        var slot = hotbarController.EquippedSlot;
        var item = slot?.inventoryItem?.itemPrefab;
        if (!item || !item.TryGetComponent<CookwareItem>(out var tool) || tool.isPlate) return;
        Stove.ConsumeOne(slot);
        Store(item);
    }
}

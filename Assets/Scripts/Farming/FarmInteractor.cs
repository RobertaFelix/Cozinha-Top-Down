using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public sealed class FarmInteractor : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float interactionRange = 1.75f;
    [SerializeField] private Sprite[] wateringFrames;
    [SerializeField, Min(.01f)] private float wateringFrameSeconds = .1f;
    [SerializeField] private HotbarController hotbar;
    private SceneTransitionController scenes;
    private EnvironmentTransition environment;
    private EventSystem pointerEvents;
    private PointerEventData pointerData;
    private readonly System.Collections.Generic.List<RaycastResult> uiHits = new System.Collections.Generic.List<RaycastResult>();
    private PlayerController movement;
    private Animator animator;
    private SpriteRenderer actor;
    private Rigidbody2D body;
    private bool busy, previousMovement, previousAnimator, previousFlip;
    private Sprite previousSprite;
    private Transform marker;
    private InventorySelectionStyle markerStyle;

    private void Awake()
    {
        movement = GetComponent<PlayerController>(); animator = GetComponent<Animator>();
        actor = GetComponent<SpriteRenderer>(); body = GetComponent<Rigidbody2D>();
    }
    private void ResolveHotbar()
    {
        if (hotbar != null && hotbar.slotCount > 0 && hotbar.hotbarPanel != null) return;
        hotbar = null;
        foreach (var candidate in Object.FindObjectsByType<HotbarController>(FindObjectsSortMode.None))
            if (candidate.slotCount > 0 && candidate.hotbarPanel != null && candidate.isActiveAndEnabled)
            {
                hotbar = candidate;
                break;
            }
    }
    private bool CanAct()
    {
        var saves = SaveGameService.Instance;
        if (scenes == null) scenes = Object.FindAnyObjectByType<SceneTransitionController>();
        if (environment == null) environment = Object.FindAnyObjectByType<EnvironmentTransition>();
        return !busy && Time.timeScale > 0 && (saves == null || saves.IsReady) && !InventoryPanelSession.HasOpenPanel &&
            !ItemTooltipView.IsDragging && !InventoryStackCursor.IsHolding && (scenes == null || !scenes.IsLoading) &&
            (environment == null || !environment.IsTransitioning);
    }
    private bool PointerOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        if (pointerEvents != EventSystem.current || pointerData == null)
        {
            pointerEvents = EventSystem.current;
            pointerData = new PointerEventData(pointerEvents);
        }
        pointerData.Reset(); pointerData.position = position; uiHits.Clear();
        pointerEvents.RaycastAll(pointerData, uiHits);
        foreach (var hit in uiHits) if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }
    private void Update()
    {
        if (!FarmArea.HasActiveArea) { ShowMarker(null, default); return; }
        ResolveHotbar();
        var mouse = Mouse.current;
        bool valid = CanAct() && mouse != null && Camera.main != null &&
            !PointerOverUI(mouse.position.ReadValue());
        FarmArea area = null; Vector3Int cell = default;
        if (valid)
        {
            var world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue()); world.z = 0;
            valid = FarmArea.TryFind(world, out area, out cell) && InRange(area, cell);
        }
        ShowMarker(valid && hotbar != null && hotbar.EquippedSlot != null ? area : null, cell);
        if (!valid) return;
        if (mouse.leftButton.wasPressedThisFrame) TryUseAt(area, cell);
        else if (mouse.rightButton.wasPressedThisFrame) TryHarvestAt(area, cell);
    }
    private bool InRange(FarmArea area, Vector3Int cell) => area != null && area.CanPlantAt(cell) &&
        Vector2.Distance(transform.position, area.CellCenter(cell)) <= interactionRange;

    public bool TryUseAt(FarmArea area, Vector3Int cell)
    {
        if (!CanAct() || !InRange(area, cell) || FarmingSaveModule.Instance == null) return false;
        ResolveHotbar();
        var slot = hotbar != null ? hotbar.EquippedSlot : null;
        if (slot?.inventoryItem?.itemPrefab == null) return false;
        var farm = FarmingSaveModule.Instance;
        var seed = slot.inventoryItem.itemPrefab.GetComponent<SeedItem>();
        if (seed != null)
        {
            if (!farm.TryPlant(area, cell, seed.Crop))
            {
                MarketHoursNotice.Show(farm.GetPlot(area.AreaId, cell) == null ? "Prepare this plot with the hoe first." : "This plot already has a crop.");
                return false;
            }
            slot.inventoryItem.amount--;
            if (slot.inventoryItem.amount <= 0)
            {
                slot.currentItem.SetActive(false); Destroy(slot.currentItem);
                slot.currentItem = null; slot.inventoryItem = null;
            }
            slot.UpdateQuantityDisplay();
            return true;
        }
        var tool = slot.inventoryItem.itemPrefab.GetComponent<FarmToolItem>();
        if (tool == null) return false;
        if (tool.Kind == FarmToolKind.Hoe) return farm.TryTill(area, cell);
        if (!farm.TryWater(area, cell))
        {
            if (farm.GetPlot(area.AreaId, cell) == null) MarketHoursNotice.Show("Prepare this plot with the hoe first.");
            return false;
        }
        StartCoroutine(Water(area.CellCenter(cell)));
        return true;
    }
    public bool TryHarvestAt(FarmArea area, Vector3Int cell)
    {
        return CanAct() && InRange(area, cell) && FarmingSaveModule.Instance != null &&
            FarmingSaveModule.Instance.TryHarvest(area, cell, Object.FindAnyObjectByType<InventoryController>());
    }
    private IEnumerator Water(Vector3 target)
    {
        if (wateringFrames == null || wateringFrames.Length == 0 || actor == null) yield break;
        busy = true; previousMovement = movement.enabled; previousAnimator = animator != null && animator.enabled;
        previousSprite = actor.sprite; previousFlip = actor.flipX;
        movement.enabled = false;
        if (body != null) body.linearVelocity = Vector2.zero;
        if (animator != null) animator.enabled = false;
        actor.flipX = target.x < transform.position.x;
        foreach (var frame in wateringFrames)
        {
            if (frame != null) actor.sprite = frame;
            yield return new WaitForSeconds(wateringFrameSeconds);
        }
        RestoreActor();
    }
    private void RestoreActor()
    {
        if (!busy) return;
        if (actor != null) { actor.sprite = previousSprite; actor.flipX = previousFlip; }
        if (animator != null) animator.enabled = previousAnimator;
        if (movement != null) movement.enabled = previousMovement;
        busy = false;
    }
    private void OnDisable()
    {
        StopAllCoroutines(); RestoreActor();
        if (marker != null) marker.gameObject.SetActive(false);
    }
    private void OnDestroy() { if (marker != null) Destroy(marker.gameObject); }
    private void ShowMarker(FarmArea area, Vector3Int cell)
    {
        if (area == null) { if (marker != null) marker.gameObject.SetActive(false); return; }
        if (marker == null)
        {
            markerStyle = Resources.Load<InventorySelectionStyle>("InventorySelectionStyle");
            if (markerStyle == null) return;
            marker = new GameObject("Planting Target").transform;
            Corner(markerStyle.topLeft, new Vector2(-.5f, .5f));
            Corner(markerStyle.topRight, new Vector2(.5f, .5f));
            Corner(markerStyle.bottomLeft, new Vector2(-.5f, -.5f));
            Corner(markerStyle.bottomRight, new Vector2(.5f, -.5f));
        }
        marker.position = area.CellCenter(cell);
        marker.gameObject.SetActive(true);
    }
    private void Corner(Sprite sprite, Vector2 corner)
    {
        if (sprite == null) return;
        var r = new GameObject("Corner", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        r.transform.SetParent(marker, false);r.sprite = sprite;r.sortingOrder = 10;
        var half = sprite.bounds.extents;
        r.transform.localPosition = new Vector3(corner.x - Mathf.Sign(corner.x) * half.x, corner.y - Mathf.Sign(corner.y) * half.y, 0) - sprite.bounds.center;
    }
}

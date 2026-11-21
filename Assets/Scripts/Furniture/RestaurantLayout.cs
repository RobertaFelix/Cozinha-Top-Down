using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;

[Serializable]
public sealed class RestaurantLayoutData
{
    public int version;
    public List<FurniturePlacement> pieces = new List<FurniturePlacement>();
}

[Serializable]
public sealed class FurniturePlacement
{
    public string id, support, purchasedType;
    public bool placed;
    public Vector3 localPosition;
}

// Scene instances retain their interactive components, stock, recipes and seat references.
public sealed class RestaurantLayout : MonoBehaviour
{
    public sealed class Piece
    {
        public string id, label, category;
        public Transform root;
        internal Transform originalParent;
        public SpriteRenderer visual;
        public Vector2 span, footprintOffset;
        public Vector3 anchorOffset, initialLocalPosition;
        public bool tabletop, acceptsItem, stock, wallMounted;
        public string support, purchasedType;
        public bool Placed => root.gameObject.activeSelf;
        public Vector3 Anchor => root.position + anchorOffset;
        public Rect Footprint(Vector3 anchor) => new Rect((Vector2)anchor + footprintOffset - span * .5f, span);
    }

    private readonly List<Piece> pieces = new List<Piece>();
    public IReadOnlyList<Piece> Pieces => pieces;
    public Vector2 ClampView(Vector2 point)
    {
        var min = floor.CellToWorld(floor.cellBounds.min);
        var max = floor.CellToWorld(floor.cellBounds.max);
        return new Vector2(Mathf.Clamp(point.x,min.x,max.x), Mathf.Clamp(point.y,min.y,max.y));
    }
    private Tilemap floor;
    private Collider2D[] borders;
    private Transform entrance;
    private SpriteRenderer menuBoard;
    private Vector3 legacyCounterPosition, legacyStovePosition;
    private readonly Dictionary<string,string> legacySupports = new Dictionary<string,string>();
    private static readonly Dictionary<string, Sprite> moduleSprites = new Dictionary<string, Sprite>();
    private bool initialized;
    private int actorOrder, actorLayer;
    private readonly Dictionary<string, SpriteRenderer> applianceTemplates = new Dictionary<string, SpriteRenderer>();

    public static RestaurantLayout FindLayout()
    {
        foreach (var grid in FindObjectsByType<Grid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (grid.name == "GridRestaurant")
            {
                var layout = grid.GetComponent<RestaurantLayout>();
                if (!layout) layout = grid.gameObject.AddComponent<RestaurantLayout>();
                layout.Initialize();
                return layout;
            }
        return null;
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        var actor = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include).GetComponent<SpriteRenderer>();
        actorOrder = actor.sortingOrder;
        actorLayer = actor.sortingLayerID;
        floor = transform.Find("TileBackGroundFloor").GetComponent<Tilemap>();
        borders = transform.Find("TilemapBordes").GetComponentsInChildren<Collider2D>(true);
        var kitchen = transform.Find("Kitchen");
        entrance = kitchen.Find("menu/InteriorDoorRestaurant");
        menuBoard = kitchen.Find("menu").GetComponent<SpriteRenderer>();
        var stove = kitchen.Find("stove");
        var counter = kitchen.Find("counter");
        legacyCounterPosition = counter.localPosition;
        legacyStovePosition = stove.localPosition;
        // The old scene parents the two pickup points to one wide counter.
        foreach (string name in new[] { "pot", "pan" })
            counter.Find(name)?.SetParent(kitchen, true);
        SplitKitchenRuns(kitchen, counter);
        foreach (string name in new[] { "stove", "vegetables", "frizer", "refrigerator", "deep fryer", "trash", "fire extinguisher",
                     "coffee maker", "mixer", "microwave", "blender", "dishes", "pot", "pan", "cutting board" })
        {
            var t = kitchen.Find(name);
            if (!t || !t.TryGetComponent<SpriteRenderer>(out var sr)) continue;
            bool top = name == "coffee maker" || name == "mixer" || name == "microwave" || name == "blender" ||
                       name == "dishes" || name == "pot" || name == "pan" || name == "cutting board";
            string label = name == "frizer" ? "Freezer" : name == "vegetables" ? "Produce counter" :
                name == "pot" ? "Pot stand" : name == "pan" ? "Pan stand" : name == "dishes" ? "Plate stack" : name;
            var p = Register("kitchen/" + name, t, sr, label,
                name == "vegetables" ? "Construction" : name == "trash" || name == "fire extinguisher" ? "Utility" : "Kitchen", top);
            if (top)
            {
                bool front = name == "dishes" || name == "pot" || name == "pan" || name == "cutting board";
                Piece closest = null;
                foreach (var candidate in pieces)
                {
                    if (!candidate.acceptsItem || candidate.id.StartsWith("kitchen/back counter", StringComparison.Ordinal) == front) continue;
                    if (closest == null || Mathf.Abs(p.Anchor.x-candidate.Anchor.x) < Mathf.Abs(p.Anchor.x-closest.Anchor.x))
                        closest = candidate;
                }
                if (closest != null)
                {
                    p.support = closest.id;
                    if (!front) legacySupports["bay/" + name] = closest.id;
                }
            }
        }
        foreach (Table table in GetComponentsInChildren<Table>(true))
        {
            var t = table.transform;
            var surface = t.Find("table");
            if (!surface) continue;
            var p = Register("dining/" + t.name, t, surface.GetComponent<SpriteRenderer>(), "Dining table", "Utility", false);
            var bounds = ArtBounds(surface.GetComponent<SpriteRenderer>());
            foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sprite) bounds.Encapsulate(ArtBounds(sr));
            p.span = new Vector2(bounds.size.x, bounds.size.y) * .95f;
            p.footprintOffset = bounds.center - p.Anchor;
        }
        var catalog = Resources.Load<RestaurantFurnitureCatalog>("RestaurantFurnitureCatalog");
        foreach (var entry in catalog.entries) AddStock(entry);
        foreach (var offer in RestaurantApplianceOffer.All)
        {
            if (offer.id == "sink")
            {
                applianceTemplates[offer.id] = pieces.Find(p => p.id == "kitchen/sink").visual;
                continue;
            }
            var t = kitchen.Find(offer.id);
            if (!t || !t.TryGetComponent<SpriteRenderer>(out var art)) continue;
            applianceTemplates.Add(offer.id, art);
            var id = "kitchen/" + offer.id;
            var authored = pieces.Find(p => p.id == id);
            if (authored != null) authored.label = offer.label;
            KitchenAppliance.Install(t.gameObject, offer.id, id);
        }
        // Install components before attaching countertop objects to their visual sorting group.
        foreach (var piece in pieces)
            if (piece.tabletop && piece.support != null) AttachToCounter(piece);
    }

    private void SplitKitchenRuns(Transform kitchen, Transform counter)
    {
        var back = kitchen.Find("sink").GetComponent<SpriteRenderer>();
        var front = counter.GetComponent<SpriteRenderer>();
        var cabinets = kitchen.Find("range hood").GetComponent<SpriteRenderer>();
        // Pixel intervals in cozinha_4. The existing standalone stove occupies [61, 78).
        AddModule(back, 0, 25, "back counter 1", "Back counter", true);
        AddModule(back, 25, 44, "back counter 2", "Back counter", true);
        AddModule(back, 44, 61, "back counter 3", "Back counter", true);
        AddModule(back, 78, 98, "back counter 4", "Back counter", true);
        AddModule(back, 98, 124, "sink", "Sink", false);
        int[] ends = { 0, 24, 44, 64, 85, 109 };
        for (int i = 0; i < ends.Length-1; i++)
            AddModule(front, ends[i], ends[i+1], i == 0 ? "counter" : "front counter " + (i+1), "Serving counter", true);
        int[] cabinetEdges = { 0, 16, 31, 46, 61, 77, 92, 107, 124 };
        for (int i = 0; i < cabinetEdges.Length-1; i++)
        {
            bool hood = i == 4;
            AddModule(cabinets, cabinetEdges[i], cabinetEdges[i+1], hood ? "range hood" : "wall cabinet " + (i+1),
                hood ? "Range hood" : "Wall cabinet", false, true, hood ? 0 : 10);
        }
        foreach (var old in new[] { back, front, cabinets }) old.gameObject.SetActive(false);
    }

    private void AddModule(SpriteRenderer source, int start, int end, string key, string label, bool acceptsItem, bool wall = false, int trimBottom = 0)
    {
        var original = source.sprite;
        var rect = original.rect;
        var area = new Rect(rect.x + start, rect.y + trimBottom, end - start, rect.height - trimBottom);
        if (!moduleSprites.TryGetValue(key, out var sprite) || !sprite)
        {
            sprite = Sprite.Create(original.texture, area, Vector2.one * .5f, original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = label;
            moduleSprites[key] = sprite;
        }
        var art = new GameObject(label).AddComponent<SpriteRenderer>();
        art.transform.SetParent(source.transform.parent, false);
        var pixelCenter = new Vector2((start + end) * .5f, (rect.height + trimBottom) * .5f);
        art.transform.position = source.transform.TransformPoint((pixelCenter - original.pivot) / original.pixelsPerUnit);
        art.transform.localScale = source.transform.localScale;
        art.sharedMaterial = source.sharedMaterial;
        art.sortingLayerID = source.sortingLayerID;
        art.sortingOrder = source.sortingOrder;
        art.sprite = sprite;
        var piece = Register("kitchen/" + key, art.transform, art, label, "Construction", false, wall);
        piece.acceptsItem = acceptsItem;
    }

    // Keep stored module icons available in the furniture tab when the village scene unloads.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetModuleSprites()
    {
        foreach (var sprite in moduleSprites.Values) if (sprite) Destroy(sprite);
        moduleSprites.Clear();
    }

    // The isolated tutorial owns its own layout. Removed entries stay out of both the room and the inventory.
    public void RestrictToStarterSet(Func<Piece, bool> keep)
    {
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (keep(pieces[i])) continue;
            pieces[i].root.gameObject.SetActive(false);
            pieces.RemoveAt(i);
        }
        FurnitureInventoryPage.Remember(this);
    }

    private static Bounds ArtBounds(SpriteRenderer renderer)
    {
        var art = renderer.sprite.bounds;
        return new Bounds(renderer.transform.TransformPoint(art.center), Vector3.Scale(art.size,renderer.transform.lossyScale));
    }

    private Piece Register(string id, Transform t, SpriteRenderer sr, string label, string category, bool top, bool wall = false)
    {
        var art = sr.sprite.bounds;
        var span = Vector2.Scale((Vector2)art.size, (Vector2)sr.transform.lossyScale);
        BoxCollider2D solid = null;
        foreach (var box in t.GetComponents<BoxCollider2D>())
        {
            // Interaction triggers have their own reach. A tabletop item's support supplies its solid.
            if (box.isTrigger) continue;
            if (top || wall) box.enabled = false;
            else if (!solid) solid = box;
        }

        if (!top && !wall && !t.GetComponent<Table>())
        {
            if (!solid) solid = t.gameObject.AddComponent<BoxCollider2D>();
            var pixel = 1f / sr.sprite.pixelsPerUnit;
            // Short counters occupy their entire work surface. Tall pieces leave a walkable area behind them.
            float depth = Mathf.Min(art.size.y, 1.15f);
            solid.size = new Vector2(Mathf.Max(pixel, art.size.x - pixel), depth);
            solid.offset = new Vector2(art.center.x, art.min.y - pixel + depth * .5f);
            var group = t.gameObject.AddComponent<SortingGroup>();
            group.sortingLayerID = actorLayer;
            group.sortingOrder = actorOrder;
            sr.sortingOrder = 0;
        }
        if (solid) span = Vector2.Scale(solid.size, (Vector2)t.lossyScale);
        var p = new Piece { id = id, label = char.ToUpperInvariant(label[0]) + label.Substring(1), root = t, originalParent = t.parent, visual = sr,
            initialLocalPosition = t.localPosition, category = category, tabletop = top, wallMounted = wall, span = span,
            anchorOffset = sr.transform.TransformPoint(art.center) - t.position };
        if (solid) p.footprintOffset = (Vector2)(t.TransformPoint(solid.offset) - p.Anchor);
        pieces.Add(p);
        return p;
    }

    private void AttachToCounter(Piece item)
    {
        var counter = pieces.Find(other => other.id == item.support && other.acceptsItem);
        item.root.SetParent(counter != null ? counter.root : item.originalParent, true);
        if (counter == null) return;
        item.visual.sortingLayerID = counter.visual.sortingLayerID;
        item.visual.sortingOrder = counter.visual.sortingOrder + 1;

        if (!item.root.GetComponent<CookwareCounter>() && !item.root.GetComponent<PlateCounter>()) return;
        // A physical worktop separates the player from the utensil. Keep E reachable at both counter faces.
        var reach = Array.Find(item.root.GetComponents<BoxCollider2D>(), collider => collider.isTrigger);
        if (!reach) reach = item.root.gameObject.AddComponent<BoxCollider2D>();
        reach.isTrigger = true;
        var footprint = counter.Footprint(counter.Anchor);
        var front = item.root.InverseTransformPoint(new Vector3(item.Anchor.x,footprint.yMin - .45f));
        var back = item.root.InverseTransformPoint(new Vector3(item.Anchor.x,footprint.yMax + .45f));
        reach.offset = (front + back) * .5f;
        reach.size = new Vector2(Mathf.Min(1f,footprint.width) / item.root.lossyScale.x, back.y-front.y);
    }

    private void AddStock(RestaurantFurnitureCatalog.Entry entry)
    {
        for (int i = 0; i < entry.startingCount; i++)
        {
            var t = new GameObject(entry.displayName).transform;
            t.SetParent(transform, false);
            var sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = entry.sprite;
            sr.sortingLayerID = pieces[0].visual.sortingLayerID;
            sr.sharedMaterial = pieces[0].visual.sharedMaterial;
            sr.sortingOrder = pieces[0].visual.sortingOrder;
            var p = Register("stock/" + entry.id + "/" + i, t, sr, entry.displayName, entry.category, entry.requiresCounter);
            p.acceptsItem = entry.acceptsCountertopItem;
            p.stock = true;
            t.gameObject.SetActive(false);
        }
    }

    public Sprite GetApplianceIcon(string type) =>
        applianceTemplates.TryGetValue(type, out var template) ? template.sprite : null;

    public bool TryBuyAppliance(RestaurantApplianceOffer offer)
    {
        if (offer == null || !applianceTemplates.ContainsKey(offer.id) || pieces.Count >= 2047 ||
            !CoinManager.Instance || offer.price <= 0 || CoinManager.Instance.TotalCoins < offer.price) return false;
        var catalogOffer = Array.Find(RestaurantApplianceOffer.All, entry => entry.id == offer.id);
        if (catalogOffer == null || catalogOffer.price != offer.price) return false;

        var piece = CreatePurchasedAppliance(catalogOffer, "purchase/" + Guid.NewGuid().ToString("N"));
        if (!CoinManager.Instance.SpendCoins(catalogOffer.price))
        {
            RemovePurchased(piece);
            return false;
        }
        FurnitureInventoryPage.Remember(this);
        return true;
    }

    private Piece CreatePurchasedAppliance(RestaurantApplianceOffer offer, string id)
    {
        var template = applianceTemplates[offer.id];
        var root = new GameObject(offer.label);
        root.SetActive(false);
        // Keep the original world scale, independently of the counter the template is on.
        root.transform.localScale = template.transform.lossyScale;
        root.transform.SetParent(transform, true);
        root.transform.localPosition = Vector3.zero;
        var art = root.AddComponent<SpriteRenderer>();
        art.sprite = template.sprite;
        art.flipX = template.flipX;
        art.flipY = template.flipY;
        art.sharedMaterial = template.sharedMaterial;
        art.sortingLayerID = template.sortingLayerID;
        art.sortingOrder = template.sortingOrder;
        bool sink = offer.id == "sink";
        var piece = Register(id, root.transform, art, offer.label, sink ? "Construction" : "Kitchen", !sink && offer.id != "deep fryer");
        piece.stock = true;
        piece.purchasedType = offer.id;
        if (!sink) KitchenAppliance.Install(root, offer.id, id);
        return piece;
    }

    private void RemovePurchased(Piece piece)
    {
        pieces.Remove(piece);
        piece.root.gameObject.SetActive(false);
        Destroy(piece.root.gameObject);
    }

    public Piece Pick(Vector2 point)
    {
        Piece picked = null;
        foreach (var p in pieces)
        {
            if (!p.Placed) continue;
            var bounds = p.visual.bounds;
            if (!bounds.Contains(new Vector3(point.x, point.y, bounds.center.z)) && !p.Footprint(p.Anchor).Contains(point)) continue;
            if (picked == null || p.tabletop || (!picked.tabletop && p.Anchor.y < picked.Anchor.y)) picked = p;
        }
        return picked;
    }

    public bool Preview(Piece piece, Vector2 pointer, out Vector3 anchor, out Piece support)
    {
        support = null;
        anchor = floor.GetCellCenterWorld(floor.WorldToCell(pointer));
        if (piece.root.TryGetComponent<Table>(out var table) && table.HasOccupiedChair) return false;
        if (piece.tabletop)
        {
            foreach (var candidate in pieces)
                if (candidate.acceptsItem && candidate.Placed && new Rect(candidate.visual.bounds.min, candidate.visual.bounds.size).Contains(pointer))
                {
                    support = candidate;
                    anchor = candidate.Anchor + Vector3.up * .35f;
                    return !pieces.Exists(p => p != piece && p.Placed && p.support == candidate.id);
                }
            return false;
        }
        Rect footprint = piece.Footprint(anchor);
        Vector3Int min = floor.WorldToCell(footprint.min + Vector2.one*.015f);
        Vector3Int max = floor.WorldToCell(footprint.max - Vector2.one*.015f);
        for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++)
                if (!floor.HasTile(new Vector3Int(x,y,0))) return false;
        if (menuBoard && footprint.Overlaps(new Rect((Vector2)menuBoard.bounds.min, (Vector2)menuBoard.bounds.size))) return false;
        if (entrance && footprint.Overlaps(new Rect((Vector2)entrance.position - Vector2.one*.65f, Vector2.one*1.3f))) return false;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!piece.wallMounted && player && footprint.Overlaps(new Rect((Vector2)player.transform.position - Vector2.one*.36f, Vector2.one*.72f))) return false;
        foreach (var hit in Physics2D.OverlapBoxAll(footprint.center, footprint.size, 0))
            if (!piece.wallMounted && System.Array.IndexOf(borders, hit) >= 0 && !hit.isTrigger) return false;
        foreach (var p in pieces)
            if (p != piece && p.Placed && !p.tabletop && p.wallMounted == piece.wallMounted && footprint.Overlaps(p.Footprint(p.Anchor))) return false;
        return true;
    }

    public bool Place(Piece piece, Vector2 point)
    {
        if (!Preview(piece, point, out var anchor, out var support)) return false;
        var delta = anchor - piece.Anchor;
        delta.z = 0;
        piece.root.position += delta;
        piece.root.gameObject.SetActive(true);
        piece.support = support?.id;
        if (piece.tabletop) AttachToCounter(piece);
        Physics2D.SyncTransforms();
        return true;
    }

    public bool Store(Piece p)
    {
        if (p == null || !p.Placed || pieces.Exists(other => other.Placed && other.support == p.id)) return false;
        if (p.root.TryGetComponent<KitchenAppliance>(out var appliance) && !appliance.CanStore) return false;
        if (p.root.TryGetComponent<Table>(out var table) && table.HasOccupiedChair) return false;
        if (p.id == "kitchen/stove" && FindAnyObjectByType<StoveController>() is StoveController stove && stove.CurrentRecipe != null) return false;
        p.root.gameObject.SetActive(false);
        p.support = null;
        if (p.tabletop) AttachToCounter(p);
        return true;
    }

    public RestaurantLayoutData Capture()
    {
        var state = new RestaurantLayoutData { version = 2 };
        foreach (var p in pieces)
            state.pieces.Add(new FurniturePlacement { id = p.id, localPosition = p.originalParent.InverseTransformPoint(p.root.position), placed = p.Placed, support = p.support, purchasedType = p.purchasedType });
        return state;
    }

    private void MigrateKitchenRuns(RestaurantLayoutData data)
    {
        var oldCounter = data.pieces.Find(p => p.id == "kitchen/counter");
        var oldStove = data.pieces.Find(p => p.id == "kitchen/stove");
        foreach (var p in pieces)
        {
            if (p.id == "kitchen/counter" || p.id.StartsWith("kitchen/front counter", StringComparison.Ordinal) ||
                p.id == "kitchen/pot" || p.id == "kitchen/pan" || p.id == "kitchen/dishes" || p.id == "kitchen/cutting board")
            {
                if (oldCounter != null) p.root.localPosition = p.initialLocalPosition + oldCounter.localPosition - legacyCounterPosition;
            }
            else if (p.wallMounted || p.id == "kitchen/sink" || p.id.StartsWith("kitchen/back counter", StringComparison.Ordinal))
            {
                if (oldStove != null) p.root.localPosition = p.initialLocalPosition + oldStove.localPosition - legacyStovePosition;
            }
        }
    }

    public void Restore(RestaurantLayoutData data)
    {
        var savedPurchases = new HashSet<string>();
        foreach (var entry in data.pieces)
            if (!string.IsNullOrEmpty(entry.purchasedType)) savedPurchases.Add(entry.id);
        for (int i = pieces.Count - 1; i >= 0; i--)
            if (pieces[i].purchasedType != null && !savedPurchases.Contains(pieces[i].id)) RemovePurchased(pieces[i]);

        // Coordinates in the save stay relative to the authored parent regardless of the current support.
        foreach (var piece in pieces)
            if (piece.tabletop) piece.root.SetParent(piece.originalParent, true);
        if (data.version < 2) MigrateKitchenRuns(data);
        foreach (var entry in data.pieces)
        {
            var p = pieces.Find(candidate => candidate.id == entry.id);
            if (p == null && !string.IsNullOrEmpty(entry.purchasedType))
            {
                var offer = Array.Find(RestaurantApplianceOffer.All, item => item.id == entry.purchasedType);
                if (offer == null || !applianceTemplates.ContainsKey(offer.id))
                    throw new InvalidOperationException("Unknown saved kitchen appliance: " + entry.purchasedType);
                p = CreatePurchasedAppliance(offer, entry.id);
            }
            if (p == null) continue;
            if (data.version >= 2 || p.id != "kitchen/counter") p.root.localPosition = entry.localPosition;
            p.root.gameObject.SetActive(entry.placed);
            p.support = data.version < 2 && entry.support != null && legacySupports.TryGetValue(entry.support, out var migrated)
                ? migrated : entry.support;
        }
        foreach (var piece in pieces)
            if (piece.tabletop && piece.Placed) AttachToCounter(piece);
        Physics2D.SyncTransforms();
    }
}

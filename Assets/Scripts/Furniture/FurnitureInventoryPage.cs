using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class FurnitureInventoryPage : MonoBehaviour
{
    private const int PageSize = 16;
    private int currentPage;
    private readonly List<GameObject> rows = new List<GameObject>();

    public static RectTransform Install(TabController tabs)
    {
        var source = (RectTransform)tabs.pages[0].transform;
        var page = RestaurantFurnitureEditor.MakePanel(source.parent, "FurniturePage", source.sizeDelta);
        page.gameObject.SetActive(false);
        page.anchorMin = source.anchorMin;
        page.anchorMax = source.anchorMax;
        page.pivot = source.pivot;
        page.localScale = source.localScale;
        page.anchoredPosition = source.anchoredPosition;
        page.gameObject.AddComponent<FurnitureInventoryPage>();
        int index = tabs.pages.Length;
        var button = RestaurantFurnitureEditor.MakeButton(tabs.tabImages[0].transform.parent, "Furniture", Vector2.zero, () => tabs.ActivateTab(index));
        button.GetComponentInChildren<Text>().color = new Color32(73,45,42,255);
        System.Array.Resize(ref tabs.tabImages, index+1);
        System.Array.Resize(ref tabs.pages, index+1);
        tabs.tabImages[index] = (Image)button.targetGraphic;
        tabs.pages[index] = page.gameObject;
        return page;
    }

    internal sealed class StockRow
    {
        public string label;
        public Sprite sprite;
        public int stored;
    }

    private static readonly List<StockRow> cached = new List<StockRow>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => cached.Clear();

    internal static void Remember(RestaurantLayout layout)
    {
        cached.Clear();
        foreach (var piece in layout.Pieces)
        {
            if (piece.Placed) continue;
            var row = cached.Find(entry => entry.label == piece.label && entry.sprite == piece.visual.sprite);
            if (row == null)
            {
                row = new StockRow { label = piece.label, sprite = piece.visual.sprite };
                cached.Add(row);
            }
            row.stored++;
        }
    }

    private void OnEnable()
    {
        var layout = RestaurantLayout.FindLayout();
        if (layout) Remember(layout);
        currentPage = 0;
        ShowPage(layout);
    }

    private void ShowPage(RestaurantLayout layout)
    {
        foreach (var row in rows)
            if (row) { row.SetActive(false); Destroy(row); }
        rows.Clear();
        if (cached.Count == 0)
        {
            rows.Add(RestaurantFurnitureEditor.Label(transform,"No furniture in storage",24).gameObject);
            return;
        }
        int first = currentPage * PageSize;
        for (int i = first; i < Mathf.Min(cached.Count, first + PageSize); i++)
        {
            var p = cached[i];
            int x = (i-first)%4, y = (i-first)/4;
            var cell = RestaurantFurnitureEditor.MakePanel(transform,p.label,Vector2.zero);
            cell.anchorMin = new Vector2(.02f+x*.24f,.80f-y*.18f);
            cell.anchorMax = cell.anchorMin + new Vector2(.225f,.16f);
            cell.offsetMin = cell.offsetMax = Vector2.zero;
            cell.GetComponent<Image>().color = Color.white;
            if (layout && layout.gameObject.activeInHierarchy)
            {
                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = cell.GetComponent<Image>();
                button.onClick.AddListener(() => RestaurantFurnitureEditor.ChooseFromInventory(p.label,p.sprite));
            }
            var icon = new GameObject("Icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            icon.rectTransform.SetParent(cell,false);
            icon.rectTransform.anchorMin = new Vector2(.15f,.44f);
            icon.rectTransform.anchorMax = new Vector2(.85f,.95f);
            icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
            icon.sprite = p.sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var text = RestaurantFurnitureEditor.Label(cell,p.label+"\n"+p.stored+" stored",16);
            text.rectTransform.anchorMax = new Vector2(1,.47f);
            rows.Add(cell.gameObject);
        }
        if (cached.Count <= PageSize) return;
        void AddPageButton(string label, int direction, float x)
        {
            var button = RestaurantFurnitureEditor.MakeButton(transform,label,Vector2.zero,() =>
            {
                currentPage += direction;
                ShowPage(layout);
            });
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(x,.02f);
            rect.anchorMax = new Vector2(x+.14f,.15f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            button.interactable = direction < 0 ? first > 0 : first + PageSize < cached.Count;
            rows.Add(button.gameObject);
        }
        AddPageButton("<", -1, .32f);
        AddPageButton(">", 1, .54f);
    }
}

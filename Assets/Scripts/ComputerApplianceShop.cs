using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shares the computer's panel session, so escaping or walking away dismisses either page.
public sealed class ComputerApplianceShop
{
    private readonly List<GameObject> upgradeContents = new();
    private readonly RectTransform page;
    private readonly Button upgradesTab, appliancesTab, buy;
    private readonly RestaurantApplianceOffer[] offers;
    private readonly Button[] products;
    private readonly Image[] icons;
    private readonly Text message;
    private RestaurantLayout layout;
    private int selected = -1;

    public bool IsShowing => page.gameObject.activeSelf;

    public ComputerApplianceShop(RectTransform computer, UnityEngine.Events.UnityAction close)
    {
        foreach (Transform child in computer)
            if (child.gameObject.activeSelf) upgradeContents.Add(child.gameObject);

        page = RestaurantFurnitureEditor.MakePanel(computer, "Appliance shop", Vector2.zero);
        page.anchorMin = Vector2.zero;
        page.anchorMax = Vector2.one;
        page.offsetMin = page.offsetMax = Vector2.zero;

        upgradesTab = Tab(computer, "Upgrades", 150, () => Show(false));
        appliancesTab = Tab(computer, "Appliances", 408, () => Show(true));

        offers = RestaurantApplianceOffer.All;
        products = new Button[offers.Length];
        icons = new Image[offers.Length];
        var grid = new GameObject("Appliance catalog", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
        grid.SetParent(page, false);
        grid.anchorMin = new Vector2(.05f, .26f);
        grid.anchorMax = new Vector2(.95f, .85f);
        grid.offsetMin = grid.offsetMax = Vector2.zero;
        var group = grid.GetComponent<GridLayoutGroup>();
        group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        group.constraintCount = 4;
        group.cellSize = new Vector2(268, 175);
        group.spacing = new Vector2(28, 20);
        group.childAlignment = TextAnchor.MiddleCenter;
        for (int i = 0; i < offers.Length; i++)
        {
            int index = i;
            var offer = offers[i];
            var button = RestaurantFurnitureEditor.MakeButton(grid, offer.label, group.cellSize, () => Select(index));
            var label = button.GetComponentInChildren<Text>();
            label.rectTransform.anchorMax = new Vector2(1, .48f);
            label.text = offer.label + "\n" + offer.price + " coins";
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.rectTransform.SetParent(button.transform, false);
            icon.rectTransform.anchorMin = new Vector2(.2f, .52f);
            icon.rectTransform.anchorMax = new Vector2(.8f, .88f);
            icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icons[i] = icon;
            products[i] = button;
        }

        message = RestaurantFurnitureEditor.Label(page, "", 23);
        message.rectTransform.anchorMin = new Vector2(.04f, .05f);
        message.rectTransform.anchorMax = new Vector2(.61f, .22f);
        buy = RestaurantFurnitureEditor.MakeButton(page, "Buy", new Vector2(210, 86), Purchase);
        Anchor(buy, new Vector2(.72f, .13f), Vector2.zero);
        var cancel = RestaurantFurnitureEditor.MakeButton(page, "Close", new Vector2(200, 86), close);
        Anchor(cancel, new Vector2(.87f, .13f), Vector2.zero);

        page.gameObject.SetActive(false);
        upgradesTab.interactable = false;
    }

    private static Button Tab(Transform parent, string label, float x, UnityEngine.Events.UnityAction choose)
    {
        var tab = RestaurantFurnitureEditor.MakeButton(parent, label, new Vector2(250, 76), choose);
        Anchor(tab, Vector2.up, new Vector2(x, -45));
        return tab;
    }

    private static void Anchor(Button button, Vector2 anchor, Vector2 position)
    {
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
    }

    public void Show(bool appliances)
    {
        foreach (var content in upgradeContents) content.SetActive(!appliances);
        page.gameObject.SetActive(appliances);
        upgradesTab.interactable = appliances;
        appliancesTab.interactable = !appliances;
        if (!appliances) return;

        if (!layout) layout = RestaurantLayout.FindLayout();
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].sprite = layout ? layout.GetApplianceIcon(offers[i].id) : null;
            icons[i].enabled = icons[i].sprite != null;
        }
        ResetSelection();
    }

    private void Select(int index)
    {
        if (selected >= 0) products[selected].targetGraphic.color = Color.white;
        selected = index;
        products[index].targetGraphic.color = new Color32(239, 197, 119, 255);
        var offer = offers[index];
        bool affordable = CoinManager.Instance && CoinManager.Instance.TotalCoins >= offer.price;
        message.text = !layout ? "Kitchen unavailable." : affordable ?
            offer.label + "  ·  " + offer.price + " coins" : "Not enough coins for " + offer.label + ".";
        buy.interactable = layout && affordable;
    }

    private void Purchase()
    {
        if (!IsShowing || selected < 0 || !layout) return;
        var offer = offers[selected];
        if (layout.TryBuyAppliance(offer))
        {
            ResetSelection();
            message.text = offer.label + " added to Furniture.";
        }
        else
        {
            Select(selected);
            if (buy.interactable) message.text = "Purchase unavailable.";
        }
    }

    private void ResetSelection()
    {
        if (selected >= 0) products[selected].targetGraphic.color = Color.white;
        selected = -1;
        buy.interactable = false;
        message.text = "Choose an appliance.";
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public static class ItemTooltipCatalog
{
    private static readonly Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "batter", "Batter" }, { "flour", "Flour" }, { "butter", "Butter" },
        { "tomato", "Tomato" }, { "cheese", "Cheese" }, { "lettuce", "Lettuce" },
        { "carrot", "Carrot" }, { "potato", "Potato" }, { "beet", "Beetroot" },
        { "garlic", "Garlic" }, { "milk", "Milk" }, { "mustard", "Mustard" },
        { "ketchup", "Ketchup" }, { "egg01", "Raw Egg" }, { "egg02", "Fried Egg" },
        { "bread01", "Sliced Bread" }, { "bread02", "Bread" },
        { "Pan", "Cooking Pot" }, { "Pot", "Frying Pan" },
        { "yellow", "Yellow Potion" }, { "red", "Red Potion" },
        { "purple", "Purple Potion" }, { "pink", "Pink Potion" },
        { "orange", "Orange Potion" }, { "green", "Green Potion" },
        { "gray", "Gray Potion" }, { "cyan", "Cyan Potion" },
        { "brown", "Brown Potion" }, { "blue", "Blue Potion" }
    };

    private static readonly string[] upgradeNames =
    {
        "Faster Cooking", "Faster Movement", "Expanded Backpack", "More Patience",
        "Market Discount", "Magnifying Glass", "Harvesting Glove", "Village Key",
        "Upgrade 09", "Double Dish Prices", "Knife", "TV Advertising", "Magnet", "Upgrade 14"
    };

    private static readonly string[] upgradeDescriptions =
    {
        "Reduces recipe cooking time.", "Increases movement speed.",
        "Increases the stack limit for identical items from 10 to 99 per slot.", "Customers wait longer for their food.",
        "Reduces item prices at the market.", "More resources from enemies. Coming soon.",
        "Chance of a double harvest. Coming soon.", "Unlocks shop access outside opening hours. After-hours purchases cost more.",
        "Coming soon.", "Doubles the payment for dishes served to customers.",
        "Coming soon.", "Customers arrive more frequently while the restaurant is open.",
        "Increases coin collection range.", "Coming soon."
    };

    public static string GetName(GameObject prefab)
    {
        if (prefab == null) return "";
        var info = prefab.GetComponent<ItemTooltipInfo>();
        if (info != null && !string.IsNullOrWhiteSpace(info.DisplayName)) return info.DisplayName;
        string key = prefab.name.Replace("(Clone)", "").Trim();
        if (names.TryGetValue(key, out string value)) return value;
        if (Regex.IsMatch(key, "^meat[0-9]+$", RegexOptions.IgnoreCase)) return "Meat " + key.Substring(4);
        if (Regex.IsMatch(key, "^dinner[0-9]+$", RegexOptions.IgnoreCase)) return "Dish " + key.Substring(6);
        return Regex.Replace(key.Replace('_', ' '), "([a-z])([A-Z])", "$1 $2");
    }

    private static Sprite GetIcon(GameObject prefab)
    {
        var image = prefab.GetComponentInChildren<Image>(true);
        if (image != null && image.sprite != null) return image.sprite;
        var renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
        return renderer != null ? renderer.sprite : null;
    }

    public static ItemTooltipContent ForItem(GameObject prefab, int amount = 0, int? unitPrice = null)
    {
        if (prefab == null) return default;
        var info = prefab.GetComponent<ItemTooltipInfo>();
        string body = info != null ? info.Description : "";
        if (amount > 0) body += (string.IsNullOrEmpty(body) ? "" : "\n") + "Quantity: " + amount;
        if (unitPrice.HasValue) body += (string.IsNullOrEmpty(body) ? "" : "\n") + "Unit price: " + Mathf.Max(0, unitPrice.Value) + " coins";
        return new ItemTooltipContent(GetName(prefab), body, GetIcon(prefab));
    }

    public static ItemTooltipContent ForRecipe(CookingRecipe recipe, bool unlocked = true)
    {
        if (recipe?.result?.itemPrefab == null) return default;
        var content = ForItem(recipe.result.itemPrefab);
        var text = new StringBuilder("Ingredients: ");
        bool first = true;
        if (recipe.ingredients != null)
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient?.itemPrefab == null) continue;
                if (!first) text.Append(", ");
                text.Append(GetName(ingredient.itemPrefab));
                if (ingredient.amount > 1) text.Append(" ×").Append(ingredient.amount);
                first = false;
            }
        text.Append("\nBase cooking time: ").Append(recipe.cookingTime.ToString("0.#")).Append(" s");
        int price = CoinManager.Instance != null ? CoinManager.Instance.GetDishSalePrice(recipe.basePrice) : Mathf.Max(0, recipe.basePrice);
        text.Append("\nSale price: ").Append(price).Append(" coins");
        if (!unlocked) text.Append("\nCook this dish to unlock the recipe.");
        content.Body = text.ToString();
        return content;
    }

    public static ItemTooltipContent ForUpgrade(int index, Sprite sprite, int price, bool purchased, bool pending)
    {
        if (sprite == null || index < 0) return default;
        string title = index < upgradeNames.Length ? upgradeNames[index] : "Upgrade " + (index + 1);
        string description = index < upgradeDescriptions.Length ? upgradeDescriptions[index] : "Coming soon.";
        string status = purchased ? "Purchased" : pending ? "Selected - confirm purchase" : "Price: " + Mathf.Max(0, price) + " coins";
        return new ItemTooltipContent(title, description + "\n" + status, sprite);
    }
}

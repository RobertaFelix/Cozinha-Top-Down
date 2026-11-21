using System;
using UnityEngine;

[CreateAssetMenu(menuName = "UnderGrub/Restaurant/Furniture stock")]
public sealed class RestaurantFurnitureCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string displayName;
        public string category;
        public Sprite sprite;
        [Min(0)] public int startingCount = 1;
        public bool acceptsCountertopItem;
        public bool requiresCounter;
    }

    public Font interfaceFont;
    public Sprite panelFrame;
    public Entry[] entries;
    public RestaurantApplianceOffer[] applianceOffers;
}


[Serializable]
public sealed class RestaurantApplianceOffer
{
    public string id;
    public string label;
    [Min(1)] public int price;

    public static RestaurantApplianceOffer[] All
    {
        get
        {
            var catalog = Resources.Load<RestaurantFurnitureCatalog>("RestaurantFurnitureCatalog");
            return catalog && catalog.applianceOffers != null ? catalog.applianceOffers : Array.Empty<RestaurantApplianceOffer>();
        }
    }
}

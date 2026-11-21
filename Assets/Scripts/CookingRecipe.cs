using System;
using UnityEngine;

[Serializable]
public class CookingRecipe
{
    public InventoryItem[] ingredients;
    public DishTemperature temperature;

    public float cookingTime = 5f;

    [Header("Preço do prato")]
    [Min(0)]
    public int basePrice = 10;

    public InventoryItem result;
}
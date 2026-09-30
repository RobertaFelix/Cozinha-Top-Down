using System;
using UnityEngine;

[Serializable]
public class CookingRecipe
{
    public InventoryItem[] ingredients;

    public float cookingTime = 5f;

    public InventoryItem result;
}
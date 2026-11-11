using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class RecipeSaveState { public string[] unlocked; public string[] menu; }
public sealed class RecipeSaveModule : SaveParticipant<RecipeSaveState>, ISceneSaveParticipant
{
    public override string SaveKey => "recipes-menu";
    public override int RestoreOrder => 40;
    public bool SceneStateAvailable => Stove != null;
    private StoveController Stove => FindAnyObjectByType<StoveController>();
    protected override RecipeSaveState Capture()
    {
        var unlocked = new List<string>();
        foreach (var recipe in Stove.recipes)
            if (RecipeBookController.Instance.HasUnlockedRecipe(recipe)) unlocked.Add(Id(recipe));
        var menu = new List<string>();
        foreach (var recipe in RestaurantMenuController.Instance.GetSelectedRecipes()) menu.Add(Id(recipe));
        return new RecipeSaveState { unlocked = unlocked.ToArray(), menu = menu.ToArray() };
    }
    public static string Id(CookingRecipe recipe) => SaveItemCatalog.Instance.Id(recipe.result.itemPrefab);
    public static CookingRecipe FindRecipe(StoveController stove, string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var recipe in stove.recipes)
            if (recipe?.result?.itemPrefab != null && Id(recipe) == id) return recipe;
        throw new System.IO.InvalidDataException("Unknown saved recipe: " + id);
    }
    protected override void Validate(RecipeSaveState state)
    {
        Require(state.unlocked != null && state.menu != null && state.menu.Length <= RestaurantMenuController.Instance.MaxDishes, "Invalid saved recipes.");
        var unlocked = new HashSet<string>();
        foreach (var id in state.unlocked) { Require(FindRecipe(Stove, id) != null && unlocked.Add(id), "Invalid unlocked recipe."); }
        var menu = new HashSet<string>();
        foreach (var id in state.menu) Require(unlocked.Contains(id) && menu.Add(id), "Invalid saved menu.");
    }
    protected override void Restore(RecipeSaveState state)
    {
        var menu = RestaurantMenuController.Instance;
        menu.ClearMenu();
        RecipeBookController.Instance.RestoreUnlockedRecipes(Array.ConvertAll(state.unlocked, id => FindRecipe(Stove, id)));
        foreach (var id in state.menu)
            if (!menu.TryAddRecipe(FindRecipe(Stove, id))) throw new InvalidOperationException("Could not restore menu.");
    }
}

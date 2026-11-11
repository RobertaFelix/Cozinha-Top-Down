using System;
using UnityEngine;

[Serializable] public sealed class CookingSaveState
{
    public string recipeId;
    public float timer;
    public float burnTimer;
    public bool cooking;
    public bool finished;
    public bool usedPan;
    public bool usedPot;
}
public sealed class CookingSaveModule : SaveParticipant<CookingSaveState>, ISceneSaveParticipant
{
    public override string SaveKey => "cooking";
    public override int RestoreOrder => 50;
    public bool SceneStateAvailable => Stove != null;
    private StoveController Stove => FindAnyObjectByType<StoveController>();
    protected override CookingSaveState Capture() => Stove.CaptureCookingState();
    protected override void Validate(CookingSaveState state)
    {
        Require(!float.IsNaN(state.timer) && !float.IsInfinity(state.timer) && state.timer >= 0 &&
            !float.IsNaN(state.burnTimer) && !float.IsInfinity(state.burnTimer) && state.burnTimer >= 0 &&
            !(state.cooking && state.finished), "Invalid saved cooking state.");
        Require(string.IsNullOrEmpty(state.recipeId) ? !state.cooking && !state.finished :
            RecipeSaveModule.FindRecipe(Stove, state.recipeId) != null && (state.cooking || state.finished), "Invalid saved cooking recipe.");
    }
    protected override void Restore(CookingSaveState state) => Stove.RestoreCookingState(state);
}

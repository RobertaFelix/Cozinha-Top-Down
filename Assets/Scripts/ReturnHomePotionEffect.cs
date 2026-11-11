using UnityEngine;

[CreateAssetMenu(menuName = "UnderGrub/Potions/Return Home")]
public sealed class ReturnHomePotionEffect : PotionEffect
{
    public override bool TryApply(GameObject user)
    {
        var environment = Object.FindFirstObjectByType<EnvironmentTransition>();
        if (environment != null) return environment.TryReturnHome();
        var scenes = Object.FindFirstObjectByType<SceneTransitionController>();
        return scenes != null && scenes.TryReturnHome();
    }
}

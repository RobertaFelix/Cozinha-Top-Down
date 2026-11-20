using UnityEngine;

[CreateAssetMenu(menuName = "UnderGrub/Potions/Instant Stamina")]
public sealed class StaminaPotionEffect : PotionEffect
{
    [SerializeField, Min(1)] private float amount = 50;
    public string Description => $"Restores {amount:0.#} stamina instantly.";
    public override bool TryApply(GameObject user) => user &&
        user.TryGetComponent<PlayerStamina>(out var stamina) && stamina.TryRestore(amount);
}

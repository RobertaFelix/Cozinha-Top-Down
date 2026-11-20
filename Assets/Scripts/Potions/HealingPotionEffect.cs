using UnityEngine;

[CreateAssetMenu(menuName = "UnderGrub/Potions/Instant Healing")]
public sealed class HealingPotionEffect : PotionEffect
{
    [SerializeField, Min(1)] private int amount = 50;
    public string Description => $"Restores {amount} HP instantly.";

    public override bool TryApply(GameObject user)
    {
        if (!user || !user.TryGetComponent<PlayerHealth>(out var health) ||
            !health.InDungeon || health.Current >= health.Maximum) return false;
        health.Heal(amount);
        return true;
    }
}

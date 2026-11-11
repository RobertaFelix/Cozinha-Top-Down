using UnityEngine;

public sealed class PotionItem : MonoBehaviour
{
    [SerializeField] private PotionEffect effect;

    public bool TryUse(GameObject user)
    {
        // Potions without an assigned effect already support consumption from the hotbar.
        return effect == null || (user != null && effect.TryApply(user));
    }
}

using UnityEngine;

// Future potion effects implement this contract and return false when use is rejected.
public abstract class PotionEffect : ScriptableObject
{
    public abstract bool TryApply(GameObject user);
}

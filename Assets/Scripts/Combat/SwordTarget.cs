using UnityEngine;

public abstract class SwordTarget : MonoBehaviour
{
    public abstract bool TakeDamage(int amount, Vector2 direction);
}

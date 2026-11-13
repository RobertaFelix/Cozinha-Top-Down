using UnityEngine;

public enum FarmToolKind { Hoe, WateringCan }
public sealed class FarmToolItem : MonoBehaviour
{
    [SerializeField] private FarmToolKind kind;
    public FarmToolKind Kind => kind;
}

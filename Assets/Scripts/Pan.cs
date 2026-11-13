using UnityEngine;

public class Pan : CookwareCounter
{
    [SerializeField] private InventoryItem panItem;
    public override GameObject InitialItem => panItem.itemPrefab;
}

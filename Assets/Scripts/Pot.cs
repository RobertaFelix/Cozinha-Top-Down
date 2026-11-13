using UnityEngine;

public class Pot : CookwareCounter
{
    [SerializeField] private InventoryItem potItem;
    public override GameObject InitialItem => potItem.itemPrefab;
}

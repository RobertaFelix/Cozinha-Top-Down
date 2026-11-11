using UnityEngine;

// Opcional no prefab do item para substituir os nomes e descrições padrão.
[DisallowMultipleComponent]
public class ItemTooltipInfo : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;

    public string DisplayName => displayName;
    public string Description => description;
}
